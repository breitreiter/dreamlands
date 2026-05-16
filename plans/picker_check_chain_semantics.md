---
kind: plan
title: Picker check semantics inside @if/@elif chains
state: ready
created: 2026-05-16
updated: 2026-05-16
related:
  - skill_tier_rework.md
  - arc_leveling.md
touches:
  files:
    - lib/Encounter/  # parser + bundle shape for conditional blocks
    - lib/Game/Conditions.cs
    - lib/Orchestration/EncounterRunner.cs
    - text/encounter-tool/EncounterCli/
    - project/encounter-spec/format.md
    - project/encounter-spec/mechanics_reference.md
    - text/encounters/arcs/forest/the_fugitive/Mareen.enc  # only known chained-check site
  features: [encounters, syntax, picker]
---

# Picker Check Chain Semantics

## Problem

Under the d20 model, `check` is a pure predicate: evaluate, get true/false, branch. It
slots into `@if`/`@elif`/`@else` chains alongside `tag`, `quality`, `has`, etc., and
silent fall-through on fail is fine because the player never "committed" to anything —
the engine just rolled dice during evaluation.

Under the [[skill_tier_rework]] picker model, evaluating a `check` requires player
input (the 3-approach picker), and the player has visibly invested in a chosen approach.
If they pick "Reason" and fail, falling silently into an `@elif tag bribed` branch that
narrates a totally different outcome reads as the engine ignoring their choice.

We need to define what happens when a picker `check` appears in a chain that has
siblings.

## Today's semantics (d20)

`@if`/`@elif`/`@else` walks branches in order. Each branch's condition is evaluated as
a pure function of state (+ rng for `check`). First branch whose condition is true wins;
otherwise `@else` fires. `check` failure is just "this branch's condition was false" —
identical in shape to a tag miss.

## Why pickers break it

A picker `check` is not pure-evaluation. It:

1. Renders a UI (the 3-approach picker for the skill).
2. Awaits player input.
3. Resolves outcome from `(tier, pick, correct, wrong)`.
4. Emits a connector line.
5. Now needs to fire either a success or a fail body.

Step 5 is where it conflicts with the chain. Today step 5 just hands true/false back to
the chain walker. Tomorrow, after the player has actively chosen "Threaten" and the
engine has emitted "You try to threaten, but find yourself flattering…", silently
moving on to evaluate an `@elif tag` is a narrative disconnect.

Also: picker checks have an **authored fail body** (the @else of the picker's success
branch). That fail body is a story beat. It needs to play. It can't be skipped because
some downstream `@elif tag` happens to match.

## Corpus audit (2026-05-16)

Only 2 `.enc` files in the entire corpus use `@elif`:

- `text/encounters/arcs/forest/the_fugitive/Dawn.enc` — chains `@elif tag …` branches.
  No `check` involvement. Easy.
- `text/encounters/arcs/forest/the_fugitive/Mareen.enc` — chains `@elif tag …` branches
  **with a single `@elif check negotiation hard`** buried mid-chain. This is the only
  site in the corpus where the question actually bites.

So whatever resolution we adopt has effectively **one** existing authoring rewrite —
plus the format-spec, parser, and runner work to support whatever shape we pick.

The Mareen.enc pattern, abstracted:

```
@if tag X { ... }
@elif tag Y { ... }
@elif tag Z { ... }
@elif check negotiation hard { ... }   <-- the picker would fire here
@elif tag W { ... }
@else { ... }
```

The intent is clearly "first try a series of static state matches; if none match, give
the player a skill check; if even the check fails, fall through to more state matches
and finally a default." The chain is using `check` as a *priority-ordered fallback*,
not as a peer predicate.

## Decision (2026-05-16): Option 1 — picker `check` must be terminal

A `check` condition is only legal as the **last** branch of an `@if`/`@elif` chain,
and that chain must end with `@else`. The `@else` body is the check's authored fail
beat. Anything else (a `check` mid-chain, a `check` chain with no `@else`, an
`@elif` after a `check`) is a parse error.

Static state conditions (`tag`, `quality`, `has`, `meets`) stack as `@if`/`@elif`
peers as today. The picker check is the terminating gate.

### Mental model for authors

> `tag`/`quality`/`has` are state matches and can stack. `check` asks the player to
> commit, so it goes last and pairs with `@else` as its fail body.

### Mareen.enc impact

The existing chain at `Mareen.enc:86–110` already conforms to this shape
(static-tag acks, then `@elif check negotiation hard`, then `@else`). Zero
authoring rewrites required.

### Connector behavior

When a picker `check` fires, the connector line ("you try to threaten, but find
yourself flattering…") emits on **both** the success and the fail path. The player
committed to an approach regardless of outcome, so the narration of that
commitment plays before the success body or the `@else` fail body. Pin this in the
format spec.

### Escape hatch (not implemented; documented for future)

If a future encounter genuinely needs check-then-fallthrough, authors can nest:
put the `check`+`@else` block inside the outer `@else` of a static chain. We
don't need this today and won't add parser affordances for it until someone hits
the wall.

### Rejected alternatives (for the record)

- **Inline picker block** (`@picker check ... { success } { fail }`): adds a new
  block construct; conflicts with the existing-syntax-preserving goal of
  [[skill_tier_rework]].
- **Nested-only** (Option 3 as the rule): zero new syntax but visually verbose at
  the one site that uses it; Option 1 reads better.
- **New `@try/@ok/@fail` construct**: cleaner in the abstract but new-keyword tax
  for one corpus site.

## Implementation checklist

1. Parser (`lib/Encounter/`): enforce the terminal-check rule. Reject
   non-terminal `check` and `check` chains missing `@else` with a clear error.
2. Bundle JSON shape: confirm the existing `conditional` shape carries enough to
   distinguish "picker check terminal branch" from a static-condition branch (the
   runner needs to know to render the picker UI and emit the connector). If not,
   tag the terminal branch in the bundle.
3. Runner (`lib/Orchestration/EncounterRunner.cs`): on a terminal `check` branch,
   render picker UI → resolve → emit connector → fire success body or `@else`
   body. No fall-through past `@else`.
4. Connector emit: fires on both success and fail.
5. Format spec (`project/encounter-spec/format.md`) and mechanics reference
   (`project/encounter-spec/mechanics_reference.md`): document the terminal-check
   rule and the connector behavior.
6. Update [[skill_tier_rework]] §.enc Syntax Extension to point at this plan for
   the chain-semantics contract.
7. Add a parser test covering the four illegal shapes (mid-chain check, check
   with no `@else`, `@elif` after check, lone `@if check` with no `@else`).

## Revisit trigger

Reopen if an author hits a real case that the nested escape hatch can't express
cleanly, or if the "connector fires on fail" call reads badly in playtest.
