# Item Data Model

This document describes the data model for items as implemented across `Dreamlands.Rules`,
`Dreamlands.Game`, and `Dreamlands.Flavor`.

## Two-Layer Architecture

Items exist as two distinct objects:

1. **ItemDef** — a static, immutable definition in the balance catalog. Declares what an
   item *is*: its type, bonuses, cost, biome, shop tier. Lives in `lib/Rules/ItemDef.cs`.
2. **ItemInstance** — a runtime record representing an item the player actually possesses.
   Carries a `DefId` back-reference to the catalog plus a display name that may differ from
   the catalog name (food items get biome-flavored names). Lives in `lib/Game/ItemInstance.cs`.

```
ItemDef (catalog)                    ItemInstance (player inventory)
┌─────────────────────┐              ┌──────────────────────────┐
│ Id: "food_protein"  │──────────────│ DefId: "food_protein"    │
│ Name: "Meat & Fish" │              │ DisplayName: "Salt Beef" │
│ Type: Consumable    │              │ FoodType: Protein        │
│ FoodType: Protein   │              │ Description: "Thick..."  │
│ Cost: Trivial       │              └──────────────────────────┘
└─────────────────────┘
```

### ItemDef Fields

| Field | Type | Purpose |
|-------|------|---------|
| `Id` | `string` | Machine key, snake_case (e.g. `"hatchet"`, `"food_grain"`) |
| `Name` | `string` | Default display name |
| `Description` | `string?` | Flavor text (trade goods and tokens only) |
| `Type` | `ItemType` | Category — determines storage, equipment, and mechanical role |
| `Slots` | `int` | Inventory slots consumed (default 1) |
| `CapacityBonus` | `int` | Reserved for future pack-expanding items |
| `WeaponClass` | `WeaponClass?` | Dagger / Axe / Sword (weapons only) |
| `FoodType` | `FoodType?` | Protein / Grain / Sweets (food consumables only) |
| `Cost` | `Magnitude?` | Price tier — null means not purchasable |
| `Biome` | `string?` | Which biome's shops stock this item (`"any"` = universal) |
| `ShopTier` | `int?` | Settlement tier required to stock this item (1/2/3) |
| `SkillModifiers` | `IReadOnlyDictionary<Skill, int>` | Skill check bonuses when equipped/carried |
| `ResistModifiers` | `IReadOnlyDictionary<string, Magnitude>` | Condition resistance bonuses |
| `Cures` | `IReadOnlySet<string>` | Conditions this item can cure — binary, always improves 1 stack |
| `IsPackItem` | `bool` (computed) | True for Weapon, Armor, Boots, Tool, TradeGood |

### ItemInstance Fields

| Field | Type | Purpose |
|-------|------|---------|
| `DefId` | `string` | Back-reference to `ItemDef.Id` |
| `DisplayName` | `string` | What the player sees (may be flavor-generated) |
| `FoodType` | `FoodType?` | Copied from def for food items |
| `Description` | `string?` | Instance-level flavor text |

---

## Item Types

### Weapons (`ItemType.Weapon`)

Storage: Pack. Equippable to the **Weapon** slot.

Contribute to **Combat** and **Foraging** skill checks via `SkillModifiers`.
Classified by `WeaponClass` (Dagger, Axe, Sword) — currently cosmetic, no mechanical difference.

Scaling: +1 to +5 Combat, with a parallel Foraging track. A weapon might be
excellent for combat but useless for foraging (swords), or mediocre in combat
but strong for foraging (seax, the old tooth).

10 weapons defined. Available in biome-specific settlement shops at tiers 1–2.

### Armor (`ItemType.Armor`)

Storage: Pack. Equippable to the **Armor** slot.

Two mechanical roles that trade off against each other:
- **Cunning bonus** via `SkillModifiers[Skill.Cunning]` — light, agile armor
- **Injury resistance** via `ResistModifiers["injured"]` — heavy, protective armor

Heavy armor (chainmail, scale) penalizes Cunning (-3) while providing injury
resistance. The light/cunning armor track (+1 to +5) is designed but not yet
implemented in the catalog.

5 armor pieces defined. Gap: no armor provides a positive Cunning bonus.

### Boots (`ItemType.Boots`)

Storage: Pack. Equippable to the **Boots** slot.

Provide **exhausted resistance** via `ResistModifiers["exhausted"]`. Scale from
+1 to +5.

3 boots defined.

### Tools (`ItemType.Tool`)

Storage: Pack. Not equippable — carried in Pack and contribute passively.

Two sub-roles:

**Skill tools** provide `SkillModifiers` for Negotiation, Bushcraft, or Mercantile
checks. The skill check system picks the best 2 tools in the Pack for each skill.
Designed as +3 and +2 pairs per skill type.

**Utility tools** provide `ResistModifiers` for conditions (exhausted, thirsty,
freezing, swamp_fever, irradiated, gut_worms). The resist system picks the best
1 or 2 tools depending on the condition.

11 tools defined. Some serve both roles (writing_kit gives Mercantile +2 and
Negotiation +1).

### Consumables (`ItemType.Consumable`)

Storage: Haversack. Not equippable. One slot = one item (no stacking).

Two sub-categories:

**Food** (`FoodType != null`): three base defs — `food_protein`, `food_grain`,
`food_sweets`. Always available in settlements at trivial cost with unlimited
stock. Each occupies 1 haversack slot. Display names are generated dynamically
at purchase/forage time (see "Food Instance Creation" below).

**Medicines** (`FoodType == null`): provide condition cures (`Cures`) or
condition resistance (`ResistModifiers`). Biome-specific with `ShopTier` 2–3.
**Not yet stocked by the market** — `Market.InitializeSettlement` does not
query for consumables. This is a known gap (see TODO). Examples: bandages cure
injured, mudcap_fungus cures poisoned, shustov_tonic both resists and cures
irradiated.

15 consumables defined (3 food + 12 medicines).

### Tokens (`ItemType.Token`)

Storage: Haversack. Not equippable — carried passively.

Always provide exactly **+1** to a single skill check type via `SkillModifiers`.
Earned as dungeon rewards, not purchasable (no `Cost`).

The skill check system searches the Haversack for the first Token matching the
relevant skill and caps the bonus at +1.

1 token defined (ivory_comb, +1 Negotiation). Design calls for one token per
check type, to be awarded from the 21 dungeon encounters.

### Trade Goods (`ItemType.TradeGood`)

Storage: Pack. Not equippable. No mechanical bonuses.

Pure economic items — buy in one biome, sell in another for profit. Each has a
unique `Description` for flavor. Organized by biome (plains, mountains, forest,
scrub, swamp) across shop tiers 1–3.

Cross-biome selling earns a flat gold bonus; same-biome selling takes a penalty.

60+ trade goods defined.

---

## Player Inventory Structure

Source: `lib/Game/PlayerState.cs`

```
PlayerState
├── Pack: List<ItemInstance>          (weapons, armor, boots, tools, trade goods)
│   └── PackCapacity: int             (starting: 3)
├── Haversack: List<ItemInstance>     (food, medicines, tokens)
│   └── HaversackCapacity: int        (starting: 20)
└── Equipment: EquippedGear
    ├── Weapon: ItemInstance?
    ├── Armor: ItemInstance?
    └── Boots: ItemInstance?
```

Routing rule: `ItemDef.IsPackItem` determines which container receives the item.
Equipping moves an item from Pack into the corresponding Equipment slot; unequipping
moves it back.

---

## Instance Creation

Items are instantiated as `ItemInstance` records at several points. The process
differs based on item type.

### Standard Items (non-food)

When a non-food item is gained — whether through encounter mechanics, market
purchase, or random treasure — the instance is created directly from the catalog:

```csharp
new ItemInstance(def.Id, def.Name)
```

The `DisplayName` is simply copied from `ItemDef.Name`. No flavor variation.

Sources that create standard instances:
- **Encounter mechanics**: `+add_item <id>` in `.enc` files → `Mechanics.ApplyAddItem()`
- **Random encounter rewards**: `+add_random_items <count> <category>` → `Mechanics.ApplyAddRandomItems()`
- **Random treasure**: `+get_random_treasure` → `Mechanics.ApplyGetRandomTreasure()`
- **Market purchase**: `Market.Buy()` for non-food items

### Food Items

Food items get biome-flavored display names at creation time. There are only three
`ItemDef` entries (`food_protein`, `food_grain`, `food_sweets`), but each produces
instances with names like "Salt Beef", "Flatbread", or "Maple Candy" depending on
biome and whether the food was foraged or purchased.

**Market purchase path**: `Market.Buy()` accepts an optional `createFood` callback:

```csharp
Func<FoodType, string biome, Random, ItemInstance>? createFood
```

The orchestration layer passes `FlavorText.FoodName()` → `FoodNames.Pick()` to
generate a `(Name, Description)` tuple keyed by `(FoodType, Terrain, foraged=false)`.
The callback returns an `ItemInstance` with the flavored `DisplayName` and
`Description`.

If no callback is provided, food falls back to the catalog name ("Meat & Fish",
"Breadstuffs", "Sweets") with no description.

**Flavor data source**: `lib/Flavor/FoodNames.cs` contains a static dictionary
mapping `(FoodType, Terrain, bool foraged)` → array of `(Name, Description)` tuples.
Each biome has 3 purchased and 3 foraged names per food type (90 total names across
5 biomes × 3 types × 2 sources).

### Encounter-Granted Items

Encounters grant items via mechanic lines in `.enc` files:

| Mechanic | Behavior |
|----------|----------|
| `+add_item <id>` | Looks up `<id>` in the catalog, creates instance with catalog name |
| `+add_random_items <n> <category>` | Picks `n` random items matching category, creates instances |
| `+get_random_treasure` | Picks a random trade good from the catalog |
| `+lose_random_item` | Removes a random item from Pack |

All encounter-created instances use the catalog `Name` directly — no flavor variation.

---

## How Items Affect Skill Checks

Source: `lib/Game/SkillChecks.cs`

Each check type has a fixed formula for which gear contributes:

| Check | Big Gear | Pack Items (best N) | Token |
|-------|----------|---------------------|-------|
| Combat | Weapon | — | +1 |
| Cunning | Armor | — | +1 |
| Negotiation | — | Best 2 tools | +1 |
| Bushcraft | — | Best 2 tools | +1 |
| Mercantile | — | Best 2 tools | +1 |
| Foraging | Weapon | — | +1 |

| Resist | Big Gear | Pack Items | Haversack | Token |
|--------|----------|------------|-----------|-------|
| Injured | Armor | — | — | +1 |
| Poisoned | Armor | — | — | +1 |
| Exhausted | Boots | Best 1 tool | — | +1 |
| Freezing | — | Best 2 tools | — | +1 |
| Thirsty | — | Best 2 tools | — | +1 |
| Swamp fever | — | Best 1 tool | Best 1 consumable | +1 |
| Gut worms | — | Best 1 tool | Best 1 consumable | +1 |
| Irradiated | — | Best 1 tool | Best 1 consumable | +1 |

`Magnitude` values on `ResistModifiers` convert to numeric bonuses via
`CharacterBalance.ResistBonusMagnitudes`: Trivial=+1, Small=+2, Medium=+3,
Large=+4, Huge=+5.

---

## Market and Pricing

Source: `lib/Game/Market.cs`, `lib/Rules/Balance.cs`

### Settlement Stock

`Market.InitializeSettlement` builds a catalog from three hardcoded categories.
Items not matching any category never appear in shops, regardless of their
`Cost`/`ShopTier`/`Biome` values.

| Category | Filter | Stock behavior |
|----------|--------|----------------|
| Food | Hardcoded (`food_protein`, `food_grain`, `food_sweets`) | Unlimited, never depletes |
| Trade goods | `Type == TradeGood && Biome == settlement && ShopTier != null && ShopTier <= tier` | Restocks daily based on settlement size |
| Equipment | `Type is Weapon/Armor/Boots && ShopTier != null && ShopTier <= tier && Cost != null` | Stocks at 1, never restocks |

**Not stocked**: Tools, medicines, and tokens are excluded by item type — the
market never queries for them. Tools and medicines have `Cost`/`ShopTier` on
their defs for future use, but these fields have no effect until the market
code adds stocking logic for those types.

An item with `Cost = null` is not purchasable (equipment filter requires it;
trade goods all have Cost). An item with `ShopTier = null` won't match the
tier filter. Items that should never spawn in shops (tokens, unique encounter
rewards) omit both fields.

### Pricing

Base price comes from `ItemDef.Cost` → `CharacterBalance.CostMagnitudes` lookup
(Magnitude → gold amount).

Modifiers at purchase:
- Mercantile skill discount: 2% per point
- Featured sell item: 15% discount

Modifiers at sale:
- Cross-biome bonus: +10 gold flat
- Same-biome penalty: -10%
- Featured buy item: +25% premium

---

## Enums Reference

```csharp
enum ItemType    { Tool, Consumable, Token, Weapon, Armor, Boots, TradeGood }
enum WeaponClass { Dagger, Axe, Sword }
enum FoodType    { Protein, Grain, Sweets }
enum Magnitude   { Trivial, Small, Medium, Large, Huge }
enum Skill       { Combat, Negotiation, Bushcraft, Cunning, Luck, Mercantile }
```

---

## Source Files

| File | Contains |
|------|----------|
| `lib/Rules/ItemDef.cs` | `ItemType`, `WeaponClass`, `ItemDef`, static catalog (`BuildAll`) |
| `lib/Rules/Balance.cs` | `Magnitude`, `FoodType`, `Skill`, `BalanceData`, pricing/resist magnitude tables |
| `lib/Game/ItemInstance.cs` | `ItemInstance` record |
| `lib/Game/PlayerState.cs` | `PlayerState` inventory fields, `EquippedGear` |
| `lib/Game/Mechanics.cs` | Item gain/loss/equip/unequip/discard mechanics |
| `lib/Game/SkillChecks.cs` | Gear bonus calculation per check type |
| `lib/Game/Market.cs` | Settlement stock, buy/sell, pricing |
| `lib/Flavor/FoodNames.cs` | Biome-specific food name tables |
| `lib/Flavor/FlavorText.cs` | `FoodName()` entry point for flavor generation |
