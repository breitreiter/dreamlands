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
    /// Returns exactly balance.Tactical.DeckSize cards, shuffled.
    /// </summary>
    public static List<OpeningSnapshot> Build(
        TacticalEncounter encounter,
        PlayerState player,
        BalanceData balance,
        List<ActiveTimer> drawnTimers,
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

        // 4. Fill remaining with global chaff
        if (deck.Count < deckSize)
            FillWithChaff(deck, deckSize, balance, rng);

        // 5. Shuffle
        Shuffle(deck, rng);
        return deck;
    }

    /// <summary>Draw one card from the deck. Reshuffles when exhausted.</summary>
    public static OpeningSnapshot Draw(TacticalState state, Random rng)
    {
        if (state.DrawIndex >= state.Deck.Count)
        {
            Shuffle(state.Deck, rng);
            state.DrawIndex = 0;
        }
        return state.Deck[state.DrawIndex++];
    }

    /// <summary>Draw multiple cards from the deck.</summary>
    public static List<OpeningSnapshot> DrawMultiple(TacticalState state, int count, Random rng)
    {
        var result = new List<OpeningSnapshot>(count);
        for (int i = 0; i < count; i++)
            result.Add(Draw(state, rng));
        return result;
    }

    /// <summary>Fisher-Yates shuffle in place.</summary>
    public static void Shuffle(List<OpeningSnapshot> deck, Random rng)
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }
    }

    // ── Internals ──────────────────────────────────────

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

        // Equipment cards (weapon, armor, boots — any equipped item with TacticalCards)
        foreach (var item in GetEquippedItems(player, balance))
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
        // Gated filler first (requires matching player inventory)
        foreach (var o in encounter.Openings)
        {
            if (deck.Count >= deckSize) return;
            if (o.Requires != null && Conditions.Evaluate(o.Requires, player, balance, new Random(0)))
            {
                deck.Add(SnapshotFromOpening(o));
            }
        }

        // Then ungated scenery
        foreach (var o in encounter.Openings)
        {
            if (deck.Count >= deckSize) return;
            if (o.Requires == null)
            {
                deck.Add(SnapshotFromOpening(o));
            }
        }
    }

    static void FillWithChaff(List<OpeningSnapshot> deck, int deckSize, BalanceData balance, Random rng)
    {
        var tb = balance.Tactical;
        var chaffArchetypes = tb.Chaff;
        int i = 0;
        while (deck.Count < deckSize && chaffArchetypes.Count > 0)
        {
            var archId = chaffArchetypes[i % chaffArchetypes.Count];
            if (tb.Archetypes.TryGetValue(archId, out var arch))
            {
                // Use archetype ID as name — the UI will apply flavor names based on encounter subtype
                deck.Add(SnapshotFromArchetype(arch, archId));
            }
            i++;
        }
    }

    static OpeningSnapshot SnapshotFromArchetype(TacticalArchetype arch, string name) => new()
    {
        Name = name,
        CostKind = Enum.Parse<CostKind>(arch.CostKind, ignoreCase: true),
        CostAmount = arch.CostAmount,
        EffectKind = Enum.Parse<EffectKind>(arch.EffectKind, ignoreCase: true),
        EffectAmount = arch.EffectAmount,
    };

    static OpeningSnapshot SnapshotFromOpening(OpeningDef o) => new()
    {
        Name = o.Name,
        CostKind = o.Cost.Kind,
        CostAmount = o.Cost.Amount,
        EffectKind = o.Effect.Kind,
        EffectAmount = o.Effect.Amount,
    };

    static IEnumerable<ItemDef> GetEquippedItems(PlayerState player, BalanceData balance)
    {
        var equipment = player.Equipment;
        if (equipment.Weapon?.DefId is { } wid && balance.Items.TryGetValue(wid, out var w))
            yield return w;
        if (equipment.Armor?.DefId is { } aid && balance.Items.TryGetValue(aid, out var a))
            yield return a;
        if (equipment.Boots?.DefId is { } bid && balance.Items.TryGetValue(bid, out var b))
            yield return b;
    }
}
