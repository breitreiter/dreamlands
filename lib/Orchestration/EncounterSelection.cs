using Dreamlands.Encounter;
using Dreamlands.Game;
using Dreamlands.Map;
using Dreamlands.Rules;

namespace Dreamlands.Orchestration;

public static class EncounterSelection
{
    static readonly Dictionary<Terrain, string> TerrainDirs = new()
    {
        [Terrain.Plains] = "plains",
        [Terrain.Forest] = "forest",
        [Terrain.Mountains] = "mountains",
        [Terrain.Swamp] = "swamp",
        [Terrain.Scrub] = "scrub",
    };

    public static string? GetCategory(Dreamlands.Map.Node node)
    {
        if (!TerrainDirs.TryGetValue(node.Terrain, out var dir))
            return null;
        var tier = node.Region?.Tier ?? 1;
        return $"{dir}/tier{tier}";
    }

    public static string? GetPoiCategory(Dreamlands.Map.Node node)
    {
        if (node.Poi == null) return null;
        if (!TerrainDirs.TryGetValue(node.Terrain, out var dir))
            return null;

        return node.Poi.Kind switch
        {
            PoiKind.Dungeon when node.Poi.DungeonId != null => $"arcs/{dir}/{node.Poi.DungeonId}",
            PoiKind.Settlement => $"settlements/{dir}",
            _ => null,
        };
    }

    public static Encounter.Encounter? PickOverworld(GameSession session, Dreamlands.Map.Node node)
    {
        var category = GetCategory(node);
        if (category == null) return null;

        var pool = session.Bundle.GetByCategory(category);
        var available = pool
            .Where(e => e.Recurring || !session.Player.UsedEncounterIds.Contains(e.Id))
            .Where(e => e.Requires.Count == 0 || e.Requires.All(r => Conditions.Evaluate(r, session.Player, session.Balance, session.Rng)))
            .ToList();
        if (available.Count == 0) return null;

        return available[session.Rng.Next(available.Count)];
    }

    public static Encounter.Encounter? GetDungeonStart(GameSession session, Dreamlands.Map.Node node)
    {
        var category = GetPoiCategory(node);
        if (category == null) return null;

        var pool = session.Bundle.GetByCategory(category);
        return pool.FirstOrDefault(e => e.ShortId.Equals("Start", StringComparison.OrdinalIgnoreCase))
            ?? pool.FirstOrDefault();
    }

    public static Encounter.Encounter? ResolveNavigation(GameSession session, string encounterId, Dreamlands.Map.Node node)
    {
        // Try resolving as a short name relative to the current encounter's category
        var category = session.CurrentEncounter?.Category
            ?? (session.Player.CurrentDungeonId != null ? GetPoiCategory(node) : null);
        if (category != null)
        {
            var pool = session.Bundle.GetByCategory(category);
            var match = pool.FirstOrDefault(e => e.ShortId.Equals(encounterId, StringComparison.OrdinalIgnoreCase));
            if (match != null) return match;
        }

        // Fall back to direct qualified lookup
        return session.Bundle.GetById(encounterId);
    }

    public static IReadOnlyList<Encounter.Encounter> GetAvailableAtPoi(GameSession session, Dreamlands.Map.Node node)
    {
        var category = GetPoiCategory(node);
        if (category == null) return [];

        var pool = session.Bundle.GetByCategory(category);
        return pool
            .Where(e => e.Recurring || !session.Player.UsedEncounterIds.Contains(e.Id))
            .Where(e => e.Requires.Count == 0 || e.Requires.All(r => Conditions.Evaluate(r, session.Player, session.Balance, session.Rng)))
            .ToList();
    }

    /// <summary>
    /// Returns all settlement-triggered encounters eligible for stocking at this node,
    /// excluding any already in the settlement's offers or used by the player.
    /// </summary>
    public static IReadOnlyList<Encounter.Encounter> GetEligibleStorylets(
        GameSession session, Dreamlands.Map.Node node, IReadOnlyList<string> currentOffers)
    {
        if (!TerrainDirs.TryGetValue(node.Terrain, out var biome)) return [];
        var tier = node.Region?.Tier;

        // Combine category-based settlement pool with trigger-based pool
        var categoryPool = session.Bundle.GetByCategory($"settlements/{biome}");
        var triggerPool = session.Bundle.GetByTrigger("settlement", biome, tier);
        var pool = categoryPool.Concat(triggerPool).DistinctBy(e => e.Id);

        return pool
            .Where(e => !currentOffers.Contains(e.Id))
            .Where(e => e.Recurring || !session.Player.UsedEncounterIds.Contains(e.Id))
            .Where(e => e.Requires.Count == 0 || e.Requires.All(r => Conditions.Evaluate(r, session.Player, session.Balance, session.Rng)))
            .ToList();
    }
}
