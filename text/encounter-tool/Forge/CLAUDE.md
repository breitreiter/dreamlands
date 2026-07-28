# Forge — developer guide

Forge enriches skeleton `.enc` encounter files (FIXME beat stubs) into finished
prose. It is the .NET port of the proven `~/repos/narr/forge` Python pipeline, and
it **supersedes the failed EncounterCli `colorize`/`factual`/`voice`/`critic`
stages** (deleted). See `plans/forge_dotnet_port.md` for the full plan and the
phase-by-phase parity gates against the Python.

Taste anchor: `text/encounters/arcs/plains/the_city`.

## The one rule

**The inbound `.enc` is never modified.** Each `.enc` gets a peer-JSON sidecar
(`X.enc.json`) that accumulates, per FIXME beat, the original stub plus every
phase's output. The final `integrate` pass splices approved prose into a *fresh*
`.enc` under `out/`. Source is pristine; the sidecar is the work; `out/` is the
product. **Line numbers are the splice key** and are stable only because the `.enc`
is never rewritten — `integrate` guards this with a `source_sha`.

The peer JSON is forge-internal (scratch + a fine-tune corpus). The dreamlands
`Encounter` library never reads it — the game only ever sees finished `.enc` via
`bundle`. So **Forge depends on `Dreamlands.Encounter`/`.Game`/`.Rules`, never the
reverse.**

## Pipeline

```
.enc (FIXME stubs)
  parse        enc -> X.enc.json beat skeleton                free, local
  categorize   tag each beat 1 of 6 tones                     GLM via router, free
  color        gemma triplet enriches each beat (imp loom)    free, SLOW (overnight/arc)
  synthesis    beat + color -> finished prose (logic filter)  Cloudflare via router, PAID
  weave        threaded "story so far" synthesis along a path Cloudflare via router, PAID
  integrate    peer JSON -> out/<arc>/<name>.enc              free, local
```

Each phase is a subcommand of the `forge` CLI, reads what it needs from the peer
JSON, and writes only its own `stages[<phase>]` key. Phases are independent and
idempotent; a missing upstream key is a legible "run that phase first," never a
silent skip.

```bash
dotnet run --project text/encounter-tool/Forge -- <cmd> ...
# parse <file.enc>|<dir> ...            enc -> X.enc.json
# categorize <file.enc.json>|<dir> ...  GLM tone tags          [--force] [--dry-run]
# color <file.enc.json>|<dir> ...       imp loom color bank    [--force] [--limit N] [--dry-run] [--provider imp|glm-hi-temp]
# synthesis <file.enc.json> ...         per-beat integrator    [--model ID] [--limit N] [--beats id,id] [--dry-run] [--no-lens] [--force]
# weave <arc-dir>                       threaded synthesis     [--thread vastand|cave] [--model ID] [--dry-run] [--force]
# integrate <file.enc.json>|<dir> ...   peer JSON -> out/      [--out <dir>]
# thread <arc-dir>                      debug: print beat spine [--thread name]
```

Dirs expand to their `*.enc` / `*.enc.json` children. With nothing approved,
`integrate` reproduces the source byte-for-byte (the round-trip identity that
proves the spine is lossless).

## Modules

| File | Responsibility |
|---|---|
| `Beats.cs` | FIXME-beat extraction (Python-`splitlines`-faithful line splitter) |
| `Peer.cs` / `PeerDocument.cs` | peer-JSON schema: build / merge / load / save / sha; opaque `stages` |
| `ParseCommand` / `IntegrateCommand` | the spine: enc ⇄ peer JSON |
| `RouterClient.cs` | one OpenAI-compat client for every LLM, fronted by minrouter on imp:8086 (`ChatAsync`/`CompleteAsync`/`ModelIdAsync` + `StripThink`); holds the quota/empty exceptions |
| `CategorizeCommand.cs` | tone tagging (GLM via router, `imp-glmchat`) |
| `Color.cs` + `ImpLoomColorProvider` + `GlmHighTempColorProvider` + `ColorCommand` | color stage behind `IColorProvider` (loom, over ssh — NOT via the router) |
| `SynthesisCommand.cs` | the logic-filter SYSTEM prompt + `BuildUser` (shared with weave); paid integrator via router `cf` |
| `Thread.cs` + `Threads.cs` + `ThreadCommand` | engine-driven graph walk -> ordered beat spine |
| `WeaveCommand.cs` | threaded story-so-far synthesis |
| `Config.cs` | gitignored `appsettings.json` loader (Router / Imp) |

## Config & secrets

Every LLM call goes through **minrouter on imp:8086** — one authenticated gateway
that fronts the local imp GLM profiles (auto-loaded on demand) and the paid
Cloudflare workers-ai models, holding each provider's own credentials. Forge carries
**no provider key, no Cloudflare WAF hack, and no `swap-model` dance** — it just names
an upstream (`imp-glmchat`, `cf`) and sends an OpenAI-shaped request.

The only secret is the router's bearer token, read from the **`$MINROUTER_KEY`
environment variable** — never a config file, never logged. Runtime config is an
*optional* gitignored `appsettings.json` next to the binary (or `--config <path>`);
absent, the defaults (BaseUrl `http://imp:8086`, upstreams `imp-glmchat`/`cf`,
integrator kimi-k2.6) work as-is. Copy `appsettings.example.json` only to override a
default. `*.enc.json`, `out/`, and `*.key` are gitignored.

- `Router` — router base URL, token env-var name, and the per-stage upstream+model
  (categorize `imp-glmchat`; synthesis/weave `cf`). Swap the integrator per run with
  `--model` — every model the router exposes (anthropic, gemini, openai, other cf ids)
  is one string away.
- `Imp` — the opaque loom color service (ssh target, loom-io inbox/outbox, `grind`).
  **Not fronted by the router**: loom is a torch+LoRA steering script, not an OpenAI
  endpoint, so `color` still drives it over ssh.

## Hard-won gotchas (carry these — they are the load-bearing 20%)

- **Synthesis is a logic filter, not a stylist.** The gemma color bank is vivid but
  produced *without reasoning*; the integrator must catch (1) figure-stated-as-fact
  and (2) continuity/scale/timeline breaks. The SYSTEM prompt encodes this — port it
  verbatim; no ban lists.
- **Thinking must stay ON for synthesis** (`/nothink` reproduced gemma-like incoherence).
- **Threaded story-so-far** (`weave`) beats isolated per-beat synthesis: feeding the
  prior finished prose forward is a stronger POV+fact anchor than any rule, and the
  lens is dropped when a story-so-far is present. This is what took the_villa from
  ~50% rewrites to ship-quality.
- **Color exists to break "Claude-tic" contamination** — decompose (Claude) bakes its
  tics into the stubs; the gemma triplet injects genuinely alien texture. This is why
  color is load-bearing and why the `glm-hi-temp` FOSS fallback is explicitly inferior.
- **Daily caps are fatal, not transient.** minrouter enforces a per-upstream daily cap
  and signals a spent one with HTTP 429 + `daily limit for '<upstream>' reached` —
  `RouterClient` raises `QuotaExhaustedException` on that marker so the run aborts
  rather than backing off into the same wall. (Transient 429/5xx still retry with
  backoff.) The old Cloudflare-WAF `User-Agent` hack and the 4006-neuron special case
  are gone — the router owns provider auth and quotas now.

## imp model management

`categorize` no longer touches `swap-model`: it calls the router's `imp-glmchat`
upstream, which **auto-loads the glmchat profile on demand**. Only `color` still needs
hands-on imp management — loom (the gemma color triplet) is a **script, not a
service**, so free the box first (`ssh imp '~/.local/bin/swap-model stop all'`) then
let `color` drive `grind` over ssh. The big loads need the box to themselves; a
full-arc color pass is an overnight run.

## Test arc

`~/repos/narr/forge/arcs/the_villa` (5 enc, 39 beats, full substrate + color). Every
deterministic phase is parity-gated against the Python forge on this arc (round-trip,
prompt assembly, thread spines, beats-job) — see the plan.
