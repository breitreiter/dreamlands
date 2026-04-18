# Market Screen — selected changes to roll in

Context: we prototyped a "fresh take" on the market/shop screen and the designer cherry-picked four changes worth keeping. This doc describes each one with enough detail that you can implement it without the prototype in front of you. Keep the rest of the existing screen as-is.

---

## 1. Locale-flavored market name (replaces "Market")

Every settlement should display a **unique, flavorful market name** instead of the generic "Market". Examples of the tone we're going for:

- Aldgate Grand Bazaar
- Westbrook Corner Market
- Fenhallow Tithe Barn
- Longford Weigh-House
- Three Rivers Dock Market

**What to build:**

1. A small name-generation module. Inputs: a settlement's region/culture tag + its seed. Output: a stable name. Same settlement always generates the same name.
2. A **seeded grammar per region**. Each region has its own word-banks for prefixes (often the settlement name itself or a local landmark) and stall-types (Caravan Stall, Corner Market, Tithe Barn, Weigh-House, Dock Market, Customs Hall, Toll-House, Cloth Yard, Fish Steps, etc.). The stall-type bank should feel grounded in that region's economy (fishing villages get Dock Market / Fish Steps; agricultural towns get Tithe Barn / Weigh-House; trade hubs get Customs Hall / Caravan Stall).
3. 8–12 regional grammars is enough to cover ~200 settlements without obvious repetition. Start with 4–5 and expand.
4. Store only `{regionId, seed}` on the settlement — generate the display string on demand. This lets us tweak grammars later without a data migration.

**Same treatment for the proprietor line.** Currently we probably have a generic "Shopkeeper" label; replace with `{given_name} the {epithet}` where the epithet reflects their trade or personality (e.g. "Ilesa the Factor", "Bren the Weigher", "Old Harlen", "Mira the Long-Tongue"). Use the same region-keyed tables so proprietors feel of-a-place.

**Also add a small date/time byline below the name** — something like "Evening of the third market-day" or "Morning, high summer". Pulls from the existing in-game clock. One line, dim color, serves as worldbuilding texture.

---

## 2. Decorative market-name header

The market name currently renders as a flat text label. Upgrade it to a **sign-like header** that evokes a wooden shop sign. Visual breakdown:

**Structure** (three rows in a flex column, `gap: .35rem`):
1. The sign row itself (see below)
2. A byline row with proprietor + day-note
3. (Tabs come after, unchanged)

**Sign row** (`display: inline-flex; align-items: center; gap: .7rem`):

- **Lantern icon** on the far left. Use a gold-tinted icon from our existing iconography. Sized roughly 1.5× the body text height.
- **Market name** as an `<h2>`: serif/header font, ~32px, our accent gold color, `letter-spacing: .01em`, `line-height: 1`, zero margin.
- **Horizontal rule** that fills remaining space: `flex: 1; height: 1px;` with a **fading gradient** — `linear-gradient(to right, rgba(gold, .4), transparent)`. This is the key visual — the rule fades out into darkness on the right, giving a hand-painted feel.
- **Hanging-hook ornament** on the far right: an 18px circle, 1px gold border, 55% opacity, with a 1px vertical line dropping 12px below it via `::after`. Reads as a metal ring the sign hangs from.

The sign row has `border-bottom: 1px dashed rgba(gold, .35)` and a little vertical padding, so the whole row looks like a pinned placard.

**Byline row** below the sign:
- `display: flex; gap: .9rem; align-items: center`
- Dim text color, ~.95rem
- Format: `Proprietor: <span class="accent">{Name}</span>  ·  {day-note}`
- The "·" separator is a 3px dim circle div, not a literal character — gives a more deliberate look.

**Why this works:** the fading rule + hanging hook do most of the visual work. It's basically free (pure CSS, no art) but reads as a crafted sign rather than a title label.

---

## 3. Shop-subtitle in the proprietor's voice

Currently most shop screens have terse labels like "Sell" or "Trade". Keep the heading, but add a **one-line subtitle in the NPC's voice** underneath. For the market's sell column we used:

> "The factor will take things off your hands — click to stage."

The pattern: `{NPC-role-phrased-as-action} — {UI hint}`. The em-dash transitions from flavor to instruction, so it's both worldbuilding and functional UX copy.

Roll this out across shop screens with NPC-appropriate phrasing:
- Blacksmith sell: "The smith will melt down what you can spare — click to stage."
- Apothecary: "The herbalist sorts the useful from the spoiled — click to stage."
- Fence (if such a thing exists): "They'll ask no questions — click to stage."

Keep it to one line. One sentence per screen. The consistency is the point.

---

## 4. Wax seals on contracts

Contracts (delivery jobs, sealed letters, etc.) should display a **wax seal** in the full contract-card view to make them feel like physical documents. Important constraint: **the compact version in the player's pack should NOT show the seal** — just a colored dot hint (same color as the seal wax) to preserve visual continuity without cluttering the pack list.

**Data model changes:**

Add to the contract type:
- `sealVariant: string` — which crest/design. Start with: `"guild"`, `"crown"`, `"merchant"` (default), plus room for more later.
- `sealColor: string` — the wax color. Start with: red (default merchant), blue (guilds), gold/ochre (crown), green (rangers/woodsmen), black (illicit/urgent).
- `sealInitial: string` — 1–2 letters stamped into the wax (often the issuer's initial or a short sigil).

Derive these from the contract issuer where possible rather than storing per-contract.

**Implementation in two passes:**

### Pass A: ship with CSS-drawn placeholders (no art required)

Render the seal as a 44px circle with:
- A radial gradient going from a lighter highlight at 35% 30% to the wax color to a dark shadow edge — gives a wax-blob look.
- `inset 0 0 0 2px rgba(0,0,0,.25)` box-shadow to suggest an impressed rim.
- The `sealInitial` letter(s) centered inside, in our header font, ~18px, light cream color (#f0d9b8), slightly letter-spaced.
- Below the seal, a tiny "SEALED" tag in dim uppercase tracking.

Example CSS for the red merchant default:
```css
background: radial-gradient(circle at 35% 30%, #c0634a 0%, #8a3a2a 60%, #5a2419 100%);
```
Swap the three stops for blue/gold/green/black variants.

Ship this. It already looks decent.

### Pass B: swap in real seal artwork later

Once art is ready:
- Seals are small (~64–96px display, so 2x assets at 128–192px).
- Need 6–10 crest designs to cover the main issuer types. Color is a separate axis — the same crest in different wax colors reads as different issuers, so the art count stays manageable.
- The data model above doesn't change — `sealVariant` maps to crest image, `sealColor` tints the wax.
- Keep the CSS wax as fallback for any `sealVariant` without art yet.

### Compact/pack presentation

When a contract is shown in the **pack/inventory** (compact row view), the seal is suppressed. Instead, render a **small colored dot** (6–8px circle) in `sealColor` at the start of the row — same information, 1/10th the visual weight. Could also be a thin colored left-border on the row. Either works; pick the one that fits the existing pack-list styling.

The full seal should only appear when the contract is presented as a "letter" — market screen, contract detail modal, etc.

---

## Suggested implementation order

1. **Naming** (#1) — biggest worldbuilding payoff, pure backend/data work, unblocks the header display.
2. **Header decoration** (#2) — pure CSS, ~30 lines, immediately upgrades the feel once #1 is landing real names.
3. **Shop subtitle copy** (#3) — trivial; roll across all shop screens in one pass.
4. **Wax seals Pass A** (#4) — CSS-only, ship it. Queue Pass B with the art team separately.

Each of these is independent — ship in any order.
