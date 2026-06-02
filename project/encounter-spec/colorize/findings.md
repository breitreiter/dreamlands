# Colorize — Findings for the Dreamlands Tool

Written 2026-06-01, end of the colorize prototyping spike. Audience: whoever folds
color generation into the larger dreamlands pipeline tool. This is the "what we
learned + what to carry / what to leave" doc. Full session log lives in `STATUS.md`;
the spec in `NOTES.md`; the taste anchor in `exemplars.md`.

---

## 1. What colorize is (and isn't)

Generates **"color"** — scene texture / ambient observable detail — for `.enc`
encounters: 8–20 short, raw, *atomic observables* per scene, a grab bag for a human
curator. It owns **substance** (what's worth noticing), **not voice or placement** —
the pipeline is colorize → curator selects → writer integrates → house-style applies
voice. Color lines are raw material, never player-facing.

This was a **quality + steering** spike, deliberately *not* pipeline integration. The
harness is a bench, not a product. Mine it for the method and the prompts; don't lift
the plumbing wholesale (see §5).

---

## 2. The method that works — carry this over

A **two-stage pipeline**: steer generation, then gate output. Don't try to make one
clever prompt do both.

```
  per-scene LENS  ─┐
  scene beats     ─┼─►  GLM-4.5-Air (v3 prompt, thinking OFF)  ──►  over-generated pool
  (world facts)   ─┘        temp sweep, ~60–75 candidates/scene        │
                                                                       ▼
                              Haiku critic (one cached call/scene)  ──►  curated grab bag
                              cut simile/argue/invention/anachronism;     (+ cut list w/ reasons)
                              LIGHT dedup; keep variations for curator
```

- **Generation:** GLM-4.5-Air (106B/12B-active MoE), `v3` system prompt, thinking
  **off**, a small temperature sweep to over-generate. Plus a **per-scene lens** (§3.4).
- **Filter:** one `claude-haiku-4-5` call per scene with the candidate pool + the
  scene beats; cuts rule-breakers, dedups *lightly*. Decoupled — it reads a saved
  pool off disk, so you can tune the filter prompt without re-running the LLM.

Measured end-to-end on 6 held-out scenes: **5 of 6 came out with zero similes and
zero anachronisms**, grab-bag intact (the 6th had 2 stray "industrial" lines from a
dirty beat — left for the curator).

---

## 3. Surprising findings (read this section)

**3.1 — Model size beat prompt contortion.** The original 3B-active model
(Qwen3-30B-A3B) produced characteristic small-MoE slop — em-dash spam, "X but not Y",
repetition collapse to the token cap, per-NPC "dossier" reflexes. We spent effort
trying to prompt around it. Swapping to a **12B-active MoE (GLM-4.5-Air) erased the
structural failures outright** (no repetition collapse, plain register, recovered
dread ratio) *before any prompt change*. Lesson: validate the model before grinding
the prompt. Inference here is memory-bandwidth-bound (Strix Halo, ~256 GB/s), so
MoEs win and *active* param count is the quality knob, not total.

**3.2 — Prose-level prohibition has a low ceiling.** Rewriting the system prompt
(v2→v3) to forbid the slop barely moved it: similes dropped only ~18% (6.9% → 5.3%
of output lines). A system-prompt rule is weak against the model's baked-in
"vivid writing" prior at one-shot generation. Past a point, more "don't" text is
wasted tokens.

**3.3 — Reasoning mode made adherence WORSE (counterintuitive).** GLM is a hybrid
reasoning model. We expected "think before you answer" to help it follow the rules.
It did the opposite: normalized simile rate *rose* (5.3% → 6.4%), anachronisms
*quadrupled*, output ran ~1.8× more verbose, and it was much slower. Inspecting the
chain-of-thought: **the model reasons about the *scene*, not about the *discipline*** —
it deliberates over what's in the room, then writes the same slop. Don't assume
thinking-on helps stylistic constraint; measure it.

**3.4 — The per-scene LENS was the highest-leverage lever — and it works by
*translation*, not prohibition.** A short per-scene file stating *whose eye, what to
notice, in what key* (a "merchant pricing the goods"; "a renaissance traveler with no
word for this machinery"; "villagers who have no use for strangers") cut similes 42%
(5.3% → 3.1%) — more than every prompt rewrite combined. Crucially, where a filter
*deletes* an out-of-frame word, the lens makes the model **render it in-frame on its
own**: told to see as a renaissance eye, GLM turned the beats' "heavy industrial
shelving" into *"heavy iron racks standing in orderly rows"* and "ventilation current"
into *"a dry draught that never touches the sky."* **Steer perception upstream; it
beats scrubbing vocabulary downstream.** This is the headline finding — it generalizes
the "PC's frame is a lens" principle from a global default into per-scene steering.

**3.5 — Similes were the dominant slop and weren't on the prohibited list.** The v2
"show, don't argue" rule named the tells "despite / not X but Y / as if" — and missed
plain `like` similes, which turned out to be the single most common failure. Audit
your prohibited list against actual output; the obvious tells aren't always the
frequent ones.

**3.6 — Same-scene contamination gives false confidence.** Early on, the prompt
*examples*, the *test input*, and the *eval gold* were all the same scene. The model
looked great because it was echoing the examples, not generalizing. We only saw the
real behavior after building **held-out test scenes with their own gold**. For the
dreamlands tool: never evaluate color on a scene whose color is also in the prompt.

**3.7 — The model faithfully echoes the *beats'* vocabulary.** Most "anachronism"
leaks in one scene traced to the input beats literally containing "industrial
shelving"/"ventilation current" (an artifact of how we stripped that scene). The
model rendered what it was given. **Fix register at the source — clean beats and a
good lens — not with an output filter.** Garbage vocabulary in the beats propagates.

**3.8 — Gold/exemplars must be *substance*, not *voice*.** When we extracted color
from well-written existing scenes, copying the authors' finished prose as the target
would have trained the model toward over-writing — the exact failure we were fighting.
We de-voiced every gold line to an atomic observable ("steam curling off the pie",
not "steam curling like ghosts"). Whatever you feed as exemplars, strip the voice
first; colorize owns substance, the house-style stage owns voice.

**3.9 — Over-generate + cheap critic > one perfect call.** Splitting "be creative"
(generation) from "obey the rules" (filter) let each do its job. The local model is
free and slow, so over-generating costs only wall-clock; a Haiku pass to cull is
pennies. Keeping the filter decoupled (operates on a saved pool) meant we could tune
the filter prompt in seconds without regenerating.

**3.10 — GLM serving/sampling gotchas (operational, will bite you).**
- Serve with `--jinja`; **repeat-penalty off** (=1.0); sampling **top-p 1.0 + min-p
  0.01**, not top-p 0.95.
- It's a **reasoning model**: CoT lands in `reasoning_content`, *not* `content`. With a
  small `max_tokens`, reasoning eats the whole budget and `content` comes back **empty**.
  `max_tokens` caps reasoning + content *combined*.
- Toggle reasoning per-request via `chat_template_kwargs: { enable_thinking: false }`.
- Prompt eval is slow (~26 tok/s prompt, ~18 gen). A 3–4K-token prompt is ~2 min and
  **blew the OpenAI .NET SDK's 100s default timeout.** No KV cache shared across
  server slots, so concurrent cold prompts each re-eval the whole prefix → **serial is
  fastest on this box**, and the bench's concurrency machinery is dead weight (§5).
- The OpenAI .NET SDK could not send `chat_template_kwargs`/`min_p` nor surface
  `reasoning_content`. We dropped it for a **raw JSON POST**. If the dreamlands tool
  needs these GLM features, plan for raw HTTP, not the typed SDK.

---

## 4. Concrete things to reuse

- **`prompts/v3.md`** — the generation system prompt. Built around one positive frame
  ("transcribe the recording, don't describe it") rather than a "don't"-list. Start here.
- **The `FilterRules` constant** in `src/Program.cs` — the Haiku filter's rubric
  (simile / argue / invention / anachronism / generic / ornate / light-dedup).
- **The lens files** `tests/*.lens.md` — templates for the per-scene lens (two facets:
  Perceptual + Social/tonal). Homes: per-scene `<slug>.lens.md` sidecar, or arc-wide
  `_lens.md` peer beside the encounter. Inject **first and loudest**, above the facts.
- **The eval set shape** — `tests/<slug>.enc` (flat beat skeleton) + `<slug>.gold.md`
  (de-voiced observables) + `<slug>.lens.md` (lens), self-contained, register-diverse.
  This is how to score any future prompt/model change without contamination.
- **Config shape** — follows nb's `ChatProviders` (LocalLlm + Anthropic; real keys in
  gitignored `appsettings.json`, placeholders in `appsettings.example.json`).
- **Prompt caching** — the filter's static rubric goes in a cached system block.

---

## 5. Harness cruft — do NOT blindly copy

The bench accreted across revisions. These are dead or vestigial under the settled
config; don't carry them into the product:

- **`OpenAI` 2.10.0 package reference (`src/Colorize.csproj`).** No longer used — we
  POST raw JSON. Pure dead dependency. (The csproj comment claiming it's "the SDK nb
  uses to reach llama.cpp" is now false.)
- **The warm-cache pre-call and concurrency machinery (`RunSweep`).** The
  `if (jobs.Count > 1 && MaxConcurrency > 1)` warm-up and the `SemaphoreSlim` gate are
  leftovers from an abandoned concurrency-4 approach. We settled on **serial**
  (`MaxConcurrency: 1`), so the warm-up branch **never executes** and the gate is a
  no-op. Vestigial.
- **`PresencePenalty` / `FrequencyPenalty` settings.** Always 0 (GLM wants repeat
  penalties off). Carried over from the OpenAI/qwen era; they are sent every call but
  never non-zero. Don't mistake them for tuned knobs.
- **Single-scene mode (`Encounter` + `Context` + the signal-array bibles).** Dormant —
  `Tests` drives the real workflow now. The `Encounter`/`Context`/`MaxChars`-truncation
  path still works (it was the original calibration mode against `sample/Start.enc`) but
  isn't the main path. Decide deliberately whether the product needs it.
- **`prompts/v1.md`, `v2.md`.** Superseded. v1 had a parroting bug (echoed the
  exemplars verbatim); v2 was the simile-blind ruleset. Kept for provenance only —
  start from v3.
- **`sample/_scenes.md` and `sample/The Rest Interval.enc`.** Present in the repo but
  **not referenced** by any code or config. `_scenes.md` is bible material colorize was
  never fed; `The Rest Interval.enc` was never wired in.
- **`StripColor`'s `# []` / `# --- COLOR ---` handling.** This strips *failed-run*
  color blocks out of `sample/Start.enc` so the model won't imitate them. It is
  specific to that one legacy file's format; the held-out test skeletons carry no such
  block. The real `.enc` contract in the production pipeline differs — re-derive this
  input-stripping against the real format; don't copy the marker logic verbatim.
- **Stale top-of-file comment.** The `Program.cs` header still describes "a steering
  bench… dump raw outputs… no pipeline" — written before the lens and filter were
  added. Don't take it as the current architecture.

---

## 6. Not solved / deferred

- **LoRA** for style/voice — viable on this hardware, but data-gated (only ~5 gold
  exemplars). Phase 2, fed by the curation loop (curator-accepted lines → training
  pairs; aim ~100–300). Never train on model slop or failed-run color.
- **Bible-driven "deep-cut" generator** — a separate generation *mode* that draws from
  the world bibles (not encounter beats) and follows a chain of inference to
  logically-consistent details ("Bob the mine engineer would special-order the precise
  Kesharat instruments"). Provenance run bottom-up. See STATUS "Future options".
- **Lens-as-bible** — migrate the informal lens content already living in
  `sample/_set.md` ("What the PC reads") into a real `sample/_lens.md`.
- **Output format** — whether to keep the `[register · anchor · sense]` tags, and how
  accepted-vs-proposed color is marked, is unresolved (`NOTES.md` §11).
- **Curator-in-the-loop** — the whole method assumes a human curator picks keepers;
  we deliberately kept the grab bag wide (light dedup, variations preserved) for them.
  Two stray lines in 51 is a curator's job, not a bug.

---

## 7. Headline, if you read nothing else

Steer perception with a **per-scene lens** (it translates out-of-frame detail instead
of deleting it), **over-generate** with a big-enough-active MoE, and **gate with a
cheap critic** — don't try to win it all in one prompt, and don't trust reasoning mode
to enforce style. Validate on **held-out scenes with de-voiced gold**, or you'll
measure echo instead of skill.
