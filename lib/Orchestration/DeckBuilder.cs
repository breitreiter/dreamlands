using Dreamlands.Game;
using Dreamlands.Rules;
using Dreamlands.Tactical;

namespace Dreamlands.Orchestration;

/// <summary>
/// Assembles a fixed-size deck of openings for a tactical encounter.
/// Collection cards come from equipped gear + skill level.
/// Remaining slots filled with encounter filler then global chaff.
/// </summary>
public static class DeckBuilder
{
    /// <summary>
    /// Build a deck for the given encounter and player state.
    /// Returns exactly balance.Tactical.DeckSize cards, shuffled once.
    /// The deck loops without reshuffling when exhausted.
    /// </summary>
    public static List<OpeningSnapshot> Build(
        TacticalEncounter encounter,
        PlayerState player,
        BalanceData balance,
        Random rng)
    {
        var tb = balance.Tactical;
        var deckSize = tb.DeckSize;
        var deck = new List<OpeningSnapshot>(deckSize);

        // 1. Gather collection cards from gear + skill
        var collection = GatherCollection(encounter, player, balance);

        // 2. Add collection cards (up to deck size)
        foreach (var card in collection.Take(deckSize))
            deck.Add(card);

        // 3. Fill remaining with encounter filler (gated first, then scenery)
        if (deck.Count < deckSize)
            FillFromEncounter(deck, deckSize, encounter, player, balance);

        Shuffle(deck, rng);
        return deck;
    }

    /// <summary>Draw one card from the deck. Wraps to the start when exhausted.</summary>
    public static OpeningSnapshot Draw(TacticalState state)
    {
        if (state.DrawIndex >= state.Deck.Count)
            state.DrawIndex = 0;
        return state.Deck[state.DrawIndex++];
    }

    /// <summary>Draw multiple cards from the deck.</summary>
    public static List<OpeningSnapshot> DrawMultiple(TacticalState state, int count)
    {
        var result = new List<OpeningSnapshot>(count);
        for (int i = 0; i < count; i++)
            result.Add(Draw(state));
        return result;
    }

    // ── Internals ──────────────────────────────────────

    static void Shuffle(List<OpeningSnapshot> deck, Random rng)
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }
    }

    static List<OpeningSnapshot> GatherCollection(
        TacticalEncounter encounter, PlayerState player, BalanceData balance)
    {
        var tb = balance.Tactical;
        var cards = new List<OpeningSnapshot>();

        // Resolve governing skill
        var stat = encounter.Stat;
        Skill? skill = stat != null ? Skills.FromScriptName(stat) : null;
        int skillLevel = skill.HasValue ? player.Skills.GetValueOrDefault(skill.Value) : 0;

        // Skill-intrinsic cards (cumulative, up to skill level)
        if (skill.HasValue && tb.SkillCards.TryGetValue(skill.Value, out var skillCards))
        {
            for (int i = 0; i < Math.Min(skillLevel, skillCards.Count); i++)
            {
                var card = skillCards[i];
                if (tb.Archetypes.TryGetValue(card.Archetype, out var arch))
                    cards.Add(SnapshotFromArchetype(arch, card.Name));
            }
        }

        // Equipment cards — only from items relevant to the encounter's governing skill
        foreach (var item in GetEquippedItems(player, balance, skill))
        {
            foreach (var card in item.TacticalCards)
            {
                if (tb.Archetypes.TryGetValue(card.Archetype, out var arch))
                    cards.Add(SnapshotFromArchetype(arch, card.Name));
            }
        }

        return cards;
    }

    static void FillFromEncounter(
        List<OpeningSnapshot> deck, int deckSize,
        TacticalEncounter encounter, PlayerState player, BalanceData balance)
    {
        var archetypes = balance.Tactical.Archetypes;

        // Gated filler first (requires matching player inventory)
        foreach (var o in encounter.Openings)
        {
            if (deck.Count >= deckSize) return;
            if (o.Requires != null && Conditions.Evaluate(o.Requires, player, balance, new Random(0)))
            {
                if (archetypes.TryGetValue(o.Archetype, out var arch))
                    deck.Add(SnapshotFromArchetype(arch, o.Name));
            }
        }

        // Then ungated scenery
        foreach (var o in encounter.Openings)
        {
            if (deck.Count >= deckSize) return;
            if (o.Requires == null)
            {
                if (archetypes.TryGetValue(o.Archetype, out var arch))
                    deck.Add(SnapshotFromArchetype(arch, o.Name));
            }
        }
    }

    static T ParseSnakeCase<T>(string value) where T : struct, Enum =>
        Enum.Parse<T>(value.Replace("_", ""), ignoreCase: true);

    static OpeningSnapshot SnapshotFromArchetype(TacticalArchetype arch, string name) => new()
    {
        Name = name,
        CostKind = ParseSnakeCase<CostKind>(arch.CostKind),
        CostAmount = arch.CostAmount,
        EffectKind = ParseSnakeCase<EffectKind>(arch.EffectKind),
        EffectAmount = arch.EffectAmount,
    };

    static IEnumerable<ItemDef> GetEquippedItems(PlayerState player, BalanceData balance, Skill? encounterSkill)
    {
        var equipment = player.Equipment;
        if (equipment.Weapon?.DefId is { } wid && balance.Items.TryGetValue(wid, out var w) && IsRelevant(w, encounterSkill))
            yield return w;
        if (equipment.Armor?.DefId is { } aid && balance.Items.TryGetValue(aid, out var a) && IsRelevant(a, encounterSkill))
            yield return a;
        if (equipment.Boots?.DefId is { } bid && balance.Items.TryGetValue(bid, out var b) && IsRelevant(b, encounterSkill))
            yield return b;

        // Tokens and tools in Pack can also contribute cards
        foreach (var item in player.Pack)
        {
            if (balance.Items.TryGetValue(item.DefId, out var def) && def.TacticalCards.Count > 0 && IsRelevant(def, encounterSkill))
                yield return def;
        }
    }

    /// <summary>
    /// An item is relevant if it has no skill modifiers (generic gear) or
    /// if it has a modifier for the encounter's governing skill.
    /// </summary>
    static bool IsRelevant(ItemDef item, Skill? encounterSkill) =>
        item.SkillModifiers.Count == 0
        || (encounterSkill.HasValue && item.SkillModifiers.ContainsKey(encounterSkill.Value));
}
