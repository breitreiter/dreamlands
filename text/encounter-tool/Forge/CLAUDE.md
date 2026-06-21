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
  categorize   tag each beat 1 of 6 tones                     GLM on imp, free
  color        gemma triplet enriches each beat (imp loom)    free, SLOW (overnight/arc)
  synthesis    beat + color -> finished prose (logic filter)  Cloudflare gateway, PAID
  weave        threaded "story so far" synthesis along a path Cloudflare gateway, PAID
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
| `GlmClient.cs` | GLM on imp (OpenAI-compatible) + `StripThink` |
| `CategorizeCommand.cs` | tone tagging (GLM) |
| `Color.cs` + `ImpLoomColorProvider` + `GlmHighTempColorProvider` + `ColorCommand` | color stage behind `IColorProvider` |
| `GatewayClient.cs` | Cloudflare AI Gateway (paid integrator) with the WAF/quota/retry quirks |
| `SynthesisCommand.cs` | the logic-filter SYSTEM prompt + `BuildUser` (shared with weave) |
| `Thread.cs` + `Threads.cs` + `ThreadCommand` | engine-driven graph walk -> ordered beat spine |
| `WeaveCommand.cs` | threaded story-so-far synthesis |
| `Config.cs` | gitignored `appsettings.json` loader (Glm / Gateway / Imp) |

## Config & secrets

Runtime config is a **gitignored `appsettings.json`** next to the binary (or
`--config <path>`); copy `appsettings.example.json` and fill it in. `*.enc.json`,
`out/`, and `*.key` are gitignored. **Never read a key from a path outside the repo.**

- `Glm` — imp:8080 GLM (free; defaults work on the imp network, no config needed).
- `Gateway` — Cloudflare AI Gateway endpoint + key + default integrator model. PAID.
- `Imp` — the opaque loom color service: ssh target, loom-io inbox/outbox, `grind`.

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
- **Cloudflare WAF**: a real `User-Agent` (`curl/8.5.0`) is required or it 403s pre-auth.
  The 4006 daily free-neuron cap is not transient — abort, never back off into it.

## imp model management

`color` and `categorize` use imp. loom (the gemma color triplet) is a **script, not
a service**: to run it live, free the box first (`ssh imp '~/.local/bin/swap-model
stop all'`) then let `color` drive `grind` over ssh. GLM for categorize is the
`glmchat` profile (`swap-model glmchat`). The big loads need the box to themselves;
a full-arc color pass is an overnight run.

## Test arc

`~/repos/narr/forge/arcs/the_villa` (5 enc, 39 beats, full substrate + color). Every
deterministic phase is parity-gated against the Python forge on this arc (round-trip,
prompt assembly, thread spines, beats-job) — see the plan.
