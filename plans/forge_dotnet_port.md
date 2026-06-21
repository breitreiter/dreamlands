---
kind: plan
title: "Forge → .NET — trash the legacy enc pipeline, port the working forge process"
state: active
created: 2026-06-20
updated: 2026-06-21
status: active — Phases 1–3 SHIPPED. P1: peer-JSON spine (byte-identical round-trip). P2: GlmClient + categorize (31/39 tone agreement, deltas = temp variance). P3: GatewayClient (Cloudflare, all quirks) + synthesis; dry-run prompt assembly BYTE-IDENTICAL to synthesis.py across all 39 the_villa beats (paid path ported, not live-fired). Next: Phase 4 (Thread via Dreamlands.Game + weave).
touches:
  files:
    - text/encounter-tool/Encounter.sln
    - text/encounter-tool/EncounterCli/ColorizeCommand.cs
    - text/encounter-tool/EncounterCli/FactualCommand.cs
    - text/encounter-tool/EncounterCli/VoiceCommand.cs
    - text/encounter-tool/EncounterCli/CriticCommand.cs
    - text/encounter-tool/EncounterCli/FactualEval.cs
    - text/encounter-tool/EncounterCli/DraftBlocks.cs
    - text/encounter-tool/EncounterCli/ExpandClient.cs
    - text/encounter-tool/EncounterCli/GlmClient.cs
    - text/encounter-tool/EncounterCli/LlmClient.cs
    - text/encounter-tool/skills/arc-colorize/SKILL.md
  features: [pipeline, colorize, factual, voice, forge, peer-json]
provenance:
  author: claude
references:
  - ~/repos/narr/forge            # the definitive, working pipeline (Python)
  - ~/repos/narr/RESEARCH.md      # the full journey: what failed, why forge won
  - ~/repos/narr/forge/critic.plan.md
---

# Forge → .NET

## Why

The encounter-authoring pipeline we built inside `EncounterCli`
(`colorize` → `factual` → `voice` → `critic`) **failed at every stage** —
bland repetitive color, incoherent factual integration, voice that either
fabricated or barely changed the text, and a critic that rejected good prose
on invented grounds (see `RESEARCH.md` §"encounter-tool — infrastructure yes,
pipeline no"). After a long series of throwaway spikes under `~/repos/narr/`
(`colorize`, `factual`, `loom`, `loom-author`, `voicer`…), a **definitive
process** crystallised in **`~/repos/narr/forge`**. It works; it is proven
round-trip and ship-quality on the `the_villa` test arc.

The bad news: forge is **Python**, and it no longer resembles what we built in
`EncounterCli`. The data model is different (peer-JSON sidecars, not inline enc
comment blocks), the stages are different, and the breakthrough (threaded
"story so far" synthesis) has no analogue in the old tool.

This plan: **delete the failed EncounterCli pipeline stages** and **port the
forge process into a new `.NET` project**, keeping the parts of EncounterCli
that work (parse/check/bundle/walk + the decompose skill) and handling the
Cloudflare Workers credential safely.

**Decisions locked (2026-06-20):**
- Port lives in a **new `Forge` project** in `text/encounter-tool/Encounter.sln`
  (clean break from the inline-block legacy; references `Dreamlands.Encounter`).
- The **color stage stays an opaque imp service** (loom: gemma triplet + LoRA
  logit-steering, Python on the GPU box). The .NET `color` command only
  orchestrates: ship a beats-job, run the "magic shell stuff," get enriched
  beats back. Invocation details live in `appsettings.json`. The steering rig is
  **not** ported.
- A FOSS-friendly fallback color provider (GLM at very high temperature) is
  designed-for behind the same interface, **stubbed/documented, not built** —
  an inferior DIY path for anyone outside our imp setup. We will not run it.

---

## The forge process (what we are porting)

Forge is ~1,180 lines of clean, single-responsibility Python. The spine is a
**peer JSON sidecar** per `.enc` (`X.enc.json`): the `.enc` is **never
modified**; the sidecar accumulates, per FIXME beat, the original stub plus
every phase's output; a final `integrate` pass splices approved prose into a
**fresh** `.enc` under `out/`. Line numbers are the splice key, kept stable
*because* the source is never rewritten, and guarded by a `source_sha`.

```
.enc (FIXME beat stubs, read-only)
   │  parse        enc → X.enc.json skeleton            free, local
   ▼
peer JSON ──► categorize   tag each beat 1 of 6 tones   GLM on imp, free
   │
   ▼  color       gemma triplet enriches each beat       imp service, free, SLOW
   │              (the anti-"Claude-tic" alien texture)
   ▼  synthesis   beat + color → finished prose          Cloudflare gateway, PAID
   │              reasoning model as a LOGIC FILTER
   │
   ▼  weave       threaded "story so far" synthesis:      Cloudflare gateway, PAID
   │              walk one path (thread.py), generate
   │              beats in ORDER, feeding each finished
   │              paragraph forward → kills POV drift +
   │              factual leakage (THE breakthrough)
   ▼  (compare)   model A/B eval harness (intervention   PAID, optional
   │              rate), pick the integrator
   ▼  (critic)    dumb closed-list invariant checker      planned, not built
   │
   ▼  integrate   peer JSON → out/<arc>/<name>.enc        free, local
finished .enc (round-trips byte-identical with nothing approved)
```

### Per-file map (Python → responsibility)

| forge file | LOC | what it does |
|---|---|---|
| `enc.py` | 42 | FIXME-beat regex extractor (`FIXME:` / `FIXME(tone):`, choice tracking) |
| `peer.py` | 80 | peer-JSON schema: build / merge / load / save / sha |
| `parse.py` | 37 | phase: `.enc` → peer JSON skeleton (idempotent, preserves prior work) |
| `categorize.py` | 85 | phase: tag each beat dread/horror/wonder/mundane/action/revelation (GLM) |
| `color.py` | 112 | phase: orchestrate the imp gemma triplet over uncoloured beats |
| `clients.py` | 107 | GLM (imp, free) + Cloudflare gateway (paid) HTTP clients; quota/empty/retry handling |
| `config.py` | 29 | endpoints, imp IO contract, tones, Cloudflare key file + integrator model |
| `synthesis.py` | 176 | phase: per-beat integrator (the SYSTEM "logic filter" prompt) + `--dry-run`/`--compare` |
| `thread.py` | 113 | walk one navigable path through the enc choice/tag graph → ordered beats |
| `weave.py` | 151 | phase: threaded story-so-far synthesis along a thread |
| `compare.py` | 126 | model A/B eval harness (one beat per tone × N integrators) |
| `integrate.py` | 51 | phase: peer JSON → fresh `out/<arc>/<name>.enc`, splicing approved finals |
| `imp/color_beats.py` | 70 | **stays on imp** — the gemma decode service (NOT ported) |

### Substrate (unchanged, human/decompose-authored)

`TheArc.md`, `_scenes.md`, `_cast.md`, `_set.md`, `_color.md` (recurring motifs
only), and per-scene `SceneName.lens.md`. The lens is the highest-leverage steer
for color; `_color.md` is recurring motifs only (one-offs are per-scene color).
These are produced upstream (decompose skill) and are not part of this port.

### Why the design is the way it is (carry these into the port verbatim)

- **Synthesis is a logic filter, not a stylist.** The gemma color bank is vivid
  but produced *without reasoning* — it commits two specific errors the
  integrator must catch: (1) figure-stated-as-literal-fact ("his hands became
  pistons"), (2) continuity/scale/timeline breaks. No ban lists; the policing is
  reasoning. The full SYSTEM prompt in `synthesis.py` is load-bearing — port it
  near-verbatim.
- **Thinking must stay ON for synthesis.** `/nothink` reproduced gemma-like
  incoherence. (RESEARCH.md §"The synthesis problem".)
- **Threaded story-so-far beats isolated per-beat synthesis.** Feeding the prior
  finished prose forward is a far stronger POV+fact anchor than any rule, and
  the lens is dropped when a story-so-far is present. This was the fix that took
  the_villa from ~50% needing rewrites to ship-quality.
- **Color exists to break "Claude-tic" contamination.** decompose is written by
  Claude; its tics ("she clocks your ring") bake into the FIXME stubs and survive
  every downstream pass on every model. The gemma triplet injects texture
  genuinely alien to Claude. This is *why* color is load-bearing and why the FOSS
  GLM-high-temp fallback is explicitly inferior.

---

## What we trash in dreamlands

Delete from `text/encounter-tool/EncounterCli/` (all FAILED stages + their now-orphaned support):

| file | LOC | reason |
|---|---|---|
| `ColorizeCommand.cs` | 629 | failed drain-loop color; superseded by imp loom |
| `FactualCommand.cs` | 461 | failed; superseded by synthesis (logic-filter) |
| `VoiceCommand.cs` | 209 | failed Qwen/DSPy voice; abandoned approach |
| `CriticCommand.cs` | 344 | miscalibrated taste critic; replaced by the dumb closed-list critic (planned) |
| `FactualEval.cs` | 214 | parity harness for the dead factual stage |
| `DraftBlocks.cs` | 330 | the inline `# --- COLOR/FACTUAL/VOICED ---` block model — **forge abandons this** for peer-JSON sidecars |
| `ExpandClient.cs` | 119 | Qwen3/DSPy client for the dead voice stage |

**~2,306 LOC removed.** `Program.cs` dispatch loses `colorize`/`factual`/
`voice`/`critic`. `GlmClient.cs` (69) and `LlmClient.cs` (110) — see "reuse"
below; likely retire `LlmClient` (Anthropic SDK) and rebuild a GLM client in
Forge rather than share the old one.

**Content cleanup (already underway):** the inline COLOR/FACTUAL/VOICED comment
blocks littering the scrub arc `.enc` files are being stripped (commit
`0999870` "scrub arcs: strip COLOR/FACTUAL junk"). The peer-JSON model means the
`.enc` is pristine and all work lives in sidecars, so this cleanup is exactly
right and should continue. Also delete the stale `eval/factual-parity/` fixtures
and retire `skills/arc-colorize/SKILL.md` (rewrite as a forge-color operator
guide in a later pass).

## What we keep

- **`Dreamlands.Encounter`** parser/model library — the right substrate, reused
  by Forge.
- **EncounterCli** commands that work: `check`, `bundle`, `walk`, `fixme`/
  `generate`, `haul-generate`. Untouched.
- **`arc-decompose` skill** — Stage-1 skeleton authoring. The single source of
  truth for stub-craft. Untouched.
- The **`appsettings.json` (gitignored) + `appsettings.example.json` (committed)**
  config pattern — already correct; extend it (see Config below). *Verified
  2026-06-20: the real key is NOT in git; the `sk-ant-` strings in history are
  placeholders in the example file.*

---

## Target architecture — the `Forge` project

New project `text/encounter-tool/Forge/` added to `Encounter.sln`, referencing
`Dreamlands.Encounter` (and optionally `Dreamlands.Game` — see reuse). A single
CLI with subcommands mirroring forge's phases.

```
Forge/
  Program.cs                 subcommand dispatch
  Beats.cs                   port of enc.py — FIXME-beat extraction (regex)
  Peer.cs                    port of peer.py — peer-JSON schema (build/merge/load/save/sha)
  PeerDocument.cs            POCO model for X.enc.json (System.Text.Json)
  commands/
    ParseCommand.cs          enc → X.enc.json
    CategorizeCommand.cs     tone-tag beats (GLM)
    ColorCommand.cs          orchestrate imp loom (opaque)  ── uses IColorProvider
    SynthesisCommand.cs      per-beat integrator (logic filter)
    WeaveCommand.cs          threaded story-so-far synthesis
    CompareCommand.cs        model A/B eval (optional, later)
    IntegrateCommand.cs      peer JSON → out/<arc>/<name>.enc
    CriticCommand.cs         dumb closed-list invariant check (later phase)
  clients/
    GlmClient.cs             imp GLM (free, OpenAI-compatible)
    GatewayClient.cs         Cloudflare AI Gateway (paid) — port of clients.py
  color/
    IColorProvider.cs        the one real plugin boundary
    ImpLoomColorProvider.cs  ships beats → imp → enriched beats (opaque shell)
    GlmHighTempColorProvider.cs  STUB — FOSS DIY fallback, documented, not run
  Thread.cs                  thin adapter over Dreamlands.Game/EncounterRunner (NOT a port of thread.py)
  appsettings.example.json   committed placeholder
```

### The peer-JSON model (the spine — port faithfully)

```jsonc
{
  "schema": 1,
  "source": "The Decision.enc",
  "source_sha": "<sha256 of the .enc at parse time>",
  "speaker": null,                       // file-level dialog speaker, if any
  "beats": [{
    "line": 12, "indent": "  ", "tone": "dread",
    "choice": "Slip in through the servants' gate",
    "original": "<FIXME stub>",
    "stages": {                          // each phase writes ONLY its own key
      "color":            { "enriched": "...", "facts": [...], "register": "...", "length": "..." },
      "synthesis":        { "<model>": { "text": "..." } },
      "synthesis_sofar":  { "<model>": { "text": "..." } },
      "critic":           { "critic_model": "...", "flags": [...] }
    },
    "final": null, "approved": false
  }]
}
```

Invariants to preserve exactly:
- **Never write the `.enc`.** Anything a phase needs goes in the sidecar.
- **Line is the splice key**; `parse.merge` carries forward prior work by matching
  `(line, original)`; a changed line resets that beat.
- **`integrate` verifies `source_sha`** and aborts on drift rather than splicing
  against stale line numbers.
- **Missing upstream stage = a legible "run that phase first" error**, never a
  silent skip.
- **Persist each paid result immediately** (crash-safe / resumable).

### Color as the one real plugin boundary

`IColorProvider.EnrichAsync(IReadOnlyList<BeatJob>) -> IReadOnlyList<ColorRecord>`.

- `ImpLoomColorProvider` (default, what we run): build the beats-job JSON, scp to
  imp's loom-io inbox, ssh-run `grind`, scp results back, merge. All endpoints,
  ssh target, grind path, and sampling knobs come from `appsettings.json`. The
  loom internals stay undocumented and Python — we ship beats, we get enriched
  beats. (Implement via `Process.Start` for ssh/scp, or SSH.NET if we want to
  avoid shelling out.)
- `GlmHighTempColorProvider` (stub): same contract, single GLM call per beat at
  very high temperature, no steering. Documented as the inferior FOSS path so a
  DIYer without imp can run an end-to-end pipeline. **Not built now.**

Per the global guideline "no interfaces unless there's a real extension boundary"
— this *is* one (two genuinely different backends), so the interface is justified;
nothing else in Forge gets an interface.

---

## Reuse opportunities

1. **Graph walk: use the real engine (DECIDED 2026-06-20).** `thread.py`
   re-implements a *simplified* enc choice/tag engine (only `&&`/`!tag` in
   `requires`, `+open`/`+add_tag`/`+finish/flee_dungeon`). That re-implementation
   was hitting parsing problems in Python — **the actual trigger to halt forge and
   merge back into the .NET ecosystem.** So `Thread.cs` is a thin adapter over
   `Dreamlands.Game`/`EncounterRunner` (the engine behind the `walk` command),
   emitting the ordered `(source, line)` beat spine. This deletes the divergent
   second parser outright and guarantees the thread matches actual play — the
   whole point of coming home to .NET. Do NOT port `thread.py`.
2. **FIXME-beat extraction.** `Dreamlands.Encounter` parses to a structured model,
   not line-indexed FIXME beats. Port `enc.py`'s `FIXME_RE`/`CHOICE_RE` as a small
   standalone `Beats.cs` (42 lines); don't try to bend the structured parser to a
   line-index model.
3. **GLM client.** The old `GlmClient.cs` exposes `min_p`/`enable_thinking` knobs
   we still want (thinking ON for synthesis). Rebuild a clean version in Forge
   rather than share the EncounterCli one being partly dismantled.

---

## Config & secrets

Extend the existing (already-gitignored) `appsettings.json` pattern. **No
reading `~/repos/narr/cloudflare.key`** — the credential lives inside the repo's
gitignored config, never a path outside it.

```jsonc
// appsettings.example.json (committed, placeholders only)
{
  "Glm":     { "Endpoint": "http://imp:8080/v1", "Model": "auto" },
  "Gateway": {
    "Endpoint": "https://gateway.ai.cloudflare.com/v1/<ACCOUNT>/<GW>/compat/chat/completions",
    "ApiKey": "<CLOUDFLARE_AI_GATEWAY_KEY>",
    "IntegratorModel": "workers-ai/@cf/moonshotai/kimi-k2.6"
  },
  "Imp": {                    // opaque loom invocation — magic shell, undocumented internals
    "SshTarget": "joseph@imp",
    "LoomInbox": "~/loom-io/inbox",
    "LoomOutbox": "~/loom-io/outbox",
    "Grind": "~/repos/loom/bin/grind"
  }
}
```

Secret-handling requirements:
- `appsettings.json` is gitignored — **verified already true** (`.gitignore:8`,
  not tracked). Forge reads its config from the same gitignored file (or
  `--config <path>`). Add the Forge config keys to the existing example file.
- Add a belt-and-suspenders `*.key` line to `.gitignore` in case anyone drops a
  raw key file beside the binary.

### Sidecars & `out/` — gitignored (DECIDED 2026-06-20)

Peer JSON sidecars live **beside** the arc `.enc` files (not a separate repo) but
are **gitignored**, as is `out/`. Add to `.gitignore`: `*.enc.json`, `out/`,
`*.key`. Rationale:
- The sidecar is **scratch/work-product**: rewritten on every stage and saved
  per-beat — committing it means megabyte-scale noisy diffs of model text on every
  run. The **product** is the promoted/integrated `.enc`; that is what version
  control should track.
- Keeps accumulated third-party model output (gemma/kimi/glm/gpt-oss) out of the
  published game repo, and keeps the **fine-tune corpus off the public repo** —
  consistent with the FT-on-imp decision below.
- Tradeoff accepted: we lose the in-repo git audit trail forge's standalone repo
  enjoyed. Mitigated because the only *irreplaceable* cells are paid synthesis
  results, which are saved per-beat (crash-safe) and should be pushed to imp for
  corpus retention anyway (below); free stages re-run at will.
- Port the gateway client's hard-won details from `clients.py`:
  - **Real `User-Agent` (`curl/8.5.0`)** — Cloudflare's WAF 403s the default
    .NET/`Python-urllib` UA as a bot *before auth*.
  - **`QuotaExhausted` (daily free-neuron cap, internalCode 4006)** — not
    transient; abort the whole run, never back off into it.
  - **`EmptyCompletion`** — a think-only/soft-refusal answer is a real failure;
    retry then surface, never store a blank cell.
  - **Backoff honouring `Retry-After`** on 429/5xx.
  - **`strip_think`** — drop any `<think>…</think>` preamble, keep what follows.
- Never log the key; redact in any dry-run/prompt dump.

---

## Porting risks

- **imp orchestration (color).** The one genuinely awkward piece: shelling out to
  `ssh`/`scp` against a live GPU box. Mitigated by the opaque `IColorProvider`
  boundary and config-driven paths; loom itself is untouched Python. Keep the
  `--dry-run` (build the beats-job, don't ship) and `--limit 1` smoke paths.
- **Gateway quirks** — covered above; they are the load-bearing 20% of
  `clients.py`. Port them with their comments.
- **Prompt fidelity** — the synthesis SYSTEM prompt and the weave story-so-far
  framing are the product. Port them as exact string constants; don't paraphrase.
- **JSON-vs-line discipline** — the entire correctness story rests on never
  rewriting the `.enc` and matching beats by line. C# `string.Split('\n')` must
  preserve indices identically to Python `splitlines()` (watch trailing newline /
  `\r\n`). Add a round-trip identity test (parse → integrate with nothing approved
  == byte-identical) as the first regression gate, mirroring forge's the_villa
  proof.
- **Slowness** — color is an overnight run; nothing in .NET changes that. Commands
  must be resumable and idempotent (forge already is — preserve it).

---

## Phasing

1. **Skeleton + spine. ✅ SHIPPED 2026-06-21.** New `Forge` project in
   `Encounter.sln` (net10.0, `forge` binary, zero deps); `Beats.cs`,
   `Peer.cs`/`PeerDocument.cs`, `Cli.cs`, `ParseCommand`, `IntegrateCommand`.
   Gate PASSED: the_villa round-trips byte-identical (5/5, cmp + sha) AND the
   generated `source_sha` matches forge's own `Start.enc.json` bit-for-bit (39
   beats, matching forge's count) — the port is spine-compatible with the Python,
   not merely self-consistent. Idempotent re-parse + drift-guard abort both
   verified. `.gitignore` += `*.enc.json`, `out/`, `*.key`.
   Deleted from EncounterCli (build green, 0 warnings): `ColorizeCommand`,
   `FactualCommand`, `VoiceCommand`, `CriticCommand`, `FactualEval`, `DraftBlocks`,
   and the orphaned clients `GlmClient` → `QwenClient` → `LoraClient` (9 files).
   **Kept `ExpandClient` + `voices/`** — the surviving `fixme` command depends on
   them (not just dead `voice`); deviates from the original delete list to avoid
   breaking `fixme`. Stale `eval/factual-parity/` fixtures + `arc-colorize` skill
   left for the docs phase (Phase 7).
2. **Free GLM stages. ✅ SHIPPED 2026-06-21.** `Config.cs` (gitignored
   appsettings.json, imp:8080 defaults), `GlmClient.cs` (OpenAI-compatible chat +
   `/models` id resolution + `StripThink`; `data[0].id` — imp returns a hybrid
   models/data body, `data` present), `CategorizeCommand.cs`. Prompts byte-identical
   to `categorize.py`. Gate PASSED: 31/39 (79%) tone agreement with forge on
   the_villa (Start 8/8 exact); the 8 deltas are all adjacent/ambiguous calls =
   GLM temp=0.3 variance (forge's tones are one sample, not truth), not a port
   defect. `--dry-run` verified offline prompt assembly incl. `Choice context:`.
3. **Paid synthesis. ✅ SHIPPED 2026-06-21.** `Config.cs` += `Gateway` section
   (gitignored appsettings.json), `GatewayClient.cs` (curl UA vs WAF, 4006
   QuotaExhausted abort, EmptyCompletion retry, Retry-After backoff),
   `SynthesisCommand.cs` (logic-filter SYSTEM as exact 1655-char raw string +
   `build_user`; `--dry-run`/`--force`/`--model`/`--limit`/`--beats`/`--no-lens`).
   Gate PASSED: `synthesis --dry-run --force` is BYTE-IDENTICAL to
   `synthesis.py --dry-run` across all 39 the_villa beats (2711 lines each, diff
   empty). Paid path ported but not live-fired (no spend); live one-beat smoke
   deferred to the user's discretion. appsettings.example.json += Gateway placeholder.
4. **Threaded weave.** `Thread.cs` (adapter over `Dreamlands.Game`/`EncounterRunner`) + `WeaveCommand`.
   Gate: the_villa "tell Vastand" thread (19 beats) produces ship-quality prose,
   no POV drift / factual leakage — forge's acceptance bar.
5. **Color provider.** `IColorProvider` + `ImpLoomColorProvider` + `ColorCommand`
   (opaque imp orchestration). Gate: `--dry-run` builds a correct beats-job;
   `--limit 1` round-trips one enriched beat from imp.
6. **Eval + critic (optional/last).** `CompareCommand`; then `CriticCommand` per
   `critic.plan.md` (deterministic POV pre-pass + one per-thread GLM checklist
   call; closed six-kind list; default-pass; every flag cites a verbatim span).
   Stub `GlmHighTempColorProvider` + a short DIY note.
7. **Docs.** Rewrite `skills/arc-colorize` as a forge-color operator guide; add a
   `Forge/CLAUDE.md` (port forge's gotchas); update the main `CLAUDE.md` pipeline
   section; refresh memory.

---

## Decided

- **Thread walk → real engine.** `Thread.cs` adapts `Dreamlands.Game`/
  `EncounterRunner`; `thread.py` is not ported. (2026-06-20 — see Reuse §1.)
- **Sidecars & `out/` → gitignored, beside the files.** (2026-06-20 — see Config.)

## Fine-tune corpus & distillation (DECIDED 2026-06-20)

Keep the corpus, but treat it like loom: **opaque to dreamlands, lives/trains on
imp, weights never distributed.** Maybe a future game uses it, maybe never.

- **Retain `final` + provenance.** Persist each approved `final` in the sidecar
  (it's a free byproduct, expensive to reconstruct). The peer JSON already keys
  outputs by model (`stages.synthesis[<model>]`); also record which model produced
  the chosen `final`, so the corpus is mechanically filterable by provenance.
- **The corpus flows to imp, not the repo.** Since sidecars are gitignored, the FT
  corpus is exported to an imp-local repo through the same opaque channel as the
  gemma color service. Training rig + weights stay on imp; dreamlands never holds
  them and never ships them.
- **Distillation is clean only with three guards** (the licence check, 2026-06-20):
  1. *Open-weight integrators only.* kimi-k2.6 (modified MIT), glm-5.x (Apache 2.0),
     gpt-oss-120b (Apache 2.0) and gemma color (Gemma Terms — Google claims no
     rights in outputs) are all distill-safe. The `compare.py` "salad bar" can
     reach proprietary API models (claude-opus, gpt-5.5, gemini, grok) whose ToS
     forbid training competing models — **never let those become a `final`** in the
     corpus. Provenance tagging makes this a filter.
  2. *The stub is Claude's.* arc-decompose (Claude) authors the FIXME stub, which
     sits in the *input* of every training pair regardless of integrator — the
     largest and most universal exposure. For a private/internal FT (our case) the
     stakes are low; if a distilled model ever shipped commercially, regenerate
     stubs with an open model first (or move decompose off Claude).
  3. *Access channel.* Confirm Cloudflare Workers AI / AI Gateway service terms add
     no no-train clause on top of the permissive model licences.
  (Not legal advice — read each model card before anything commercial.)

## Open questions

- **Compare/critic scope:** build now or defer until the core 4 stages are
  shipping arcs? (Lean: defer; they are eval/QA, not authoring-critical.)
