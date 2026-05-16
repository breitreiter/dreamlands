---
kind: plan
title: Inventory slot refactor — equipped-as-flag, 5 pack slots
state: exploring
created: 2026-05-15
updated: 2026-05-15
related:
  - inventory_consolidation.md
touches:
  files:
    - lib/Rules/CharacterBalance.cs
    - lib/Rules/ItemInstance.cs
    - lib/Game/EquippedGear.cs
    - lib/Game/PlayerState.cs
    - lib/Game/Mechanics.cs
    - lib/Game/Bank.cs
    - lib/Game/Market.cs
    - lib/Game/Rescue.cs
    - lib/Game/CombatPlayerProfile.cs
    - server/GameServer/GameResponse.cs
    - server/GameServer/GameFunctions.cs
    - ui/web/src/api/types.ts
    - ui/web/src/screens/Inventory.tsx
    - ui/web/src/screens/Market.tsx
    - ui/web/src/screens/Bank.tsx
    - tests/Dreamlands.Game.Tests/MechanicsTests.cs
    - tests/Dreamlands.Game.Tests/BankTests.cs
  features: [inventory, equipment, combat, balance]
---

# Inventory Slot Refactor

## Design Intent

Carrying martial gear should have a measurable cost. Under the old model, a weapon/armor/boots
slot exists outside the pack and is either filled or empty — there is no cost to holding combat
gear beyond the opportunity of not having it. "I don't engage with the combat system" has no
mechanical expression.

**Goal**: make pack slots a scarce resource that combat participation consumes. A player who
carries a weapon, armor, and boots is giving up three slots permanently. A player who goes
unarmed and unarmored has a materially freer pack but faces genuine risk in hostile encounters.

## Model Change

**Old**: 3 pack slots + 3 dedicated equipment slots (`EquippedGear.Weapon/Armor/Boots`).
Equipping moves an item out of the pack into a slot that has no capacity cost.

**New**: 5 pack slots. `IsEquipped` is a bool flag on `ItemInstance`. All gear lives in the pack
whether equipped or not. Equipping/unequipping is a flag flip with a mutual-exclusion sweep
(equipping a weapon unsets `IsEquipped` on any other weapon in the pack).

---

## Layer 1 — Data Model

### `lib/Rules/CharacterBalance.cs`
- `StartingPackSlots = 3` → `5`

### `lib/Rules/ItemInstance.cs`
- Add `bool IsEquipped { get; set; }` property.
- **Serialization**: existing Cosmos documents will deserialize `IsEquipped` as `false` (the
  default). Migrated sessions start fully unequipped — acceptable. No migration script needed,
  but the first equip action on a resumed session will feel like a fresh start for gear.

### `lib/Game/EquippedGear.cs`
- **Delete.** Its three nullable item properties are fully replaced by the `IsEquipped` flag
  on pack items.

### `lib/Game/PlayerState.cs`
- Remove `EquippedGear Equipment` property.
- Add three computed (non-persisted) helper properties:
  `EquippedWeapon`, `EquippedArmor`, `EquippedBoots` — each scans `Pack` for the first item
  with matching `ItemType` and `IsEquipped == true`. These replace all reads of
  `state.Equipment.Weapon/Armor/Boots` across the engine without changing call-site logic.

---

## Layer 2 — Mechanics

### `lib/Game/Mechanics.cs`

**`ApplyEquip`**
- Old: remove from pack → stash old equipped item → write to `Equipment.X`.
- New: find item in pack by ID → unset `IsEquipped` on any other pack item with the same
  `ItemType` → set `IsEquipped = true` on target. Item never leaves the pack.
- `MechanicResult.ItemEquipped` still carries a slot name (derivable from `ItemType`).
  No callers change.

**`ApplyUnequip`**
- Old: clear `Equipment.X`, push to pack.
- New: find pack item matching the slot-type name where `IsEquipped == true` → set
  `IsEquipped = false`. No item movement.

### `lib/Game/Bank.cs`
- Deposit lookup currently switches on slot name to read `Equipment.X`. Change to scan `Pack`
  for an item matching the slot type where `IsEquipped == true`, clear the flag, then move to
  bank. Behavior is identical from outside.

### `lib/Game/Market.cs`
- **Auto-equip on buy**: after adding item to pack, if no pack item of the same `ItemType`
  has `IsEquipped == true`, set `IsEquipped = true` on the new item.
- **Auto-unequip on sell**: if the sold item has `IsEquipped == true`, clear flag before
  removing from pack.
- Pack capacity check on buy: unchanged — still needs a free slot.

### `lib/Game/Rescue.cs`
- `ShouldStrip` is cost-based and already strips from `Pack`. No change needed — equipped
  items are in the pack by definition.

### `lib/Game/CombatPlayerProfile.cs`
- Replace `state.Equipment.Weapon` / `state.Equipment.Armor` reads with the new
  `state.EquippedWeapon` / `state.EquippedArmor` computed helpers. Logic is identical.

---

## Layer 3 — Server / API

### `server/GameServer/GameResponse.cs`
- `EquipmentInfo` (weapon/armor/boots as nullable `ItemInfo`) stays as a derived presentation
  view, computed from pack items, so the frontend stays compatible without changes to
  the `EquipmentInfo` contract.
- Add `bool IsEquipped` to `ItemInfo` so the frontend can also render an equipped badge on
  pack items directly (used by the Option A inventory layout below).

### `server/GameServer/GameFunctions.cs`
- `BuildInventoryResponse`: derive `EquipmentInfo` by scanning pack items for `IsEquipped == true`
  per type, rather than reading `Equipment.Weapon/Armor/Boots`.
- Equip/unequip action routing: unchanged — `Mechanics.Apply` handles everything.
- `IsEquippable` check: unchanged.

---

## Layer 4 — Frontend

### `ui/web/src/api/types.ts`
- Add `isEquipped?: boolean` to `ItemInfo`.
- `EquipmentInfo` interface unchanged (server still derives and sends it).

### `ui/web/src/screens/Inventory.tsx`
- **Equipped tab**: can continue reading from `inventory.equipment` (still sent by server).
  No logic change required unless we move to Option A below.
- **Pack tab — design decision** (pick one):
  - **Option A** (recommended): show all pack items including equipped ones; render an equipped
    badge on equipped items; Equip button only on unequipped equippables; Unequip button on
    equipped items. This surfaces the slot cost — the whole point.
  - **Option B**: pack tab shows unequipped items only; equipped tab shows equipped items.
    Cleaner tabs but hides that the sword occupies a slot.

### `ui/web/src/screens/Market.tsx`
- `projectedEquipment` projection logic: instead of a `{weapon, armor, boots}` object tracking
  freed/filled external slots, track which projected-pack items have `isEquipped == true`.
  The slot-vacancy logic is equivalent, just derived differently. This section is already the
  most complex part of the frontend refactor.

### `ui/web/src/screens/Bank.tsx`
- Equipment section: derive from `pack.filter(i => i.isEquipped)` rather than
  `inventory.equipment.*`. Functionally identical.

---

## Layer 5 — Tests

### `tests/Dreamlands.Game.Tests/MechanicsTests.cs`
- `Equip_MovesFromPackToEquipment` → `Equip_SetsIsEquippedFlagInPack` (item stays in pack)
- `Equip_SwapsOldItemBackToPack` → `Equip_ClearsOldItemIsEquipped` (no movement; old item
  stays in pack with `IsEquipped = false`)
- `Unequip_MovesFromEquipmentToPack` → `Unequip_ClearsIsEquippedFlag`
- New: `Equip_WhenPackFull_StillSucceeds` — the pack is not full from the engine's perspective
  since the item is already in it; verify flag flip doesn't reject on capacity

### `tests/Dreamlands.Game.Tests/BankTests.cs`
- Deposit-from-equipped tests: assertions change (item was in pack with `IsEquipped = true`,
  after deposit it's in bank with flag cleared)

---

## Blast Radius Summary

| Area | Files | Risk |
|---|---|---|
| Delete `EquippedGear` | 1 file deleted, ~10 reference sites | Low — all migrate to computed helpers |
| `ItemInstance` new field | 1 field | Low — null/false on old docs |
| Balance constant | 1 line | Trivial |
| `PlayerState` | Remove 1 property, add 3 computed | Low |
| `Mechanics.cs` equip/unequip | ~50 lines rewritten | Medium — logic simplifies substantially |
| `Bank.cs`, `Market.cs` | Slot lookup pattern | Low-medium |
| `CombatPlayerProfile.cs` | 2 property reads | Trivial |
| `GameFunctions.cs` response builder | ~10 lines | Low |
| `Inventory.tsx` | Option A adds equipped badge to pack tab | Medium |
| `Market.tsx` projection | Moderate rewrite of ~25-line block | Medium |
| `Bank.tsx` | 3-line derived read | Trivial |
| Tests | Update ~6, add ~1 | Low |

Total: ~12 C# files, ~3 TypeScript files, plus tests. No new abstraction layers. Deleting
`EquippedGear` is the simplification — most touch points get shorter, not longer.

## UI Tab Decision

With `inventory_consolidation.md` landing alongside this plan, the three-tab model (Pack /
Haversack / Equipped) collapses to a single flat list. Decision: **remove the Equipped tab**.

Rationale: the tab made sense when body, pack, and haversack were three non-overlapping
containers. With haversack gone and equipped being a flag on pack items, the tabs now represent
"big stuff" and "small odds and ends" — a distinction that doesn't need navigation. The flat list
groups by category (equipped gear → unequipped gear → supplies → tools → hauls) with sticky
headers, and equipped items carry a visible badge. Everything is scannable without switching tabs.

See `inventory_consolidation.md` §5 for the full flat-list group spec.

## Open Question

**Unequip verb signature**: currently `unequip weapon` (slot name). Post-change this still works
— engine scans pack for the equipped item of that type. No wire-format change. Keeping it as-is
is the path of least resistance.
