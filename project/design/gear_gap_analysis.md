# Gear Gap Analysis

Checks where the player cannot reach the design-target +5 gear bonus (ignoring tokens, which are entirely missing).

Item definitions: `lib/Rules/ItemDef.cs`
Skill check bonus logic: `lib/Game/SkillChecks.cs`

## Encounter Checks

| Check | Gear Source | Current Max | Best Items | Gap |
|-------|-----------|-------------|------------|-----|
| Combat | Weapon (big) | +4 | bardiche, scimitar, arming_sword | needs +5 weapon |
| Cunning | Armor (big) | 0 | no armor has positive Cunning mod | needs +5 worth of armor |
| Negotiation | Two best tools | +4 | peoples_borderlands (+3) + writing_kit (+1) | needs +2 negotiation tool |
| Bushcraft | Two best tools | +2 | yoriks_guide (+2), no second tool | needs +3 bushcraft tool |
| Mercantile | Two best tools | +2 | writing_kit (+2), no second tool | needs +3 mercantile tool |

## Condition Resist Checks

| Check | Gear Source (per dice_mechanics.md) | Current Max | Gap |
|-------|-------------------------------------|-------------|-----|
| Injury resist | Armor (big) + token | 0 | no armor has numeric bonus for this |
| Foraging | Weapon (big) + token | +4 (reusing Combat mod) | needs +5 weapon (same as Combat) |
| Poison resist | Armor (big) + token | 0 | same as Cunning — no positive armor bonus |
| Exhausted resist | Boots (big) + equipment (small) | 0 | boots have no SkillModifiers, only ResistModifiers |
| Freezing/Thirsty | Two small gear + token | 0 | heavy_furs/canteen use ResistModifiers (Magnitude), not SkillModifiers |
| Swamp Fever/Road Flux/Irradiated | Consumable (big) + equipment (small) | 0 | consumables/tools use ResistModifiers (Magnitude), not numeric bonuses |

## Systemic Issue: Condition Resists

The condition resist checks in `dice_mechanics.md` describe numeric gear bonuses (big/small), but actual items use `ResistModifiers` which are `Magnitude` enums (Small/Medium/Large), not integers. There is no code path converting `ResistModifiers` into numeric check bonuses. Either:

- The resist system needs its own bonus conversion (Magnitude -> int)
- Or items need `SkillModifiers` entries for the relevant resist checks

This affects all 6 condition resist check types. None can reach any positive gear bonus right now.

## Items Needed (Encounter Checks)

1. **Combat**: one +5 weapon (tier 3 / dungeon reward)
2. **Cunning**: positive armor-to-Cunning progression (currently armor only penalizes or is neutral)
3. **Negotiation**: one +2 negotiation tool (pairs with peoples_borderlands +3 for +5)
4. **Bushcraft**: one +3 bushcraft tool (pairs with yoriks_guide +2 for +5)
5. **Mercantile**: one +3 mercantile tool (pairs with writing_kit +2 for +5)
