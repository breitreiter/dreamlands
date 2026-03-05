using Dreamlands.Game;
using Dreamlands.Map;
using Dreamlands.Rules;

namespace Dreamlands.Orchestration;

public record SettlementData(string Name, int Tier, string Biome, SettlementSize Size, List<string> Services);

public static class SettlementRunner
{
    /// <summary>
    /// Returns settlement data for the player's current node, or null if not at a settlement.
    /// Initializes settlement state on first visit and restocks for elapsed days.
    /// Does NOT change session mode.
    /// </summary>
    public static SettlementData? EnsureSettlement(GameSession session)
    {
        var node = session.CurrentNode;
        if (node.Poi?.Kind != PoiKind.Settlement || node.Poi.SettlementId == null)
            return null;

        var tier = node.Region?.Tier ?? 1;
        var biome = node.Region?.Terrain.ToString().ToLowerInvariant() ?? "plains";
        var size = node.Poi.Size ?? SettlementSize.Camp;

        // Initialize settlement state on first visit
        if (!session.Player.Settlements.ContainsKey(node.Poi.SettlementId))
        {
            var seed = session.Player.Seed ^ node.Poi.SettlementId.GetHashCode();
            var rng = new Random(seed);
            var state = Market.InitializeSettlement(
                node.Poi.SettlementId, biome, tier, size,
                session.Player, session.Balance, rng);
            session.Player.Settlements[node.Poi.SettlementId] = state;
        }

        // Restock for elapsed days
        var settlement = session.Player.Settlements[node.Poi.SettlementId];
        Market.Restock(settlement, size, session.Player.Day, session.Balance, session.Rng);

        // Generate haul offers if below cap
        GenerateHauls(session, node.Poi.SettlementId, node.Terrain, settlement);

        var isChapterhouse = node == session.Map.StartingCity;
        var services = new List<string> { "market", "bank", isChapterhouse ? "chapterhouse" : "inn" };

        return new SettlementData(node.Poi.Name ?? node.Poi.SettlementId, tier, biome, size, services);
    }

    private static void GenerateHauls(GameSession session, string settlementId, Terrain biome, SettlementState state)
    {
        var graph = session.Graph;
        if (graph == null || !graph.Settlements.TryGetValue(settlementId, out var info)) return;

        var isLeaf = info.ChildIds.Count == 0 && info.ParentId != null;

        // Try 2 hops, fallback to 1, then 3
        var candidates = graph.GetSettlementsAtHop(settlementId, 2);
        if (candidates.Count == 0) candidates = graph.GetSettlementsAtHop(settlementId, 1);
        if (candidates.Count == 0) candidates = graph.GetSettlementsAtHop(settlementId, 3);
        if (candidates.Count == 0) return;

        var destinations = candidates
            .Select(s => new HaulGeneration.HaulDestination(s.Id, s.Name, s.Biome, s.X, s.Y))
            .ToList();

        var newOffers = HaulGeneration.Generate(
            info.X, info.Y, biome, isLeaf,
            destinations, session.Balance.Hauls,
            session.Map.Width, session.Map.Height,
            state.HaulOffers, session.Rng);

        state.HaulOffers.AddRange(newOffers);
    }
}
