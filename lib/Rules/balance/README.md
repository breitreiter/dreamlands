# Balance Data Format

This directory contains all tunable balance values for the game, externalized from code for easy iteration.

## Philosophy

- **Formulas stay in code** - Game logic and calculations remain in C#
- **Values live in data** - Numbers, strings, and configuration go here
- **Easy scanning** - YAML format allows quick visual review of related values
- **Context preservation** - Values are grouped by system, not scattered

## File Structure

```
balance/
├── README.md              # This file
├── character.yaml         # Stats, skills, progression
├── conditions.yaml        # Status conditions and drain rates
├── resources.yaml         # Food/water consumption, travel
├── equipment.yaml         # Tools, weapons, armor
├── food.yaml              # Meals and special ingredients
├── trade.yaml             # Economic categories and pricing
├── settlements.yaml       # Services and danger tiers
├── combat.yaml            # HP, damage, difficulty targets
└── encounters.yaml        # Encounter pools and dungeon structure
```

## Design Patterns

### Tier Scaling

Many values scale by danger tier (1-4). When a property has tier-specific values:

```yaml
# Flat value (no scaling)
drain: 2

# Tier-scaled values
tier_drain:
  tier1: 0
  tier2: 2
  tier3: 4
  tier4: 6
```

### Effects Arrays

Items can have multiple effects. Each effect is a key-value pair:

```yaml
effects:
  - resists: [cold, exhausted]
  - capacity_bonus: 5
  - combat_bonus: 3
```

### Availability

Services and items may be limited by location:

```yaml
availability: common        # ~70% of settlements
availability: rare          # ~30% of settlements
availability: temple_settlements
availability: swamp_adjacent_settlements
```

### Regional Modifiers

Trade goods have regional price multipliers (1.0 = base):

```yaml
regional_modifiers:
  mountains: 0.7    # 30% cheaper
  plains: 1.3       # 30% more expensive
```

## Usage in Code

C# code should load these files at startup and reference values by key path:

```csharp
// Pseudocode example
var conditionDrain = Balance.Conditions["diseased"].TierDrain[currentTier];
var startingHealth = Balance.Character.StartingStats.Health;
var swordBonus = Balance.Equipment["good_sword"].Effects.CombatBonus;
```

## Tuning Workflow

1. **Edit YAML** - Change values directly in these files
2. **Hot reload** - If supported, game picks up changes immediately
3. **Test** - Play and observe impact
4. **Iterate** - Repeat until it feels right
5. **Commit** - Balance changes tracked in git

## Open Questions (TBD Values)

Some values are marked as `tbd` or have placeholder comments:

- Settlement entertainment prevalence
- Tier 3 exotic condition details
- Specific drain values depend on final Health/Spirits scale
- Infested condition resist item (treated bedroll is placeholder)

These will be filled in as design solidifies.

## Convention Notes

- **Comments** use `#` and explain design intent
- **Underscores in numbers** improve readability (e.g., `10_000` or `5_per_treatment`)
- **Boolean flags** use `true`/`false`, not yes/no or 1/0
- **Arrays** use `[]` for inline lists, indented `-` for structured lists
- **Strings** can be unquoted unless they contain special chars

## Validation

Consider adding schema validation to catch typos:

- Required fields present
- Numbers in valid ranges
- References to other data exist
- No duplicate IDs

This can be a build-time check or runtime assertion.
