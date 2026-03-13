using Dreamlands.Rules;

namespace Dreamlands.Game;

public static class HaulGeneration
{
    public record HaulDestination(string SettlementId, string Name, Terrain Biome, int X, int Y);

    private const int TilesPerDay = 5;

    private static readonly string[] Directions =
    {
        "east", "northeast", "north", "northwest",
        "west", "southwest", "south", "southeast"
    };

    public static List<ItemInstance> Generate(
        int x, int y,
        string originName,
        Terrain originBiome,
        bool isLeaf,
        IReadOnlyList<HaulDestination> candidates,
        IReadOnlyDictionary<string, HaulDef> hauls,
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
                .Where(h => !h.IsGeneric && h.OriginBiome == origin && h.DestBiome == destBiome && !excluded.Contains(h.Id))
                .ToList();

            if (matching.Count == 0)
                matching = hauls.Values
                    .Where(h => h.IsGeneric && !excluded.Contains(h.Id))
                    .ToList();

            if (matching.Count == 0) continue;

            var def = matching[rng.Next(matching.Count)];
            var payout = (Math.Abs(x - dest.X) + Math.Abs(y - dest.Y)) * 3;
            var hint = BuildHint(x, y, originName, dest);

            result.Add(new ItemInstance("haul", def.Name)
            {
                HaulOfferId = Guid.NewGuid().ToString("N"),
                HaulDefId = def.Id,
                IsGeneric = def.IsGeneric,
                DestinationSettlementId = dest.SettlementId,
                DestinationName = dest.Name,
                DestinationHint = hint,
                DestinationX = dest.X,
                DestinationY = dest.Y,
                Payout = payout,
                Description = def.OriginFlavor
            });

            excluded.Add(def.Id);
        }

        return result;
    }

    public static string BuildRelativeHint(int playerX, int playerY, int destX, int destY)
    {
        var dx = destX - playerX;
        var dy = destY - playerY;
        var manhattan = Math.Abs(dx) + Math.Abs(dy);
        var days = Math.Max(1, (int)Math.Round((double)manhattan / TilesPerDay));

        if (manhattan == 0)
            return "Right here";

        var angle = Math.Atan2(-dy, dx);
        var index = (int)Math.Round(angle / (Math.PI / 4));
        if (index < 0) index += 8;
        var direction = Directions[index % 8];

        var dayLabel = days == 1 ? "about 1 day" : $"about {days} days";
        return $"{dayLabel} {direction} of here";
    }

    public static string BuildHint(int originX, int originY, string originName, HaulDestination dest)
    {
        var dx = dest.X - originX;
        var dy = dest.Y - originY;
        var manhattan = Math.Abs(dx) + Math.Abs(dy);
        var days = Math.Max(1, (int)Math.Round((double)manhattan / TilesPerDay));
        var biome = dest.Biome.ToString().ToLowerInvariant();

        if (manhattan == 0)
            return $"A {biome} settlement at {originName}";

        // 8-way direction from angle: atan2 gives radians, divide into 8 sectors
        var angle = Math.Atan2(-dy, dx); // -dy because y increases southward on the grid
        var index = (int)Math.Round(angle / (Math.PI / 4));
        if (index < 0) index += 8;
        var direction = Directions[index % 8];

        var dayLabel = days == 1 ? "1 day" : $"{days} days";
        return $"A {biome} settlement {dayLabel} {direction} of {originName}";
    }
}
