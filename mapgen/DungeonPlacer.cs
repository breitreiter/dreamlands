using Dreamlands.Map;
using Dreamlands.Rules;

namespace MapGen;

public static class DungeonPlacer
{
    private const int MinSeparation = 8;
    private const int SettlementTooClose = 5;
    private const int SettlementTooFar = 15;
    private const int EdgeMargin = 3;
    private const int SectorGridSize = 3;

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
        var unplaced = new List<DungeonEntry>();

        var settlements = map.AllNodes()
            .Where(n => n.Poi?.Kind == PoiKind.Settlement)
            .ToList();

        // Phase 1: T1 dungeons (region-locked, just place them)
        var t1 = roster.Where(e => e.TierMax <= 1).OrderBy(_ => rng.Next()).ToList();
        foreach (var entry in t1)
            PlaceOne(map, entry, traversable, placed, settlements, unplaced, rng, isT3: false);

        // Phase 2: T3 dungeons (region-centered, one per biome)
        var t3BiomesUsed = new HashSet<string>();
        var t3 = roster.Where(e => e.TierMin >= 3).OrderBy(_ => rng.Next()).ToList();
        foreach (var entry in t3)
        {
            if (!t3BiomesUsed.Add(entry.Biome))
                continue;
            PlaceOne(map, entry, traversable, placed, settlements, unplaced, rng, isT3: true);
        }

        // Phase 3: T2 dungeons — fill dead spaces using sector balancing
        var t2 = roster.Where(e => e.TierMin < 3 && e.TierMax > 1).OrderBy(_ => rng.Next()).ToList();
        foreach (var entry in t2)
            PlaceOne(map, entry, traversable, placed, settlements, unplaced, rng, isT3: false, sectorBalance: true);

        if (unplaced.Count > 0)
        {
            Console.Error.WriteLine($"  WARNING: {unplaced.Count} dungeon(s) could not be placed — map is invalid for production use:");
            foreach (var entry in unplaced)
                Console.Error.WriteLine($"    - {entry.Name} ({entry.Biome} T{entry.TierMin}-T{entry.TierMax})");
        }
    }

    private static void PlaceOne(
        Map map, DungeonEntry entry,
        Dictionary<(int, int), Node> traversable,
        List<Node> placed, List<Node> settlements,
        List<DungeonEntry> unplaced, Random rng,
        bool isT3, bool sectorBalance = false)
    {
        if (!BiomeToTerrain.TryGetValue(entry.Biome, out var terrain))
            return;

        var candidates = traversable.Values
            .Where(n => n.Terrain == terrain
                && n.Poi == null
                && (n.Region?.Tier ?? 0) >= entry.TierMin
                && (n.Region?.Tier ?? 0) <= entry.TierMax
                && n.X >= EdgeMargin && n.X < map.Width - EdgeMargin
                && n.Y >= EdgeMargin && n.Y < map.Height - EdgeMargin)
            .ToList();

        if (candidates.Count == 0)
        {
            unplaced.Add(entry);
            return;
        }

        // Build sector counts from already-placed dungeons
        var sectorCounts = new int[SectorGridSize, SectorGridSize];
        if (sectorBalance)
        {
            foreach (var node in placed)
            {
                var (sx, sy) = GetSector(node, map);
                sectorCounts[sx, sy]++;
            }
        }

        var scored = new List<(Node node, int score)>();
        foreach (var node in candidates)
        {
            int connections = map.LandNeighbors(node).Count();
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

            // Mountains look bad at region edges — reward interior depth
            if (terrain == Terrain.Mountains)
                score += Math.Min(RegionDepth.Compute(node, map), 5) * 8;

            if (isT3)
            {
                // T3: prefer center of region
                var region = node.Region!;
                double cx = region.Nodes.Average(n => n.X);
                double cy = region.Nodes.Average(n => n.Y);
                int distFromCenter = (int)(Math.Abs(node.X - cx) + Math.Abs(node.Y - cy));
                score -= distFromCenter * 3;
            }
            else
            {
                // Settlement proximity scoring
                int settlementDist = MinDistance(node, settlements);
                if (settlementDist < SettlementTooClose)
                    score -= (SettlementTooClose - settlementDist) * 20;
                else if (settlementDist > SettlementTooFar)
                    score -= Math.Min((settlementDist - SettlementTooFar) * 5, 40);
            }

            // Sector balancing: penalize crowded sectors, reward empty ones
            if (sectorBalance)
            {
                var (sx, sy) = GetSector(node, map);
                int count = sectorCounts[sx, sy];
                // Each existing dungeon in the sector costs 25 points
                score -= count * 25;
            }

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

    private static (int sx, int sy) GetSector(Node node, Map map)
    {
        int sx = Math.Clamp(node.X * SectorGridSize / map.Width, 0, SectorGridSize - 1);
        int sy = Math.Clamp(node.Y * SectorGridSize / map.Height, 0, SectorGridSize - 1);
        return (sx, sy);
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
