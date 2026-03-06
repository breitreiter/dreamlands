using Dreamlands.Rules;

namespace Dreamlands.Game;

public static class HaulGeneration
{
    public record HaulDestination(string SettlementId, string Name, Terrain Biome, int X, int Y);

    private static readonly string[] SectorNames =
    {
        "northwest", "north",  "northeast",
        "west",      "center", "east",
        "southwest", "south",  "southeast"
    };

    public static List<ItemInstance> Generate(
        int x, int y,
        Terrain originBiome,
        bool isLeaf,
        IReadOnlyList<HaulDestination> candidates,
        IReadOnlyDictionary<string, HaulDef> hauls,
        int mapWidth, int mapHeight,
        IReadOnlyList<ItemInstance> existingOffers,
        IReadOnlyList<ItemInstance> playerHauls,
        Random rng)
    {
        var cap = isLeaf ? 1 : 2;
        var needed = cap - existingOffers.Count;
        if (needed <= 0 || candidates.Count == 0)
            return [];

        var excluded = new HashSet<string>(
            existingOffers.Where(o => o.HaulDefId != null).Select(o => o.HaulDefId!));
        foreach (var h in playerHauls.Where(h => h.HaulDefId != null))
            excluded.Add(h.HaulDefId!);

        var origin = originBiome.ToString().ToLowerInvariant();
        var result = new List<ItemInstance>();
        var shuffled = candidates.OrderBy(_ => rng.Next()).ToList();

        foreach (var dest in shuffled)
        {
            if (result.Count >= needed) break;

            var destBiome = dest.Biome.ToString().ToLowerInvariant();
            var matching = hauls.Values
                .Where(h => h.OriginBiome == origin && h.DestBiome == destBiome && !excluded.Contains(h.Id))
                .ToList();

            if (matching.Count == 0) continue;

            var def = matching[rng.Next(matching.Count)];
            var payout = (Math.Abs(x - dest.X) + Math.Abs(y - dest.Y)) * 3;
            var hint = BuildHint(dest, mapWidth, mapHeight);

            result.Add(new ItemInstance("haul", def.Name)
            {
                HaulOfferId = Guid.NewGuid().ToString("N"),
                HaulDefId = def.Id,
                DestinationSettlementId = dest.SettlementId,
                DestinationName = dest.Name,
                DestinationHint = hint,
                Payout = payout,
                Description = def.OriginFlavor
            });

            excluded.Add(def.Id);
        }

        return result;
    }

    public static string BuildHint(HaulDestination dest, int mapWidth, int mapHeight)
    {
        var col = Math.Clamp(dest.X * 3 / mapWidth, 0, 2);
        var row = Math.Clamp(dest.Y * 3 / mapHeight, 0, 2);
        var sector = SectorNames[row * 3 + col];
        var biome = dest.Biome.ToString().ToLowerInvariant();
        return $"A {biome} settlement in the {sector}";
    }
}
