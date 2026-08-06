---
kind: plan
title: "OpenTelemetry export from GameServer to AppSignal"
state: active
created: 2026-08-06
updated: 2026-08-06
status: Phase 1 deployed (production app settings still unset, so it is inert there) and validated end-to-end against the dev AppSignal app — the custom metric is visible in their UI. Phase 2 pass 1 (game.action span + action count/duration + game.created) built and locally verified, traces now flowing; NOT yet deployed. Remaining: phase 2 pass 2 (reason_code sweep, encounter/combat counters), phase 3 Cosmos, phase 4 host telemetry + App Insights teardown.
touches:
  files:
    - server/GameServer/GameServer.csproj
    - server/GameServer/Program.cs
    - server/GameServer/Telemetry.cs (new)
    - server/GameServer/GameFunctions.cs
    - server/GameServer/CosmosGameStore.cs
    - server/GameServer/host.json (phase 4 only)
    - deploy.env.example (new, shared with the TODO deploy-identifier tidy-up)
  features: [observability, deployment, ops]
related:
  - TODO.md → Pre-Launch Cheap Wins → "Event-forensics log" (adjacent, NOT superseded — see §7)
  - TODO.md → Deployment & Hosting → "Move deployment identifiers out of tracked files"
---

# OTel → AppSignal

Ship traces and metrics from the Azure Functions GameServer to AppSignal's hosted
OTLP collector, so that after launch we can answer "is it up, is it slow, what's
erroring, and how much are people playing" without SSHing into a log blob.

**Traces and metrics only — no logs** (free plan; see §4). Phases: 1 baseline
wiring → 2 domain spans/metrics → 3 Cosmos spans → 4 optional host-level.

## 0. Secrets — read this first

**No endpoint, key, header value, or account id goes in this file, in any file
under `plans/`, in any `.cs` file, or in any tracked config.** This repo is public.

Configuration is carried entirely by the OTel SDK's own standard environment
variables, which the OTLP exporter reads natively with zero code:

| Variable | Holds | Lives in |
|---|---|---|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | AppSignal collector **base** URL (SDK appends `/v1/metrics`) | Azure app settings / local.settings.json |
| `APPSIGNAL_PUSH_API_KEY` | the AppSignal push API key | Azure app settings / local.settings.json |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | `http/protobuf` (the collector speaks OTLP over HTTP) | same |
| `APPSIGNAL_APP_NAME` | AppSignal app name, default `dreamlands` | same (or code default) |
| `OTEL_SERVICE_NAME` | `dreamlands-api` | same (or code default) |
| `OTEL_RESOURCE_ATTRIBUTES` | any extra resource attributes | same |
| `OTEL_TRACES_SAMPLER` / `..._ARG` | sampling knob, unset = always-on | same |

**The key is NOT a header.** AppSignal authenticates by *resource attribute* —
`appsignal.config.push_api_key`, attached to every payload. We read it from
`APPSIGNAL_PUSH_API_KEY` and set the attribute in code (see §2a finding 3), so
the secret still only ever exists as an environment value.

Because these are SDK-standard names, **the exact endpoint path and header name
AppSignal wants are a deployment detail, not a code detail** — if AppSignal
changes them we change an app setting, not a build. Copy both values out of the
AppSignal dashboard at deploy time.

Placement:

- **Azure** — set alongside the existing `DREAMLANDS_COSMOS` secret:
  `az functionapp config appsettings set -n <app> -g <rg> --settings OTEL_EXPORTER_OTLP_ENDPOINT=... OTEL_EXPORTER_OTLP_HEADERS=...`
  (Run it interactively; do not commit the invocation with values filled in.)
- **Local dev** — `server/GameServer/local.settings.json`, which is already
  gitignored (verified: neither it nor `appsettings.json` is tracked).
- **deploy.sh** — no change required; it deploys code, not app settings. If we
  ever want the script to set them, it reads from the gitignored `deploy.env`
  that the TODO deployment-identifier item already proposes.

**Hard rule for the implementation: if `OTEL_EXPORTER_OTLP_ENDPOINT` is unset,
telemetry wiring is skipped entirely.** That makes local `dotnet run` and the CLI
harness silent by default and means a missing setting degrades to "no telemetry",
never to a boot failure or a per-request connection-refused stall.

## 1. Approach: instrument the worker, not the host (at first)

Two places can emit OTel from an isolated-worker Functions app:

1. **The worker process** — our own `HostBuilder` in `Program.cs`. We control it,
   it needs no platform features, and because `ConfigureFunctionsWebApplication()`
   runs a real ASP.NET Core pipeline in-process, `AspNetCoreInstrumentation`
   produces genuine per-request server spans. This is where our domain spans and
   metrics have to live anyway.
2. **The Functions host** — `host.json` `"telemetryMode": "openTelemetry"`, which
   makes the platform export invocation and cold-start spans. It also changes how
   the host talks to Application Insights.

**Recommendation: phases 1–3 do worker-only.** It is the whole of the value we
actually need (request latency, error rate, Cosmos time, gameplay counters) at
zero platform risk. Phase 4 evaluates the host mode separately, because it is the
one change that can affect existing platform telemetry.

Packages (latest as of 2026-08-06, checked against nuget):

```
OpenTelemetry.Extensions.Hosting                    1.17.0
OpenTelemetry.Exporter.OpenTelemetryProtocol        1.17.0
OpenTelemetry.Instrumentation.AspNetCore            1.17.0
OpenTelemetry.Instrumentation.Http                  1.17.0
OpenTelemetry.Instrumentation.Runtime               1.17.0
Microsoft.Azure.Functions.Worker.OpenTelemetry      1.2.0   (phase 4 / correlation)
```

Add them with `dotnet add package` — per house rules, not by hand-editing the csproj.

## 2. Phase 1 — baseline wiring

New file `server/GameServer/Telemetry.cs`: the `ActivitySource`, the `Meter`, and
a single `AddDreamlandsTelemetry(this IServiceCollection)` extension. One file,
one responsibility; `Program.cs` gains one line.

```
services.AddDreamlandsTelemetry();   // no-ops when OTEL_EXPORTER_OTLP_ENDPOINT is unset
```

Inside it:

- **Resource** — `service.name` from `OTEL_SERVICE_NAME` (default `dreamlands-api`),
  `service.version` from the existing `api-version` file that `GameData` already
  reads and `deploy.sh` already bundles, `deployment.environment` from
  `GameData.IsDev`. Version-stamped telemetry is what makes "did that deploy break
  it?" answerable.
- **Tracing** — AspNetCore + HttpClient instrumentation, plus our own
  `ActivitySource`. Filter `GET /api/health` out of AspNetCore spans; whatever
  pings it will otherwise be most of our trace volume and all of our bill.
- **Metrics** — `Microsoft.AspNetCore.Hosting` + `System.Net.Http` +
  `OpenTelemetry.Instrumentation.Runtime` meters, plus our own.
- **Exporter** — OTLP, `http/protobuf`, batch processor with
  `ScheduledDelayMilliseconds` around 5000 (see §6 on why the default is wrong here).

## 2a. Phase 1 results — built 2026-08-06, verified locally, NOT yet deployed

Verified by pointing `OTEL_EXPORTER_OTLP_ENDPOINT` at a throwaway local HTTP
server that logs which OTLP signal paths get hit and dumps readable strings out
of the payloads. Worth repeating that trick before trusting AppSignal's UI: it
answers "what are we actually sending" without a vendor in the loop.

**Working:** OTLP export over `http/protobuf` with the API key carried in the
request header; resource attributes (`service.name`, `service.version` from
`api-version`, `deployment.environment=development`); runtime + ASP.NET Core +
HttpClient metrics; our own meter, confirmed via `dreamlands.debug_endpoint.hit`
landing after a `combat/list` call; our own `ActivitySource`, confirmed with a
temporary probe span. Env-var gate confirmed: nothing is registered and nothing
is sent when the endpoint is unset.

**Finding 1 — `UseOtlpExporter()` turns on all three signals, including logs.**
OTel 1.17 removed the per-signal `AddOtlpExporter` overload for
`TracerProviderBuilder`, so `UseOtlpExporter()` is the only path — and it
registers the OTel logging provider whether or not `.WithLogging()` was ever
called. The collector received `/v1/logs` traffic on the first run. Muted
explicitly with `AddFilter<OpenTelemetryLoggerProvider>("*", LogLevel.None)`;
deleting that one line is how we ship logs later. **Had we trusted the code to
mean what it looked like, we would have been shipping logs to a free-plan account
while believing we had turned them off.**

**Finding 2 — AspNetCore instrumentation produces NO request spans in the
isolated worker.** The only span the exporter ever emitted was the worker's own
long-lived gRPC channel to the Functions host
(`AzureFunctionsRpcMessages.FunctionRpc/EventStream`, via HttpClient
instrumentation). Invocations arrive over that gRPC channel rather than through
an observable ASP.NET Core server pipeline, so there is no
`POST /api/game/{id}/action` span to be had. Ruled out as causes, each tested
separately: our `/health` span filter (removed, no change);
`Microsoft.Azure.Functions.Worker.OpenTelemetry` 1.2.0 plus
`AddSource("Microsoft.Azure.Functions.Worker")` (no change); `host.json`
`"telemetryMode": "openTelemetry"` (no change **locally**, under Core Tools
4.8.0 — this says nothing about the deployed Azure host, see §5).

Consequences, applied:

- ASP.NET Core instrumentation is registered for **metrics only**. The
  `/health` trace filter is gone with it — there were no spans for it to filter.
  Note `http.server.request.duration` still exports, but with no `/api/...`
  route values, so it is a coarse aggregate rather than a per-route breakdown.
- HttpClient tracing stays (it is how Cosmos calls will show up in phase 3) with
  the gRPC channel filtered out as noise.
- The worker OTel package and the `host.json` flip were both **reverted** —
  inert here, and the `host.json` one is an unverified platform toggle that also
  changes App Insights behaviour, so it belongs to phase 4 where it can be tested
  in Azure deliberately.

**Finding 3 — AppSignal authenticates by resource attribute, and the env-var
resource detector is OFF by default.** Read AppSignal's own docs before assuming
OTLP conventions apply
(`docs.appsignal.com/opentelemetry/installation/generic`): the push API key
travels as the resource attribute `appsignal.config.push_api_key`, not as an
`Authorization`-style header, and the collector also expects
`appsignal.config.name`, `.environment`, `.revision`,
`.language_integration`, plus `service.name` and `host.name`. We set all of these
from `APPSIGNAL_PUSH_API_KEY` / `APPSIGNAL_APP_NAME` / `GameData`, with
`appsignal.config.revision` fed by `api-version` so AppSignal gets deploy markers
for free. Separately: `OTEL_RESOURCE_ATTRIBUTES` was being **silently ignored** —
`ConfigureResource` does not include the environment-variable detector unless you
call `AddEnvironmentVariableDetector()`. Both now verified on the wire.

**Finding 4 — AppSignal's collector does not accept logs at all.** Their docs say
to disable the logging exporter to silence export errors. So the §4 mute is not
merely a free-plan cost decision, it is required for correctness; shipping logs
later means shipping them somewhere else, not flipping our one line.

**Method note — kill stale workers before trusting any local run.** Orphaned
`func` workers from previous runs keep exporting *runtime* metrics on a timer
with no traffic at all, so a stale process looks exactly like a working one and
will happily show you the previous build's attributes. Eleven had accumulated
before this was noticed, which contaminated an earlier round of results. The
verification script now kills strays first. Equally: make the fake collector dump
**all** payload strings rather than probing for expected ones — a probe list can
only confirm what you already assumed and silently misses what actually shipped.

**Live check against the real dev collector, 2026-08-06:** the hosted collector
(`*.eu-central.appsignal-collector.net`) answered **`200 OK`** to three metric
exports driven by real local traffic — endpoint shape (base URL, SDK appends
`/v1/metrics`), OTLP-over-HTTP protobuf, and the resource-attribute auth all
accepted, and `appsignal.config.language_integration=dotnet` was not rejected.
Only `/v1/metrics` was ever sent: no logs, no traces.

Caveat worth keeping in mind: **a 200 proves acceptance at the wire, not
visibility in the UI.** Collectors commonly ack and then drop on a bad key.
Dashboard confirmation is the real gate. The technique — a local forwarding proxy
that prints the vendor's status codes while never logging the request body, since
the body carries the key — is the fastest way to separate "we're not sending" from
"they're not accepting", and is worth reaching for again in phase 2.

**The real consequence: phase 2 is no longer optional polish — it is where traces
come from at all.** Until the `game.action` span exists, the trace side of this
integration is effectively empty and only metrics are useful. Either pull the
`game.action` span forward, or accept metrics-only until phase 2 lands.

Exit criteria for the phase, restated after the above: deploy, hit a few
endpoints, and confirm metrics land in AppSignal with the right
`service.version` and `deployment.environment` — **not** request spans, which do
not exist yet.

## 3. Phase 2 — domain spans and metrics

The generic HTTP layer tells us `POST /api/game/{id}/action` is slow. It cannot
tell us *which action*, and that is the only question worth asking, because
`GameAction` is a ~1,100-line switch over every verb in the game
(`server/GameServer/GameFunctions.cs:172`).

**Span per action.** Wrap the switch body in an activity from our source named
`game.action`, tagged:

- `dreamlands.action` — the verb (`move`, `choose`, `inn_book`, `market_order`, …)
- `dreamlands.mode` — `session.Mode`
- `dreamlands.in_dungeon` — bool
- `dreamlands.game_id` — **span attribute only, never a metric dimension**

**Counters and histograms** on a `Dreamlands.GameServer` meter:

- `dreamlands.action.count{action, outcome}` — outcome ∈ ok / rejected / not_found
- `dreamlands.action.duration` histogram, dimensioned by `action`
- `dreamlands.game.created` — the closest thing we have to a signup metric
- `dreamlands.encounter.started{kind}` and `dreamlands.combat.started` /
  `dreamlands.combat.ended{result}`
- `dreamlands.rejected{reason_code}` — see below

### Phase 2 pass 1 — built 2026-08-06, verified locally

Shipped: the `game.action` span (tags `dreamlands.action`, `.mode`,
`.in_dungeon`, `.game_id`, plus `ActivityStatusCode.Error` on any 4xx/5xx),
`dreamlands.action.count{action,outcome}`, `dreamlands.action.duration{action}`,
and `dreamlands.game.created`.

**The outcome dimension needed no sweep at all.** `GameAction`'s ~40 return sites
already encode the outcome in their result type, so `OutcomeOf()` reads it back
off `IActionResult` — `OkObjectResult` → `ok`, `BadRequestObjectResult` →
`rejected`, `NotFoundObjectResult` → `not_found` — and not one existing return
statement had to change. The only structural change is that the handler body moved
into a private `RunGameAction(...)`, so the `[Function]` method can time it and
label the result; `req` was referenced exactly once (the body read), which is what
made the extraction clean.

Verified against the real dev collector: `/v1/traces` **and** `/v1/metrics` both
`200 OK` — this is the first run where traces flowed at all, confirming the §2a
conclusion that our own source is the only source of server spans. Label values
`ok`, `rejected` and `not_found` confirmed on the wire by driving one of each.
All 510 tests pass.

Two notes for whoever picks this up:

- A new game opens *inside* the intro encounter, so `move` is illegal until the
  encounter is resolved. Test traffic that opens with a move produces six `400`s
  and zero `ok` samples — the metrics are right and the traffic is wrong.
- Payload-string extraction from protobuf merges adjacent strings that have no
  separator between them, so a label value can appear glued to its key
  (`actionmove`). Don't read a missing token as a missing label; check the
  separator-delimited ones (`ok`, `rejected`, `not_found` all appear cleanly).

**The 400 problem, and the one refactor this phase needs — still to do (pass 2).** Client/server drift
shows up as a rising rate of "legal request the server refuses", and today those
are ~40 inline `return new BadRequestObjectResult(new { error = "…" })` sites,
several with the user's input interpolated into the message
(`$"Invalid direction: {actionReq.Direction}"`, `$"No exit {…}"`). Interpolated
strings are unbounded metric cardinality — a classic way to get a surprise bill.

So: introduce one small private helper,

```
static IActionResult Reject(string reasonCode, string message)
```

which bumps `dreamlands.rejected{reason_code}` with the **stable code** and
returns the existing human message unchanged. The wire contract does not change;
only a stable label is added beside it. This is a mechanical sweep of one file and
it also gives us, for free, a legible list of every way the server says no.

**Deliberately excluded:** anything that is game analytics rather than ops — gold
balances, encounter-choice distributions, per-player progression. Those belong to
the event-forensics log (§7), not to a metrics backend, and mixing them makes both
worse.

## 4. Phase 3 — Cosmos spans

- **Cosmos.** `CosmosGameStore.CreateClient` currently builds a client with no
  telemetry options. The Cosmos SDK ships distributed tracing behind its own
  opt-in flag, off by default. Turn it on and add its activity source to the
  tracer provider, so `store.Load` / `store.Save` become child spans with real
  request-charge and latency data — the single most likely source of a
  production latency surprise, since every action does at least one load and one
  save. **Verify the exact option/source name against the installed SDK version
  during implementation rather than trusting this paragraph**; the shape has
  changed across Cosmos 3.x releases.

### Logs — deliberately out of scope (decided 2026-08-06)

**Traces and metrics only.** Logs are the highest-volume signal and we are on
AppSignal's free plan; shipping them first would find the cap with the least
useful data. The plan is to run traces + metrics, watch the usage numbers for a
while, and only then decide whether logs fit in what's left (or justify a paid
plan).

Consequences for the implementation — do these now, so adding logs later is a
config change and not an archaeology exercise:

- Do **not** add the OTLP log exporter or an OTel `ILoggerProvider` to the worker.
  Existing `log.LogInformation(...)` calls stay as they are and keep flowing to
  the Functions host, which remains the place to go read them.
- When something needs to be *findable*, attach it to the span as an attribute or
  an `AddEvent`, not to a log line — span data is going to AppSignal and log lines
  are not. This mostly matters at the reject sites in §3: the `reason_code` on the
  span is what makes a 400 diagnosable without log access.
- Keep the exporter registration for logs a one-liner-shaped hole. Same
  `OTEL_EXPORTER_OTLP_*` env vars, same gate; flipping it on later should touch
  only `Telemetry.cs`.

**Watch for caps in this order:** metric series count is the likeliest thing to
bite first, not span volume — every label combination is its own series, and §3's
`action` and `reason_code` dimensions multiply. If usage runs hot, the first cut
is dropping `dreamlands.action.duration`'s per-action dimension (the HTTP-layer
histogram still covers overall latency), then sampling traces via
`OTEL_TRACES_SAMPLER` — a config change, no redeploy.

## 5. Phase 4 (optional, decide later) — host-level telemetry

Flipping `host.json` to `"telemetryMode": "openTelemetry"` adds host invocation
spans and cold-start visibility, and with
`Microsoft.Azure.Functions.Worker.OpenTelemetry` installed the worker's spans
become children of the host's rather than parallel roots.

Phase 1 raised the stakes here: since the worker produces no request spans of its
own (§2a finding 2), host invocation spans are the only route to automatic
per-request tracing. Note the local Core Tools host ignored the flip entirely —
**this must be tested against the deployed Azure host**, which is the only place
it can be judged.

Two things to check before doing it, which is exactly why it is its own phase:

1. **Application Insights is live — checked 2026-08-06, and it is on.** We assumed
   otherwise. Findings in `dreamlands-rg`:
   - `APPLICATIONINSIGHTS_CONNECTION_STRING` is set on `dreamlands-api`.
   - A `microsoft.insights/components` resource named `dreamlands-api` exists
     (appId `d3c43ecc-…`).
   - Two alert artifacts came with it: `Application Insights Smart Detection` and
     `Failure Anomalies - dreamlands-api`.

   Intent is to **disable it** once AppSignal is carrying traces + metrics. Order
   matters — turn AppSignal on first, confirm it is actually receiving, and only
   then remove App Insights, so we are never running blind in between. Teardown,
   when we get there:

   ```
   az functionapp config appsettings delete -n <app> -g <rg> \
     --setting-names APPLICATIONINSIGHTS_CONNECTION_STRING
   ```

   then delete the component and the two alert rules. Note this is an ARM
   **write**, so it needs the MFA step-up login recorded in TODO.md
   (`az login --tenant <id> --scope https://management.core.windows.net//.default`).
   Losing the portal's Monitor / Live Metrics view is the accepted cost; that is
   what AppSignal is replacing. Nothing in the repo references App Insights, so
   there is no code change.
2. **Double-billing.** Host + worker both exporting means every request produces
   more spans. Fine at launch traffic; measure before assuming.

## 5b. Two AppSignal targets: local dev and deployed (decided 2026-08-06)

**Two separate AppSignal apps, not one.** Local/dev telemetry goes to one, the
deployed Function App to the other.

The motivating reason is not tidiness, it's that **the metrics from §3 are only
meaningful if the traffic producing them is real.** The CLI integration tests in
the TODO backlog will hammer `game/new` and replay scripted action sequences —
traffic with a completely different shape from a human playing. Mixed into one
app it would wreck every number we actually want: `dreamlands.game.created`
becomes a test-run counter, the action-duration histogram gets a bimodal hump of
localhost-fast requests, and any error-rate alarm we set gets trained on
deliberately-provoked failures. A separate target keeps the production app's
numbers honest, and gives the tests somewhere useful to be observed in their own
right.

**Validate against the dev app first — always.** Every open question at
integration time is "will AppSignal accept this payload": endpoint path shape,
whether `appsignal.config.language_integration=dotnet` is a validated enum,
whether the free plan swallows our metric series. Locally each answer costs a
~30-second restart and can be diffed against the fake collector; in Azure each
costs a deploy cycle and asks production to be the test rig. The deployed app's
config is the *last* step, not the first — and since the gate means unset
variables produce no telemetry at all, deployed code can sit inert and harmless
while local iteration continues.

Mechanics — all env-var config, no code beyond what phase 1 already does:

- `deployment.environment` resource attribute distinguishes them regardless
  (`development` vs `production`), driven off `GameData.IsDev` per §2.
- **How AppSignal actually separates the two needs confirming in their dashboard
  before phase 1 ships**: it may be a distinct app + distinct API key (so the two
  environments differ by `OTEL_EXPORTER_OTLP_HEADERS`), or one app with AppSignal's
  own environment concept keyed off a resource attribute or `OTEL_SERVICE_NAME`.
  Either way it resolves to app settings on the Azure side and
  `local.settings.json` on the dev side; do not encode the answer in C#.
- **Free-plan caution:** two apps may draw against the same free-tier allowance.
  Check before assuming dev telemetry is free — if it competes with production
  for quota, the local target should be run *ad hoc* (set the env vars only when
  actively kicking the tires) rather than left on permanently. That is the
  default posture anyway, since an unset endpoint means no export.
- If integration tests ever run against the **deployed** app, they must be
  identifiable — an extra resource attribute or a marker header on the requests —
  or they reintroduce exactly the pollution this split exists to prevent. Prefer
  pointing them at a locally-run GameServer.

## 6. Gotchas

- **Consumption-plan worker lifetime is the real risk.** The batch exporter
  buffers, and an idle worker is torn down by the platform — potentially without
  a graceful shutdown that flushes. Low-traffic apps therefore lose exactly the
  telemetry from the last request before an idle-out, which is disproportionately
  often the interesting one. Mitigate with the shortened batch delay (§2) and
  verify by making one request, waiting out the idle window, and confirming the
  span arrived.
- **Cold starts** will show up as tail latency on every metric. Expected on
  Consumption; don't chase it as a bug.
- **Sampling** stays off (always-on) at launch traffic. `OTEL_TRACES_SAMPLER` is
  the knob if socials traffic makes that expensive; no code change needed.
- **Never dimension a metric by `gameId`, node coordinates, encounter id, or any
  user-supplied string.** Span attributes are the place for high-cardinality
  facts.
- **`ui/web` is out of scope.** Browser-side monitoring is a separate decision
  with its own privacy questions; note it and move on.

## 7. Relationship to the event-forensics log

TODO carries a pre-launch item for a replay-grade per-action event log to Cosmos,
consumed by ordinator. **OTel does not replace it and it does not replace OTel.**
Telemetry is sampled, aggregate, and retention-limited — built for "the p99 on
`move` doubled". The event log is exact, per-game, and ordered — built for "this
specific player says their sword vanished; replay their session". Different
questions, different stores.

The one place they touch: both hang off the `GameAction` boundary, so the
`game.action` activity from §3 is the natural place to later call `IEventSink`,
and the activity's trace id is worth writing into the event record so a complaint
can be jumped straight to its trace. Build the OTel side first — it is smaller and
independently useful — but leave that seam.

## Open questions

1. ~~**Signal scope for v1**~~ — RESOLVED 2026-08-06: **traces + metrics only,
   no logs.** Free plan; find out whether we hit a cap on the cheap signals
   before adding the expensive one. See §4 "Logs — deliberately out of scope".
2. ~~**App Insights**~~ — RESOLVED 2026-08-06: it **is** provisioned and active
   (contrary to assumption); we intend to disable it, after AppSignal is verified
   receiving. Details and teardown commands in §5.
3. ~~**Environments**~~ — RESOLVED 2026-08-06: **two AppSignal apps**, local/dev
   and deployed, so integration-test traffic can't corrupt production metrics.
   See §5b. Sub-question still open: whether AppSignal's free tier treats two apps
   as one allowance.
4. ~~**Do the debug endpoints get a tripwire counter?**~~ — RESOLVED 2026-08-06:
   **yes**, built in phase 1 (it needs no domain instrumentation, just the meter).
   `DebugAddCondition`,
   `CombatList`, `CombatBegin` are anonymous and live in production until the
   debug-affordance sweep lands (Launch Blockers). They're unreachable from the
   UI — the "Pick a fight" button is `import.meta.env.DEV`-gated — so *nothing
   legitimate should ever call them in production*. That makes them ideal
   tripwires: a counter whose correct value is permanently zero, where any
   nonzero reading means someone found an undocumented route and is poking at
   it, and `DebugAddCondition` in particular lets a caller mutate another
   player's game state by id.

   Cost is one counter increment per handler; value is that the alternative is
   finding out from a corrupted save. Set an alert at `> 0`. **Delete it together
   with the endpoints** in the sweep — a tripwire outliving the thing it watches
   is just a stale metric. Leaning yes; it's cheap and it expires on its own.
