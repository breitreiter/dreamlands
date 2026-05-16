---
kind: plan
title: Inventory Consolidation — remove haversack, reusable supplies, token removal
state: exploring
created: 2026-05-15
updated: 2026-05-15
supersedes: haversack_refactor.md
touches:
  files:
    - lib/Rules/CharacterBalance.cs
    - lib/Rules/ItemDef.cs
    - lib/Rules/ItemInstance.cs
    - lib/Game/PlayerState.cs
    - lib/Game/EndOfDay.cs
    - lib/Game/SkillChecks.cs
    - lib/Game/Market.cs
    - lib/Game/Bank.cs
    - lib/Game/Mechanics.cs
    - lib/Orchestration/SettlementRunner.cs
    - lib/Orchestration/Rations.cs
    - server/GameServer/GameResponse.cs
    - server/GameServer/GameFunctions.cs
    - ui/web/src/api/types.ts
    - ui/web/src/screens/Inventory.tsx
    - ui/web/src/screens/Market.tsx
    - ui/web/src/screens/Bank.tsx
    - tests/Dreamlands.Game.Tests/EndOfDayTests.cs
    - tests/Dreamlands.Game.Tests/MechanicsTests.cs
    - tests/Orchestration.Tests/SettlementRunnerTests.cs
  features: [inventory, haversack, food, conditions, tokens, ui]
related:
  - inventory_slot_refactor.md
---

# Inventory Consolidation

## Design Intent

With the haversack gone and equipment-as-flag landed (see `inventory_slot_refactor.md`), there
is one inventory: a pack. Every item in the game — gear, food, medical supplies, hauls — competes
for the same slots. This makes loadout a real decision with no safe "free" space.

The tab model that made sense across three containers (body, pack, haversack) becomes a flat
sorted list. The UI groups items by function so the player can scan their loadout at a glance
without navigating tabs.

### Why remove tokens

Tokens grant +1 to a skill or resist roll — a marginal bonus that isn't worth the mental
overhead of tracking 8 distinct named items. They live in the haversack today specifically
because pack slots are too precious; once the haversack is gone, they'd need pack slots, which
they don't deserve. The better home for interesting gear effects is the tool slot (e.g. a
spyglass that adds to Bushcraft, a waterskin that delays food drain). Remove tokens as a
category and let encounter rewards surface better-feeling alternatives.

### Why reusable medical supplies

Single-use bandages create a stocking loop — you exit a settlement, get injured, use a bandage,
then worry about whether you have enough for the rest of the trip. The resource being managed is
the bandage count, which is tedious. With a reusable medical kit, the resource being managed is
simpler: do you have one or not? The kit occupies a permanent pack slot; going unequipped with
medical supplies is a real risk, not a bookkeeping failure.

---

## Changes

### 1. Remove haversack

**`lib/Game/PlayerState.cs`**
- Remove `List<ItemInstance> Haversack` property.
- Remove `int HaversackCapacity` property.
- No replacement container — all items go to `Pack`.

**`lib/Rules/CharacterBalance.cs`**
- Remove `StartingHaversackSlots` constant.
- Increase `StartingPackSlots`: 5 → 8. (Rationale below.)

**`lib/Rules/ItemDef.cs` — `IsPackItem`**
- Old: `Type is Weapon or Armor or Boots or Tool or Haul` (Consumable/Token → haversack)
- New: everything is a pack item. Remove or simplify the property.

**Downstream plumbing that reads `Haversack` or `HaversackCapacity`:**
- `lib/Game/EndOfDay.cs` — food/medicine consumption reads from Haversack → read from Pack
- `lib/Game/Market.cs` — capacity checks for haversack → remove; pack checks already exist
- `lib/Game/Bank.cs` — deposit/withdraw from haversack tabs → remove haversack branch
- `lib/Orchestration/Rations.cs` — refill writes to Haversack → write to Pack
- `lib/Orchestration/SettlementRunner.cs` — passes haversack context to Rations → update

**`server/GameServer/GameResponse.cs`**
- `InventoryInfo` currently has `Pack` and `Haversack` lists → remove `Haversack`, keep `Pack`.

**`ui/web/src/api/types.ts`**
- Remove `haversack` from `InventoryInfo`.

**`ui/web/src/screens/Bank.tsx`**
- Remove haversack deposit/withdraw tab and any related state.

**`ui/web/src/screens/Market.tsx`**
- Remove haversack capacity tracking from projection logic.

---

### 2. Pack sizing

With gear (weapon/armor/boots = 3 slots), medical kit (1), food for a ~4-day leg (4), and a haul
(1), a fully-equipped player needs 9 slots. Base 8 with `upgrade_pack` available gives:

| Loadout | Slots used | Free at 8 | Notes |
|---|---|---|---|
| Fully armed (3 gear + 1 medical + 2 food + 1 haul) | 8 | 0 | tight, tool requires upgrade |
| Unarmed (1 medical + 4 food + 2 hauls + 1 tool) | 8 | 0 | much more flexible |
| Unarmed, no medical (4 food + 3 hauls + 1 tool) | 8 | 0 | maximum courier load, no safety net |

`upgrade_pack` still exists and can push capacity higher for pack-focused builds.

**`lib/Rules/CharacterBalance.cs`**: `StartingPackSlots = 5` → `8`.

---

### 3. Remove tokens

**`lib/Rules/ItemDef.cs`**
- Delete the 8 token item defs: `ivory_comb`, `lucky_buckle`, `knotwork_seed`, `tarnished_key`,
  `hunters_journal`, `grid_cipher`, `color_lens`, `revathi_tile`.
- Remove `ItemType.Token` from the enum.

**`lib/Rules/ItemDef.cs` — `IsPackItem`**
- Remove Token from the check (or delete the property, see §1 above).

**`lib/Game/SkillChecks.cs`**
- Delete `GetTokenBonus()` (lines 217-227) and `GetTokenResist()` (lines 229-240).
- Callers (skill check and resist roll paths) stop applying the token bonus.

**Encounter .enc files**
- Grep for any `+item ivory_comb`, `+item lucky_buckle`, etc. encounter rewards.
- Replace with something else (gold, a tool, a ration) or remove the reward line.
- This is a content audit pass, not a mechanical change.

---

### 4. Reusable medical supplies

Replace single-use bandages (and any other single-use medicines) with a persistent medical kit
that cures conditions without being consumed.

**Design**
- One item: `medical_kit` (or biome-flavored variants with identical mechanics).
- Type: `ItemType.Tool` — it's equipment, not a consumable. Occupies a pack slot permanently.
- End-of-day: if a serious condition is active and a `medical_kit` is in the pack, clear the
  condition. Do not remove the item.
- No explicit "use" action needed — auto-cure is the simplest model and matches how bandages
  already work.

**Open question**: Should serious-condition auto-cure be unconditional (any medical kit cures
any serious condition) or selective (specific kit per condition — anti-rad for Irradiated, etc.)?
- **Unconditional** is simpler and focuses the resource question on "have it or not."
- **Selective** adds planning depth but also inventory bloat (carry three different kits).
- Lean unconditional unless a later content pass makes selective cures feel interesting.

**`lib/Rules/ItemDef.cs`**
- Add `medical_kit` as `ItemType.Tool` with cure info.
- Remove `bandage` item def (and any other single-use medicine defs if unconditional model chosen).
- If selective model: keep multiple defs (medical_kit, antitoxin, anti_rad) but all as Tool type.

**`lib/Game/EndOfDay.cs`**
- Medicine auto-consume path: instead of removing the item from the haversack, just leave it.
- Change: `player.Haversack.Remove(medicine)` → omit the remove call (or explicitly: do nothing
  after clearing the condition).

**`lib/Game/Market.cs`**
- Medical kit(s) available in settlement catalogs. Remove the old bandage stock entries.
- Pricing: a reusable kit should cost more than a single bandage (bandage was 3g; medical kit
  maybe 15-25g — one-time purchase for the trip).

---

### 5. Inventory UI — flat sorted list, no tabs

With no haversack and the equipped tab removed (see `inventory_slot_refactor.md`), the inventory
is a single list. Sort items into named groups so the player can scan at a glance:

**Group order:**
1. **Equipped gear** — items with `isEquipped: true`, sorted Weapon → Armor → Boots
2. **Unequipped gear** — Weapon/Armor/Boots in pack but not equipped
3. **Supplies** — food rations + medical kit(s) (Tool type items that are non-gear)
4. **Tools** — remaining Tool type items (spyglass, canteen, etc.)
5. **Hauls** — active delivery contracts

**`ui/web/src/screens/Inventory.tsx`**
- Remove all tab navigation (Pack / Haversack / Equipped tabs gone).
- Replace with a single scrollable list with sticky group headers.
- Equipped items show the equipped badge; all items show equip/unequip/discard actions
  as appropriate.
- Capacity indicator at top: `X / N slots`.

**Group classification**
This can be done client-side from `itemType` and `isEquipped`, no server changes needed beyond
what `inventory_slot_refactor.md` already adds.

---

### 6. Condense tiered gear

The catalog has several groups of items that are mechanically identical except for a numerical
bonus (+2 vs +3, or +1/+2/+3/+4/+5). With immunity replacing resist gradients (§7), those
tiers lose their purpose. For skill-modifier tools, the difference between +2 and +3 is too
small to justify two separate items.

Direction: collapse each tiered group to one item. A second variant is only justified when it
has a distinct identity — different immunity, different move set, different thematic role — not
just a higher number.

**Boots (5 → 1–2)**
- fine_boots / heavy_work_boots / riding_boots / trail_boots / scarecrow_boots all give
  escalating exhaustion resist. With immunity, you either have boots or you don't.
- Keep one generic boots item with exhaustion immunity. A second named/special variant is fine
  if it offers something qualitatively different (a bushcraft bonus, a different immunity), not
  just a higher resist value.

**Canteen / Waterskin (2 → 1)**
- Both give thirst resist at +2/+3. With thirst immunity, one item suffices. Keep `waterskin`
  (more evocative).

**Letters of Introduction / A Guide to the Borderlands (2 → 1)**
- Both give Negotiation bonus at +2/+3. Collapse to one item. Pick a value and a name.

**Cartographer's Diary / Ornate Spyglass (2 → 1)**
- Both give Bushcraft bonus at +2/+3. Keep `ornate_spyglass` (more iconic). Pick a value.

**Light armor Cunning tiers**
- tunic (+0), silks (+1), cartographer's cloak (+3), robe_of_twilight (+5) escalate by Cunning
  bonus. This is the same pattern: multiple versions of "light armor but better."
- Direction: remove the Cunning numerical gradient. Each light armor variant should be
  distinguished by its RPS move set or a qualitative property, not a +1/+3/+5 escalation.
  If Cunning is staying as a skill modifier at all post-refactor, it should live on at most one
  special item, not as a tier ladder.

**Heavy armor injury resist tiers**
- gambeson (+2), scale_armor (+2), brigandine (+4), golem_armor (+5) escalate by injury resist.
- With injury resist not becoming immunity (§7), this tier is vestigial. See §7 for what happens
  to injury resist on armor.

---

### 7. Environmental immunity gear (replacing resist)

The current resist system (+N vs a DC) creates a numerical minigame: accumulate enough resist
and the condition rarely sticks. Replace with binary immunity for environmental/nuisance
conditions. "Do I have what I need?" is a cleaner question than "do I have enough resist?"

**Immunity rule**: if an item in your pack grants immunity to condition X, that condition cannot
be applied. No roll, no threshold. The item just needs to be present.

**Environmental conditions — immunity is appropriate:**

| Condition | Source | Immunity item | Notes |
|---|---|---|---|
| freezing | mountain biome | coat / heavy cloak (Tool) | Moves off armor — see below |
| thirsty | scrub biome | waterskin | Collapses canteen/waterskin (§6) |
| exhausted | travel | boots | Collapses 5 boot tiers (§6) |
| lost | navigation | cartographer's kit | Already +5 resist — effectively immunity |

**Note on freezing and armor**: several armors currently carry freezing resist. With immunity,
giving multiple armor types the same immunity creates redundancy. The cleaner model: freezing
immunity lives on a dedicated Tool item (a coat, heavy cloak, or similar) so armor is chosen for
its combat role and cold-weather prep is a separate inventory decision. Armor entries for
freezing resist are removed; one new Tool item carries it.

**Severe/combat conditions — not immunity:**

| Condition | Source | Verdict |
|---|---|---|
| injured | combat (direct) | **No immunity item.** Cured by medical kit. Armor may retain a small flat resist (e.g., +1) as flavor but no tiered gear, no immunity. |
| poisoned | combat / hazard zone | **Borderline.** If poisoned is inflicted in combat, immunity breaks the combat loop. If it's purely a hazard-zone condition (venomous swamp biome, specific dungeon), immunity via antivenom_kit is defensible. Flag for content review. |
| irradiated | hazard zone | **Likely OK.** Sakharov mask → irradiated immunity in radiation areas. Not combat-inflicted. |
| lattice_sickness | Lattice structures | **Likely OK.** Lattice ward → lattice_sickness immunity near Lattice areas. Not combat-inflicted. |

The borderline cases (poisoned, irradiated, lattice_sickness) should be reviewed once the biome
content is clear enough to know whether those conditions can be inflicted during combat. If yes,
keep them as resist items with a high but non-infinite value rather than converting to immunity.

**`lib/Rules/ItemDef.cs`**
- Add `List<string> Immunities` property to `ItemDef` (condition IDs, e.g. `["freezing"]`).
- Remove `ResistModifiers` entries from items that convert to immunity.
- Add new coat/cloak Tool item with `Immunities: ["freezing"]`.
- Remove injured resist tiers from armor (or flatten to a single small value on heavy armor only).

**`lib/Game/EndOfDay.cs`**
- Before applying any condition, check if any pack item has it in `Immunities`. If yes, skip.

**`lib/Game/SkillChecks.cs`**
- Resist roll path: check immunities first (auto-pass the resist if immune).
- `GetTokenResist` already being deleted (§3). The immunity check replaces the resist-modifier
  lookup for environmental conditions.

**`server/GameServer/GameResponse.cs`**
- `ItemInfo`: optionally surface `immunities` list so the frontend can display it on item cards.

---

## Superseding `haversack_refactor.md`

The old plan (`haversack_refactor.md`, state: active) diverges from this direction in three ways:
- It halves haversack to 10 slots rather than removing it.
- It retains single-use bandages per serious condition.
- It does not address tokens.

The old plan's content that is still valid and incorporated here:
- Single `food_ration` item (already implemented; endorsed here).
- Rations refill at settlement (endorsed; just targets Pack instead of Haversack).
- Foraging mechanic skips day's ration consumption.
- Conditions: binary, no stacks (the `HashSet<string>` direction is still right).
- HP model: serious condition → HP −1/day; no serious condition → HP +1/day.
- Minor conditions cure environmentally; serious conditions require kit.
- The biome-flavored ration names in `lib/Flavor/`.

Mark `haversack_refactor.md` as `state: superseded` when this plan enters active.

---

## Blast Radius

| Area | Scope | Risk |
|---|---|---|
| `PlayerState` haversack removal | 2 properties removed, all read sites updated | Medium — many callers |
| `CharacterBalance` slot count | 1 constant | Trivial |
| `ItemType.Token` removal + 8 item defs | Delete enum value, 2 methods in SkillChecks | Low — easy to grep |
| Token rewards in .enc files | Content audit | Low effort, must not miss any |
| `ItemType` cleanup (IsPackItem) | Simplify or delete the property | Low |
| Medical kit model | EndOfDay cure path loses 1 remove call; new item def | Low |
| Market medical stocking | Replace bandage catalog entries | Low |
| `EndOfDay` food consumption | Reads Pack instead of Haversack | Low |
| `Rations.Refill` | Targets Pack | Low |
| `SettlementRunner` | Passes pack context | Low |
| `GameResponse` haversack field | Remove from DTO | Low |
| `Bank.tsx`, `Market.tsx` | Remove haversack capacity paths | Low-medium |
| `Inventory.tsx` | Remove tabs, add group headers | Medium |
| Tiered gear collapse | ~15 item defs removed/merged from `ItemDef.cs` | Low — catalog only |
| Tiered gear in market catalogs | Remove stocking entries for deleted items | Low |
| Tiered gear in .enc rewards | Grep + replace with surviving item IDs | Low effort, must audit |
| `ItemDef` — add `Immunities` field | New property, immunity-granting items updated | Low |
| `ItemDef` — remove `ResistModifiers` | Remove from converted items; keep on injury-adjacent | Low-medium |
| `EndOfDay` immunity check | One guard before condition application | Trivial |
| `SkillChecks` resist path | Immunity short-circuit before resist roll | Trivial |
| New coat Tool item | 1 new item def | Trivial |
| `ItemInfo` DTO (optional) | Surface `immunities` list for UI display | Low |

Total new files: 0. Files deleted or simplified: `Rations.cs` gets simpler,
`SkillChecks.cs` loses ~25 lines, `EndOfDay.cs` loses the medicine-remove call. Gear catalog
shrinks by ~15 items. Net code and content reduction.

---

## Open Decisions

1. **Unconditional vs selective medical kit**: lean unconditional. Revisit only if content makes
   selective feel interesting.
2. **Medical kit price point**: 15-25g suggested. Lock down when balancing market economy.
3. **Supplies group in UI**: food and medical kit grouped together makes sense now; if more
   non-gear tools are added later this grouping may need refinement.
4. **upgrade_pack ceiling**: with base 8, what's the max? The old pack only went to 3 + a few
   upgrade_pack grants. Revisit alongside pack-focused encounter rewards.
5. **Poisoned immunity**: determine whether poisoned can be inflicted during combat before
   deciding if antivenom_kit becomes an immunity item. If poisoned is combat-inflicted anywhere
   in the encounter catalog, keep it as a (high) resist item rather than full immunity.
6. **Injury resist on heavy armor**: remove entirely (armor value is RPS moves) or keep a small
   flat value (+1) on heavy armor as flavor? Lean remove — the medical kit handles cure, and
   a flat resist without a tier gradient isn't useful enough to justify the stat.
