using Dreamlands.Encounter;
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

    public static Encounter.Encounter? PickOverworld(GameSession session, Dreamlands.Map.Node node)
    {
        var category = GetCategory(node);
        if (category == null) return null;

        var pool = session.Bundle.GetByCategory(category);
        var available = pool.Where(e => !session.Player.UsedEncounterIds.Contains(e.Id)).ToList();
        if (available.Count == 0) return null;

        return available[session.Rng.Next(available.Count)];
    }

    public static Encounter.Encounter? GetDungeonStart(GameSession session, string dungeonId)
    {
        var category = $"dungeons/{dungeonId}";
        var pool = session.Bundle.GetByCategory(category);
        return pool.FirstOrDefault(e => e.Id.Equals("Start", StringComparison.OrdinalIgnoreCase))
            ?? pool.FirstOrDefault();
    }

    public static Encounter.Encounter? ResolveNavigation(GameSession session, string encounterId)
    {
        if (session.Player.CurrentDungeonId is { } dungeonId)
        {
            var category = $"dungeons/{dungeonId}";
            var pool = session.Bundle.GetByCategory(category);
            var match = pool.FirstOrDefault(e => e.Id.Equals(encounterId, StringComparison.OrdinalIgnoreCase));
            if (match != null) return match;
        }
        return session.Bundle.GetById(encounterId);
    }
}
