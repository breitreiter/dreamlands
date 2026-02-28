# Inventory Screen Build Plan

Reference mockup: `assets/UI/mockup_inventory.png`

## Architecture

The inventory screen replaces the current modal overlay (`Inventory.tsx`) with a full-screen
three-column layout. It should be a dedicated screen mode, not an overlay on top of explore.

The server already sends `StatusInfo` + `InventoryInfo` on every response. The main API gap
is that `ItemInfo` only has `defId/name/description` — no modifiers, cost, or type. The market
endpoint already sends richer data; we need to bring that same richness to the main game response.

A new **mechanics summary** (resistances, encounter check bonuses, other bonuses) needs to be
computed server-side and sent as a new DTO alongside status + inventory.

---

## Phase 1: Enrich the API

**Goal**: The server response carries all data needed by the inventory screen. No UI changes yet.

### 1a. Enrich `ItemInfo` DTO

In `GameResponse.cs`, add fields to `ItemInfo`:

```csharp
public class ItemInfo
{
    public string DefId { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    // New fields:
    public string Type { get; init; } = "";                            // "weapon", "armor", etc.
    public int? Cost { get; init; }                                     // gold cost (resolved from Magnitude)
    public Dictionary<string, int> SkillModifiers { get; init; } = []; // "combat": 2
    public Dictionary<string, int> ResistModifiers { get; init; } = []; // "injured": 3
    public int ForagingBonus { get; init; }
    public List<string> Cures { get; init; } = [];
    public bool IsEquippable { get; init; }                             // true for weapon/armor/boots
}
```

In `Program.cs`, update `BuildInventory` to populate these from `ItemDef.All[i.DefId]`.

### 1b. Add `MechanicsInfo` DTO

New DTO representing the computed mechanics panel. Added to `GameResponse` as `Mechanics`.

```csharp
public class MechanicsInfo
{
    public List<MechanicLine> Resistances { get; init; } = [];
    public List<MechanicLine> EncounterChecks { get; init; } = [];
    public List<MechanicLine> Other { get; init; } = [];
}

public class MechanicLine
{
    public string Label { get; init; } = "";   // e.g. "Lost", "Mercantile"
    public string Value { get; init; } = "";   // e.g. "+4", "5%"
    public string Source { get; init; } = "";  // e.g. "Bushcraft + gear", "Gear"
}
```

**Computation** (new helper `BuildMechanics(PlayerState p)`):

- **Resistances**: For each condition in `ConditionDef.All`, sum resistance from:
  - Skill-based resist (Bushcraft level for environment conditions, Combat for injured, etc.)
  - Equipment resist modifiers (weapon + armor + boots + all pack tools)
  - Only include conditions where total resist > 0
  - Source: classify as "Gear", "Skill + gear", or skill name

- **Encounter Checks**: For each skill, compute `base skill level + equipment bonus`:
  - Base = `p.Skills[skill]`
  - Equipment = sum of `SkillModifiers[skill]` across all equipped items + pack tools
  - Source: skill name + "gear" if equipment contributes

- **Other**: Special item effects that don't fit resistances or checks:
  - Better prices (from Mercantile + gear)
  - Reroll chance (from Luck)
  - Foraging bonus (from Bushcraft + weapon foraging bonus)

### 1c. Add `ConditionInfo` to `StatusInfo`

Currently conditions are `Record<string, int>` (id → stacks). The UI needs display names
and descriptions. Enrich to:

```csharp
public class ConditionInfo
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public int Stacks { get; init; }
    public string Description { get; init; } = ""; // from ConditionFlavor.Ongoing
}
```

Replace `StatusInfo.Conditions` from `Dictionary<string, int>` to `List<ConditionInfo>`.

### 1d. Add skill flavor to `StatusInfo`

Currently skills are `Dictionary<string, string>` (e.g. `"combat": "+4"`). Add flavor text.

```csharp
public class SkillInfo
{
    public string Name { get; init; } = "";       // "Combat"
    public int Level { get; init; }                // 0, 2, 4
    public string Formatted { get; init; } = "";   // "+4"
    public string Flavor { get; init; } = "";      // "You read intent in..."
}
```

Replace `StatusInfo.Skills` from `Dictionary<string, string>` to `List<SkillInfo>`.

Wire skill flavor text into code as a static lookup in `lib/Rules/` (alongside `ConditionFlavor`).

### 1e. Fix TypeScript types

Update `ui/web/src/api/types.ts` to match all new DTOs. Fix the existing `skills: Record<string, number>`
type mismatch (server sends strings, not numbers).

### 1f. Update existing UI to not break

The enriched DTOs change shapes that `StatusBar.tsx`, `Explore.tsx`, `Market.tsx`, and `Inventory.tsx`
currently depend on. Update all consumers to use the new shapes so nothing breaks.

### Build & verify

`dotnet build Dreamlands.sln && cd ui/web && npm run build`

---

## Phase 2: Left Column — Character Panel

**Goal**: React component showing character name, vitals, skills, and conditions.

### Layout (from mockup)
- Character name header (e.g. "THE MERCHANT") — **hardcoded or from a future name system**
- Health bar (red gradient) with `16/20` label
- Spirits bar (purple gradient) with `16/20` label
- Skills list: each skill shows level badge, name, and flavor text
- Conditions section at bottom: icon + name + description

### Implementation
- New component `CharacterPanel.tsx` (or inline in rebuilt `Inventory.tsx`)
- Reuse `StatBar` component for health/spirits
- Skill levels: circular badge showing `+N`, colored by tier (0=dim, 2=normal, 4=bright)
- Conditions: simple list with condition name + description from `ConditionInfo`
- Data source: `GameResponse.status` (already available in GameContext)

---

## Phase 3: Middle Column — Mechanics Panel

**Goal**: React component showing computed resistances, check bonuses, and other bonuses.

### Layout (from mockup)
Three tables, each with Label / Value / Source columns:
1. **Resistances** — condition resist values with source
2. **Encounter Checks** — per-skill check bonus with source
3. **Other** — special bonuses (better prices, reroll, foraging)

### Implementation
- New component `MechanicsPanel.tsx`
- Three `<table>` or flex-based sections
- Data source: `GameResponse.mechanics` (new from Phase 1)
- Condition icons next to resistance labels (reuse existing condition icon set)

---

## Phase 4: Right Column — Inventory Panel

**Goal**: Rebuild inventory with tabs, rich item cards, and proper equip/discard UX.

### Layout (from mockup)
- Three tabs: Pack / Haversack / Equipped
- Each item is a card showing: icon, name, cost (gold), description line, equip + discard buttons
- Equipped items show equip icon button instead of equip, plus discard
- Empty slots shown as placeholder cards
- Items with no equip affordance (tools, trade goods) only show discard

### Implementation
- Rebuild `Inventory.tsx` as a tabbed panel
- `ItemCard` sub-component: icon, name, cost badge, modifier summary, action buttons
- Pack tab: items from `inventory.pack`, "Equip" only if `isEquippable`
- Haversack tab: items from `inventory.haversack`, discard only
- Equipped tab: 3 slots (weapon/armor/boots) with unequip action, empty slot placeholders
- Capacity indicator: `used/total` count or subtle bar

### Tab details
- **Pack**: grid of item cards. Equip button only on weapons/armor/boots. Discard on everything.
- **Haversack**: grid of item cards. Discard only.
- **Equipped**: 3 fixed slots. Shows equipped item card or "Empty slot" placeholder. Unequip button.

---

## File Changes Summary

| Phase | Files Modified | Files Created |
|-------|---------------|---------------|
| 1 | `server/GameServer/GameResponse.cs`, `server/GameServer/Program.cs`, `ui/web/src/api/types.ts`, `ui/web/src/screens/StatusBar.tsx`, `ui/web/src/screens/Explore.tsx`, `ui/web/src/screens/Inventory.tsx`, `ui/web/src/screens/Market.tsx` | `lib/Rules/SkillFlavor.cs` |
| 2 | `ui/web/src/screens/Inventory.tsx` (rebuild as full screen) | — |
| 3 | `ui/web/src/screens/Inventory.tsx` | — |
| 4 | `ui/web/src/screens/Inventory.tsx` | — |

## Resolved Questions

1. **Character name**: "The Merchant" — not a name, it's a title (like "The Courier" in Fallout).
   Hardcoded for now. No name field on PlayerState.
2. **Screen mode**: Full screen, not overlay. Inventory becomes a proper screen mode like
   settlement/market, replacing explore view.
3. **Item icons**: Full SVG asset coverage expected in `assets/icons/`.
