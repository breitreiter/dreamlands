---
kind: plan
title: "Process the scrub-biome arcs through Forge (safely)"
state: exploring
created: 2026-06-22
status: exploring — procedure + readiness assessment for running the 4 scrub arcs through the Forge pipeline without risking the (good) originals. Depends on plans/forge_dotnet_port.md (pipeline shipped, live-validated 2026-06-21).
touches:
  files:
    - text/encounters/arcs/scrub/the_villa/
    - text/encounters/arcs/scrub/relay_post/
    - text/encounters/arcs/scrub/signal_array/
    - text/encounters/arcs/scrub/foundry/
    - text/encounter-tool/Forge/
  features: [forge, pipeline, scrub-arcs]
provenance:
  author: claude
---

# Process the scrub-biome arcs through Forge

The Forge pipeline is shipped and live-validated (`plans/forge_dotnet_port.md`).
This plan is the **operational procedure** for running the four scrub arcs through
it — and, above all, doing so **without risk to the originals**, which are in good
shape and must not be damaged.

## The safety guarantee (why this is low-risk by construction)

Forge's peer-JSON model is built so the source `.enc` is **never modified**:

- `parse`/`categorize`/`color`/`synthesis`/`weave` write **only** to the `X.enc.json`
  sidecar (gitignored) — never to the `.enc`.
- `integrate` is the only thing that emits a finished `.enc`, and it writes to
  **`out/<arc>/`** (gitignored), never over the source. With nothing approved it
  reproduces the source byte-for-byte (proven round-trip).
- `integrate` verifies `source_sha` and **aborts** if the source drifted since parse.
- `*.enc.json`, `out/`, `*.key` are all gitignored.

So the entire generation pipeline cannot touch a tracked file. **The only operation
that mutates a canonical `.enc` is the final promotion** (landing finished prose into
the repo) — and that is a deliberate, branch-gated, human-reviewed git step. All the
rigor goes there.

Two tiers of isolation; pick per appetite:

- **Tier 1 (default, in-place):** run the pipeline in the real arc dir. Sidecars
  accrue beside the originals but are gitignored; originals are read-only by
  construction. Simplest; safety is architectural.
- **Tier 2 (paranoia):** `cp` the arc to a gitignored scratch dir (e.g.
  `/tmp/forge-work/<arc>/` or a throwaway git worktree) and run everything there;
  the real repo is never even opened for write until promotion. Recommended for the
  first production arc until we trust the flow, then drop to Tier 1.

## Readiness assessment (scouted 2026-06-22)

| Arc | enc | substrate | threads | Pipeline-ready? |
|---|---|---|---|---|
| `the_villa` | 5 | full (4 bibles + 5 lens) | **vastand, cave** (in `Threads.cs`) | ✅ proven (live smoke) |
| `relay_post` | 6 | full (4 bibles + 6 lens) | none yet | substrate yes; **needs threads** |
| `signal_array` | 10 | full (4 bibles + 10 lens) | none yet | substrate yes; **needs threads** |
| `foundry` | 7 | **partial — only `_scenes.md` + brief** | none yet | **NO — needs lens + `_cast`/`_set`/`_color`** |

Originals are all committed clean; `the_villa`'s dreamlands `.enc` are byte-identical
to forge's authored copies.

## Prerequisites (gaps to close before/within processing)

1. **Foundry substrate. → DECIDED: separate later effort.** foundry has no lens files
   and is missing the `_cast`, `_set`, `_color` bibles. Color's highest-leverage steer
   is the per-scene lens; without it, color quality drops. foundry is **excluded from
   wave 1**; its bibles + per-scene lenses are authored as their own task
   (arc-sketch), and it feeds back into this procedure once ready.

2. **Thread definitions per arc.** `weave` (the quality path — threaded story-so-far)
   walks named threads; only `the_villa` has them (`Threads.cs`). relay_post and
   signal_array need thread plans authored — a covering set of ordered choice-label
   lists whose union touches **every beat** (each beat must appear on ≥1 thread, or it
   gets no woven prose). Author these against each arc's choice graph (use
   `forge thread <arc> --thread <name>` to dry-check a plan's spine before generating).
   - *Coverage check:* after defining threads, diff the union of their beat spines
     against the arc's full beat set; any uncovered beat falls back to isolated
     `synthesis` (lower quality — flag it).
   - *Open:* `Threads.cs` is currently a compiled-in dictionary. For N arcs we likely
     want threads loaded from a per-arc file (e.g. `_threads.md`/`.json` in the arc
     dir) rather than recompiling. Decide before relay_post.

3. **Curation / approve step (missing tool). → DECIDED: manual JSON for now.**
   `weave` writes `stages.synthesis_sofar[<model>]`; `integrate` only splices beats
   with `final` set + `approved:true`. Nothing bridges that yet. **For the first wave
   we do it by hand-editing the sidecar JSON** (set each approved beat's `final` to
   the chosen/repaired prose and `approved:true`), with the steps documented as we go;
   a `forge lock` helper gets built later once the curation UX has been felt in
   practice. Per `feedback_curation_is_repair`, curation is cull **+ repair** (an edit
   surface, not a checkbox) — so expect to edit prose in `final`, not just copy it.
   This remains the one real gap for end-to-end completion; the manual step is the
   interim bridge, not the destination.

## Per-arc procedure (the loop)

For each arc, on its own branch, Tier-1 or Tier-2 working copy:

1. **Branch.** `git switch -c forge/<arc>` off the clean baseline.
2. **Parse.** `forge parse <arc>` → sidecars. (Free, local.)
3. **Categorize.** `forge categorize <arc>` → tones. (GLM on imp; needs `glmchat`
   loaded. ~20s/beat.) Spot-check tones; `--force` re-tags outliers if needed.
4. **Color.** `forge color <arc>` → gemma color bank. (imp loom; **overnight per
   arc**, box-to-itself. loom is a script — free the box with `swap-model stop all`,
   then `color` drives `grind` over ssh.) Resumable; `--dry-run` first to verify the
   beats-job. Smoke one beat with `--limit 1` before committing the box overnight.
5. **Weave.** For each thread: `forge weave <arc> --thread <name> --config <cfg>`.
   (PAID Cloudflare gateway, ~1 min/beat with kimi+thinking; key in gitignored
   `appsettings.json`.) Crash-safe/resumable (per-beat save; re-run without `--force`
   reuses good cells, retries empties — verified). Renders a continuous-read `.md`
   per thread to `out/compare/` for review. Run uncovered beats through `synthesis`.
6. **Review & lock (curation).** Read each thread's continuous-read; cull + **repair**
   weak beats; set `final`+`approved` per beat (via the `forge lock` helper from
   prereq 3). This is the human quality gate — front-loaded rigor, no automated
   end-to-end (`feedback_pipeline_error_amplification`). Watch for the known subtle
   failures (`feedback_subtle_errors_and_ambiguity`): plausible-but-wrong facts,
   POV slips, ambiguity resolved wrong.
7. **Integrate.** `forge integrate <arc>` → `out/<arc>/*.enc` (FIXME lines replaced
   by approved prose; unapproved beats keep FIXME). `source_sha` guard protects
   against drift.
8. **Verify the product, not the source.** `forge check out/<arc>` (syntax clean,
   no FIXME left if fully locked). Diff `out/<arc>/<name>.enc` against the source to
   eyeball exactly what changed — should be only FIXME→prose line swaps.
9. **Promote (the one risky step).** Copy `out/<arc>/*.enc` over the canonical
   source on the branch. Re-run `encounter check` + `encounter bundle` to confirm
   the game still loads it. Commit; open a PR for line-by-line human review **before**
   it lands (never `git push`/merge without an explicit ask —
   `feedback_no_unprompted_push`).
10. **Rollback is trivial:** the branch isolates everything; sidecars/`out/` are
    gitignored; the baseline `.enc` is untouched until the PR merges.

## Order (DECIDED)

**Wave 1** — the three substrate-complete arcs:
1. **`the_villa`** — first real promotion. Already proven through weave; smallest with
   full threads. Use it to nail the manual-curation + promotion procedure end-to-end.
   Tier-2 isolation for this first run.
2. **`relay_post`** (6 enc) — author threads, run the now-proven loop. Decide the
   per-arc thread-file format here.
3. **`signal_array`** (10 enc) — largest; same loop.

**Later** — `foundry`: excluded from wave 1. Author its substrate (bibles + per-scene
lenses) as a separate task, then run it through this same procedure. Tracked as its
own effort, referenced from here.

## Cost & resource notes

- **imp is serialized.** categorize (glmchat) and color (gemma triplet) both want the
  box, and color is overnight-per-arc and needs it to itself. Plan color as a
  kick-off-at-end-of-session job; don't block other work on it. Batch arcs so the box
  isn't thrashed between profiles.
- **Gateway spend** is per woven beat (paid even on `@cf` ids). `the_villa` vastand was
  19 calls; a 10-enc arc with several threads is materially more. Smoke `--limit 1`,
  review, then scale (`feedback` discipline from the port plan).
- **Key hygiene:** Cloudflare key lives only in gitignored `appsettings.json`; never
  commit it, never read from a path outside the repo.

## Open questions

- Per-arc thread storage (`_threads.*` in the arc dir) vs. compiled `Threads.cs`? Lean
  per-arc file before relay_post.
- `forge lock` curation UX — minimal CLI accept/edit vs. richer surface? Minimal first.
- Single integrator model, or A/B several per arc (the deferred `compare`/Phase 6) and
  pick? Lean single (kimi) for the first wave; revisit if quality wobbles.
- Does foundry belong in this plan at all, or a separate "finish foundry substrate"
  plan that feeds back here when ready? Lean: separate, referenced from here.
