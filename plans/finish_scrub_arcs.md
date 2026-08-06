---
kind: plan
title: "Finish the four scrub arcs — promote the_villa, then run the rest to completion"
state: active
created: 2026-07-27
updated: 2026-07-27
status: active — completion plan for the 4 scrub arcs after a ~1 month pause. Supersedes the "Order (DECIDED)" section of plans/scrub_arc_processing.md (which remains the operational per-arc procedure + safety guarantee). Order is now the_villa → signal_array → relay_post → foundry. Blocking prerequisites before arc #2: threads must move out of C# into a data file, and the minrouter smoke test must pass.
touches:
  files:
    - text/encounters/arcs/scrub/the_villa/
    - text/encounters/arcs/scrub/signal_array/
    - text/encounters/arcs/scrub/relay_post/
    - text/encounters/arcs/scrub/foundry/
    - text/encounter-tool/Forge/Threads.cs
    - text/encounter-tool/Forge/ThreadCommand.cs
    - text/encounter-tool/Forge/WeaveCommand.cs
    - project/encounter-spec/authoring_flow.md
    - plans/arc_authoring_workflow.md
    - plans/scrub_arc_processing.md
  features: [forge, pipeline, scrub-arcs, authoring, threads]
provenance:
  author: claude
references:
  - plans/scrub_arc_processing.md          # the per-arc operational procedure
  - plans/forge_dotnet_port.md             # the pipeline itself (shipped)
  - text/encounter-tool/skills/forge-run/SKILL.md
  - ~/repos/narr/forge                     # the original Python forge (frozen 2026-06-20)
---

# Finish the four scrub arcs

Work paused early July with `the_villa` complete but unpromoted and the other three
arcs untouched by Forge. This plan carries all four to done.

It **supersedes the "Order (DECIDED)" section** of `plans/scrub_arc_processing.md`.
That document remains authoritative for the per-arc *procedure* and the safety
guarantee; this one owns the *order*, the prerequisites, and the finishing work.

## Where things actually stand (verified 2026-07-27)

| Arc | `.enc` | lens | bibles | sidecars | FIXMEs | next |
|---|---|---|---|---|---|---|
| `the_villa` | 5 | 5 | all 4 | **39/39 woven, curated** (in narr) | 0 in `out/` | **promote** |
| `signal_array` | 10 | 10 | all 4 | none | 67 | threads |
| `relay_post` | 6 | 6 | all 4 | none | 50 | threads |
| `foundry` | 7 | **0** | `_scenes` only | none | 58 | 3 bibles + 7 lens, then threads |

All 28 scrub files pass `encounter check`. No arc but `the_villa` has entered Forge
at all — zero sidecars means not even the free stages have run.

### Two corrections to the 2026-07-26 notes

1. **The narr copy is not Tier-2 isolation.** `~/repos/narr/forge` is the original
   **Python** forge, its own git repo, with `arcs/the_villa/` tracked as the corpus
   the pipeline was developed against. Every Python commit lands on 2026-06-20; the
   .NET port shipped and was live-validated 2026-06-21, the day after, and has since
   gained `ReviewCommand` and the `forge-run` runbook. **.NET is ahead of Python**,
   not behind — the villa's state sits in narr only because later .NET runs were
   pointed at that path.
2. **The curated sidecars were tracked-but-uncommitted**, not gitignored. Committed
   2026-07-27 as `2ae9d9a` in `~/repos/narr/forge`.

### What the villa's curation actually consists of

Load-bearing for how carefully we review before promoting. **No tool in either
codebase writes `final` or `approved`** — `peer.py` documents the intent
(`"final": null, # human-approved prose; null until chosen`), and every stage
writes only into `stages.*`. So that layer is human by construction. Its contents:

- **36 of 39** `final` values match a kimi-k2.6 cell byte-for-byte. Consistent with
  either careful per-beat acceptance *or* a scripted bulk copy — the artifact cannot
  distinguish them.
- **3** match no generated cell anywhere (`The Decision:5`, `The Decision:11`,
  `The Early Pages:24`) — hand-written repairs, unreproducible.

Conclusion: mostly accepted-as-generated over a thin irreplaceable hand layer.
**The promotion read-through is therefore a real gate, not a formality** — it is
where the acceptance of those 36 gets audited for the first time.

---

## T1 — Authoring flow reference *(do first)*

**Problem.** After a month away the flow is unrecoverable from the repo, and the
document you would naturally reach for is actively wrong.
`plans/arc_authoring_workflow.md` (524 lines, 2026-06-06) describes the **dead**
EncounterCli `colorize → factual → voice → critic` pipeline, two weeks before the
Forge pivot deleted it. `forge-run/SKILL.md` is correct but agent-facing, starts at
a substrate-complete arc, and never defines its own vocabulary.

**Deliverable.** `project/encounter-spec/authoring_flow.md` — one human-readable
reference for the whole span, citing the skills rather than duplicating them so
there stays one source of truth per rule.

Contents:

1. **Vocabulary.** beat, thread, lens, bible, color bank, peer JSON / sidecar,
   `final` / `approved`, tone tag.
2. **Stage 0–1 — substrate.** `arc-sketch` → `_scenes` / `_cast` / `_set` /
   `_color` + per-scene `*.lens.md`. The cheap-`.md`-leads rule.
3. **Stage 2 — skeleton.** `arc-decompose` → FIXME-stub `.enc`.
4. **Stage 3 — Forge.** `parse → categorize → color → ★review gate★ → threads →
   weave → curate → integrate → promote`. Per stage: free vs paid, which box, which
   model, resumability.
5. **Where state lives.** Sidecars follow the arc path, `out/` follows cwd, both
   gitignored. The villa's four-plausible-locations trap.
6. **Cold-start checklist.** What to run, in order, to resume an arc after a gap.

**Also:** mark `plans/arc_authoring_workflow.md` `state: superseded` with a pointer
header. Before doing so, salvage anything still live — its §6b
(unit-and-representation problem raised by revisitable hubs) and §8 (author UX)
were never resolved by the pivot and should move rather than die.

## T2 — Promote `the_villa`

Cautious by design: **do not copy sidecars into this repo.** They are gitignored, so
they would land as invisible untracked junk; narr now holds them committed, which is
the archive. Only the five integrated `.enc` move.

1. **Pre-flight** (all four currently hold): `source_sha` matches between narr and
   repo; `0` FIXME across `text/encounter-tool/out/the_villa/*.enc`; `encounter
   check` clean; diff vs source is pure FIXME→prose swaps.
2. **Branch** `forge/the_villa` off `combat-pivot`.
3. **★ REVIEW GATE ★** — render all five FIXME-stub → prose diffs; human reads every
   beat. Nothing lands before this. Per the section above, this is the first real
   audit of the 36 accepted beats.
4. **Promote** — copy the 5 `.enc` from `out/the_villa/` over the canonical 5. This
   overwrites tracked files, so it produces a reviewable git diff, not a file drop.
5. **Verify** — `encounter check` + `encounter bundle`; confirm the game still loads.
6. **Cleanup** (the anti-mess step). Delete the stale in-repo sidecars
   (`text/encounters/arcs/scrub/the_villa/*.enc.json` — 33 woven / 0 approved, pure
   confusion) and both stray output dirs: `text/encounter-tool/out/` and the older
   repo-root `out/compare/`. All gitignored; nothing is lost.
7. **Commit.** No push without an explicit ask (`feedback_no_unprompted_push`).

**Exit criterion:** the villa is prose-complete on the branch, the repo contains no
generated-artifact residue, and narr remains the archive of record for its sidecars.

## T3 — Get threads out of C# *(blocking for arc #2)*

**Problem.** Threads — the ordered choice-label paths that define a playthrough for
weaving — are a hardcoded `Dictionary<string, string[]>` in `Forge/Threads.cs`,
containing only `the_villa`'s five. Authoring content for a *new* arc therefore means
editing a source file and rebuilding the tool. That is content living in code, and it
blocks every remaining arc.

**Deliverable.** Per-arc `_threads.json`, beside the bibles, loaded at runtime.

JSON, not YAML: Forge is already `System.Text.Json`-native (peer JSON), so this adds
zero dependencies. YamlDotNet exists in the solution but only in mapgen.

```json
{
  "default": "vastand",
  "threads": {
    "giveover": {
      "note": "The hand-it-over-unread ending. Covers The Decision's 'Let him make of it what he will' beats.",
      "path": [
        "Slip in through the servants' gate",
        "Set the journal down and decide",
        "Carry the journal out and hand it over without a word"
      ]
    }
  }
}
```

The `note` field is not decoration — the existing C# comments explain each thread's
*coverage intent* (why `arson` is a grand tour, which beats it exists to sweep up).
That reasoning is the hardest part to reconstruct and must survive the move.

Steps:

1. Add the loader + model; resolve `_threads.json` from the arc dir.
2. Port the villa's five threads verbatim, comments → `note`.
3. Repoint `WeaveCommand` and `ThreadCommand` at the loader; **delete `Threads.cs`**
   (no fallback — a stale second copy is the whole problem).
4. Regression-check against the villa: `forge thread` must produce identical beat
   spines to the current hardcoded walk. Cheap and decisive, since the villa is
   fully woven.
5. Error clearly on a missing/malformed file, and on a label that matches no
   `OptionLink` — a silently-dropped label yields silently-uncovered beats.

**Worth considering while in here:** a `--coverage` flag reporting which beats no
thread reaches. The runbook already calls for a coverage diff after weaving; today
that is manual, and an uncovered beat is exactly the failure that pushes work toward
isolated per-beat synthesis, which we do not use in production.

## T4 — Remaining prerequisites

- **Router smoke test** (blocks any paid weave). The minrouter migration
  (`RouterClient.cs`, `GlmClient`/`GatewayClient` deleted) is uncommitted and
  unvalidated against a real call. `GET /v1/models` 404s at `imp:8086`; use
  `GET /help` with bearer to list upstreams and confirm the exact path Forge calls
  **before** spending.
- **Commit the router migration** to the base branch, not an arc branch — keep arc
  diffs purely prose.
- **`signal_array/Start.enc:7`** — the one bare `FIXME:` with no `(tone)` tag in the
  whole scrub set. An untagged beat gets no tone steer into color. Fix at the
  substrate layer before parse.

## T5–T7 — The remaining arcs, in order

Each runs the full `forge-run` loop to completion before the next begins. Order
revised 2026-07-27; `foundry` moved from second to last.

### T5 — `signal_array` (10 enc, 67 FIXME)
Substrate-complete. Author threads (first arc on the new `_threads.json` format —
expect to shake out the format here), then the standard loop. Largest arc of the
three, so its thread set will be the most demanding coverage problem.

### T6 — `relay_post` (6 enc, 50 FIXME)
Substrate-complete, smallest remaining. Author threads, run the loop. Its fictional
blocking problem is **solved — do not re-litigate** (see `scrub_arc_processing.md`);
`Midnight.enc` and the `signal_array` rest-interval scenes are staging-clean.

### T7 — `foundry` (7 enc, 58 FIXME)
Moved to last: largest upfront authoring gap of the three. Needs `_cast`, `_set`,
`_color` plus **7 lens files** before it can enter the pipeline at all. Its sketch
(22KB, PC-POV, `c808cd6`) and skeleton are done and deliberate — the skeleton was
deliberately restructured post-decompose (`8015f15`: `The Question` + `The Switch`
folded into `Control Room`). So this is a bounded authoring task against a finished
sketch, not an `arc-sketch` effort. Then threads, then the loop.

## T8 — Kimi K3 evaluation *(parallel, cheap)*

K3 released 2026-07-16: 2.8T params, 1M context, native vision, always-on reasoning,
open weights. On Cloudflare as **`moonshotai/kimi-k3`** — note the id drops the
`@cf/` prefix our config uses (`workers-ai/@cf/moonshotai/kimi-k2.6`), so adopting it
is a config change, not a string swap.

**Default recommendation: stay on kimi-k2.6.**

- **Pricing is backwards for this workload.** ~$3.00/M input and **$15.00/M output**
  vs K2's ~$0.60 / $2.50 — roughly 5× input, 6× output. Weave is output-heavy prose
  generation, precisely what that curve punishes.
- **Always-on thinking** adds latency to a stage already at ~58s/beat.
- **2.8T params rules out self-hosting** — no path to running it free on the 128GB
  box the way the loom and categorize stages run free on imp.
- **The assembler is not the bottleneck.** The design premise is that kimi rubber-
  stamps clean input and amplifies dirty input; quality lives in the upstream color
  gate. A stronger assembler mostly buys better repair of input we have decided not
  to feed it.

**But run the A/B**, because it is nearly free and the baseline is now perfect: the
villa's 39 committed kimi-k2.6 beats. Re-weave one thread (`vastand`, 8 beats) with
K3 into a scratch copy and read them side by side. Order **$0.15–0.30**. If K3 is
visibly better on *clean* input the question reopens honestly; if it is a wash, it is
settled for the remaining three arcs.

Sources: [Cloudflare model docs](https://developers.cloudflare.com/ai/models/moonshotai/kimi-k3/) ·
[OpenRouter pricing](https://openrouter.ai/moonshotai/kimi-k3) ·
[VentureBeat](https://venturebeat.com/technology/chinas-moonshot-ai-releases-kimi-k3-the-largest-open-source-model-ever-rivaling-top-u-s-systems)

---

## Sequence

```
T1  reference doc  ──▶  T2  promote the_villa  ──▶  T3  threads out of C#  ──▶  T5  signal_array
                            │                       T4  router smoke              │
                            └──▶ T8  K3 A/B (parallel, during the read-through)   ▼
                                                                            T6  relay_post
                                                                                  │
                                                                                  ▼
                                                                            T7  foundry
```

T1 first because everything else is harder to resume without it. T8 runs during T2's
read-through, since that is human-time anyway. T3 and T4 must both land before T5.

## Session checkpoint — 2026-08-01

**Everything is committed. Working tree clean. Nothing pushed.**

Seven commits on `combat-pivot`, plus one in `~/repos/narr/forge`:

| Commit | What |
|---|---|
| `2ae9d9a` *(narr/forge)* | the villa's curated 39/39 sidecars — the durability fix |
| `c350fc5` | forge: all LLM calls through minrouter |
| `af9be44` | forge: the villa's 3 coverage threads |
| `e4877d8` | scrub arcs: `Midnight.enc` hub continuity + `Chorik.enc` typo |
| `fdec774` | plans: this plan + supersede the old order |
| `c0d31f0` | TODO: event-forensics pre-launch item |
| `584b3e9` | **T1 done** — `project/encounter-spec/authoring_flow.md` |

### T1 is complete

`project/encounter-spec/authoring_flow.md` is the current reference. Three
dead-pipeline traps were retired in the same pass:
`plans/arc_authoring_workflow.md` (superseded, banner, salvage carried forward),
`project/encounter-spec/colorize/README.md` (runtime pointer corrected, content
kept), and the already-deleted `arc-colorize` skill / `EncounterCli colorize`
command confirmed gone rather than merely undocumented.

## Session checkpoint — 2026-08-04: T2 DONE

**`the_villa` is promoted.** Commit `0a03088` on `combat-pivot` (no arc branch —
`feedback_single_branch_workflow`). 39 FIXMEs → 0; `check` clean across 157 files;
`bundle` wrote 137 encounters; diff was body prose only, +49/−39, no tag/mechanics/
choice lines touched. The 4 remaining warnings are pre-existing (`villa.*` ids
absent from `known_ids.txt`).

**The review gate was deliberately reordered**, with the user's agreement: promote
first, then read and repair the tracked files in place. Rationale — curation *is* an
editing pass ([[feedback_curation_is_repair]]), so it wants a real edit surface and
versioned diffs rather than a read-only render against a gitignored dir. Nothing
shipped: the branch is unpushed. **Final tidying is in progress by the user directly
on `text/encounters/arcs/scrub/the_villa/*.enc`** (minor: repetition, richer text).

**Verdict on the output: among the best prose in the project.** One arc is not a
sample, but it establishes that this path works — and the load-bearing part is the
human gating *before* kimi (color review → full thread coverage → curation-as-repair),
not the integrator.

**Intermediate artifacts are kept on purpose** (may inform process improvement).
Durable: narr sidecars at `2ae9d9a`. Not durable, single gitignored copy:
`text/encounter-tool/out/the_villa/` and repo-root `out/compare/`. The step-6 cleanup
below is therefore **deferred, not skipped** — including the stale in-repo
`*.enc.json`, which still misreads as state.

### T3 DONE — threads are per-arc content (`c99d47a`)

`_threads.json` beside the arc's bibles; `Threads.cs` deleted with no fallback.
`ReviewCommand` was an unlisted third consumer and moved too. The villa's five
threads ported verbatim (comments → `note`, preserving coverage intent) and
regression-checked: all five spines byte-identical to the old hardcoded walk,
verified by building a worktree at HEAD and diffing real output.

Also adds **`forge thread <arc> --coverage`** (the T3 "worth considering" item, built
because signal_array needs it): walks every thread, unions the spines, reports what
nothing reaches, exits 1 while any beat is uncovered. On the villa it independently
reproduces 39/39, 0 uncovered. Author-fixable failures now print one line instead of
a stack trace, and a no-such-label error lists the choices that do exist.

### T4 DONE — router verified end-to-end, cf cap raised (`7f41674`)

Both paths confirmed against `GET /help`, then exercised through `RouterClient`
itself (not just curl): `POST /x/imp-glmchat/v1/chat/completions` (categorize, cold
auto-load ~30s) and `POST /x/cf/compat/chat/completions` (paid, real completion).
The router migration was already committed as `c350fc5`.

**The cf daily cap was 20 — raised to 250.** 20 could not finish a single arc (the
villa's `vastand` alone was 19 calls). A weave beat is ~1.2k prompt tokens ≈ 500
neurons ≈ **$0.006**, so a full arc weave ≈ $1 and a runaway now tops out ≈
$1.50/day. Config is `~/repos/minrouter/appsettings.json` on imp (not a git repo;
`.bak` beside it is the rollback), `systemctl --user restart minrouter`. Counters are
durable SQLite and survive restarts; **caps roll at UTC midnight**, not local.

Two findings folded in: **504 was not retried** while 500/502/503 were, so a
transient edge timeout killed a beat and forced paying for it twice (fixed); and a
low `max_tokens` returns an empty *billed* completion, because kimi's always-on
thinking eats the budget before `</think>` — the mechanism behind
`EmptyCompletionException`.

## Session checkpoint — 2026-08-05: T5 in flight, `signal_array` coloured

**Committed and clean.** Six commits since the last checkpoint:

| Commit | What |
|---|---|
| `67a1247` | `signal_array` Start.enc:7 — the one untagged beat, retagged `(wonder)` + rewritten |
| `9f5a00a` | first-pass thread set + the parser bug it exposed |
| `daeaabf` | **parser keeps choice-level mechanics; `check` fails on dropped ones** |
| `6a8dbd5` | `format.md` §3.2 E + `arc_patterns.md` §1a correction |
| `e16eb87` | `forge suggest-threads` + adopted thread set |
| `f2f5878` | TODO — arc reachability lint pointed at the working algorithm |

### Where `signal_array` is

- **Substrate**: complete, all 67 beats tone-tagged (authored, not LLM — **skip
  `categorize`**, `--force` would overwrite the deliberate `(wonder)` on Start:7).
- **Parsed**: 67 beats, sidecars match disk.
- **Threads**: 5 (`siesta` 54, `silence` 50, `withdraw` 46, `leave` 39, `roadside` 5),
  **60/67 beats covered**. The 7 uncovered are all `Grabbed.enc` — reachable only via a
  branch `Thread.Walk` cannot select (it takes the first branch's navigation). Open
  decision: add branch-forcing (`"Move on the housing#else"`) or let those 7 fall to
  isolated synthesis.
- **Colour**: 67/67 done (one overnight pass, 66 beats). **Sidecars are gitignored** —
  backup of the bank at `<scratchpad>/colorbank-backup/`.
- **Review gate**: rendered to `out/review/` (whole-arc + one per thread). Read
  `signal_array.review.siesta.md` first — it is the order kimi will see.
- **Not yet run**: weave (PAID, ~60 unique calls against a 250/day cf cap).

### Colour-bank repairs applied

12 hand fixes to `stages.color.enriched` across 5 beats, all cast renames:
`Start:7` invented names for all four crew (pre-empting a reveal the source withholds
until `Start:11`), `Start:11` `Foreman Hemlock`, `Aftermath:6` `Elias` (was the PC),
`Aftermath:13` `Elias Finch`, `Rest Interval:24` `Elias` (was Richard's unnamed brother,
plus a `Husbndry` typo), `Observe:4` `the Peterson dig`.

**IN FLIGHT at checkpoint:** re-colour of `Aftermath:13` (hallucinated an indoor scene
with lamplight and a Persian rug onto an outdoor dusk beat) and `Veran:4` (`chipped
Formica`, `fluorescent hum`, `Earl Grey`). Originals saved to
`<scratchpad>/recolor-originals.json`. **If the reboot killed it**, both beats will have
no `stages.color`; re-run `forge color <arc>` (it queues exactly the uncoloured ones,
~35 min cold), or restore from the saved originals.

### Known colour-bank characteristics — do NOT treat as defects

POV flips to first person, em-dashes, markdown asterisks and ~2× expansion are the
colour stage's normal signature; the villa's bank (which produced the project's best
prose) measures the same. Colour is a bank, not a draft — `weave` reads the `facts` list
alongside it and writes clean second-person prose. Still open on `signal_array` and worth
a human eye: **13 beats with anachronism** (concrete, sodium, plastic, obsidian ×4 in
`Completion`/`Grabbed`) — flagged, not fixed.

### Resume here

1. Verify the two re-coloured beats (or re-run them).
2. Read the review gate; re-colour anything else that fails.
3. Decide `Grabbed` branch-forcing.
4. `forge weave` per thread, `siesta` first.

---

### (historical) Resume point — T5, `signal_array`

Prerequisites are all clear. Substrate is clean (67 beats, all tone-tagged since
`67a1247`). Next: `forge parse`, then **author `_threads.json` to 67/67 coverage**,
driving it with `--coverage` until it exits 0 — that is the real work and the thing
most likely to slip at 10 files. Then categorize → color (overnight, box to itself)
→ review gate → weave → curate → integrate → promote.

### (historical) Resume point — T2, promote `the_villa`

Next action is the **review gate**: render the five FIXME-stub → prose diffs and
read them. Everything is pre-verified and still true as of this checkpoint:

- `source_sha` matches between narr and repo for all 5 files
- `0` FIXME across `text/encounter-tool/out/the_villa/*.enc`
- all 28 scrub files pass `encounter check`
- 36/39 finals match a kimi cell byte-for-byte; 3 are hand repairs

Then: branch `forge/the_villa`, copy the 5 `.enc` over the canonical ones, `check`
+ `bundle`, clean up the stale in-repo sidecars and both stray `out/` dirs, commit.

**Do not lose:** `~/repos/narr/forge/arcs/the_villa/` is the archive of record for
the curated sidecars. Now committed, so a reboot is safe.

### Still open before arc #2

T3 (threads → `_threads.json`) and T4 (router smoke test — `GET /help` with bearer,
confirm the path Forge calls before spending). T8 (K3 A/B) can run any time.

## Open questions

- **Does the post-weave continuity critic need building?** The cross-file
  prose-continuity class (`plans/arc_writer.md:298`) was deferred until the pipeline
  shipped a full arc end-to-end. The villa now meets that condition. **Check its woven
  output for arrival-restatement first** — weave's story-so-far may already suppress
  most of the class, which would make the tool unnecessary.
- **Should `forge lock` replace manual JSON curation?** Deferred pending the curation
  UX being felt across a few arcs. Revisit after `signal_array`, with two arcs of
  evidence rather than one.
- **Does `foundry` still want a lens per scene**, or has the color stage improved
  enough since June to run thinner? Decide when T7 starts, not now.
