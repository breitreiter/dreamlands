using Dreamlands.Map;
using Dreamlands.Rules;

namespace MapGen;

public static class DungeonPlacer
{
    private const int MinSeparation = 8;
    private const int SettlementTooClose = 5;
    private const int SettlementTooFar = 15;

    private static readonly Dictionary<string, Terrain> BiomeToTerrain = new()
    {
        ["forest"] = Terrain.Forest,
        ["mountains"] = Terrain.Mountains,
        ["swamp"] = Terrain.Swamp,
        ["scrub"] = Terrain.Scrub,
        ["plains"] = Terrain.Plains,
    };

    public static void PlaceDungeons(Map map, List<DungeonEntry> roster, Random rng)
    {
        var traversable = map.AllNodes()
            .Where(n => !n.IsWater && n.DistanceFromCity < int.MaxValue && n.Y > 0)
            .ToDictionary(n => (n.X, n.Y));

        if (traversable.Count == 0)
            return;

        var placed = new List<Node>();

        // Collect settlement positions for proximity scoring
        var settlements = map.AllNodes()
            .Where(n => n.Poi?.Kind == PoiKind.Settlement)
            .ToList();

        // Shuffle roster so tie-breaking isn't biased by file order
        var shuffled = roster.OrderBy(_ => rng.Next()).ToList();

        foreach (var entry in shuffled)
        {
            if (!BiomeToTerrain.TryGetValue(entry.Biome, out var terrain))
                continue;

            var candidates = traversable.Values
                .Where(n => n.Terrain == terrain
                    && n.Poi == null
                    && (n.Region?.Tier ?? 0) >= entry.TierMin
                    && (n.Region?.Tier ?? 0) <= entry.TierMax)
                .ToList();

            if (candidates.Count == 0)
                continue;

            // Score candidates
            var scored = new List<(Node node, int score)>();
            foreach (var node in candidates)
            {
                int connections = map.LandNeighbors(node).Count();
                // Strongly prefer dead ends, then low connectivity
                int score = connections switch
                {
                    1 => 100,
                    2 => 60,
                    3 => 30,
                    _ => 10
                };

                // Penalize proximity to already-placed dungeons
                int minDist = MinDistance(node, placed);
                if (minDist < MinSeparation)
                    score -= (MinSeparation - minDist) * 15;

                // Penalize being too close or too far from nearest settlement
                int settlementDist = MinDistance(node, settlements);
                if (settlementDist < SettlementTooClose)
                    score -= (SettlementTooClose - settlementDist) * 20;
                else if (settlementDist > SettlementTooFar)
                    score -= (settlementDist - SettlementTooFar) * 20;

                // Randomize tiebreaking
                score += rng.Next(10);
                scored.Add((node, score));
            }

            var best = scored.OrderByDescending(s => s.score).First().node;
            best.Poi = new Poi(PoiKind.Dungeon, entry.Name)
            {
                DungeonId = entry.Id,
                DecalFile = entry.Decal
            };
            placed.Add(best);
        }
    }

    private static int MinDistance(Node start, List<Node> targets)
    {
        if (targets.Count == 0)
            return int.MaxValue;

        int min = int.MaxValue;
        foreach (var t in targets)
        {
            int d = Math.Abs(start.X - t.X) + Math.Abs(start.Y - t.Y);
            if (d < min) min = d;
        }
        return min;
    }
}
