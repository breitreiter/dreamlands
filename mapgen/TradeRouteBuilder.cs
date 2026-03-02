using Dreamlands.Map;
using Dreamlands.Rules;

namespace MapGen;

public static class TradeRouteBuilder
{
    public static void Build(Map map)
    {
        var city = map.StartingCity
            ?? throw new InvalidOperationException("StartingCity must be set before building trade routes");

        var settlements = map.AllNodes()
            .Where(n => n.Poi?.Kind == PoiKind.Settlement)
            .ToList();

        // Sort farthest from city first
        settlements.Sort((a, b) => Manhattan(b, city).CompareTo(Manhattan(a, city)));

        var childCount = new Dictionary<Node, int>();
        foreach (var s in settlements)
            childCount[s] = 0;

        foreach (var settlement in settlements)
        {
            if (settlement == city)
                continue;

            var distToCity = Manhattan(settlement, city);

            // Find nearest settlement that is closer to city, with hub attractiveness bias
            Node? bestParent = null;
            double bestScore = double.MaxValue;

            foreach (var candidate in settlements)
            {
                if (candidate == settlement)
                    continue;

                var candidateDistToCity = Manhattan(candidate, city);
                if (candidateDistToCity >= distToCity)
                    continue;

                var distance = Manhattan(settlement, candidate);
                var hubDiscount = 1.0 - 0.1 * Math.Min(childCount[candidate], 3);
                var score = distance * hubDiscount;

                if (score < bestScore)
                {
                    bestScore = score;
                    bestParent = candidate;
                }
            }

            if (bestParent != null)
            {
                map.TradeEdges.Add((settlement, bestParent));
                childCount[bestParent]++;
            }
        }

        Console.Error.WriteLine($"  Trade routes: {map.TradeEdges.Count} edges connecting {settlements.Count} settlements");
    }

    static int Manhattan(Node a, Node b) =>
        Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
}
