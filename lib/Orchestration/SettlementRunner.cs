using Dreamlands.Game;
using Dreamlands.Map;
using Dreamlands.Rules;

namespace Dreamlands.Orchestration;

public record SettlementData(string Name, int Tier, string Biome, SettlementSize Size, List<string> Services);

public static class SettlementRunner
{
    public static SettlementData? Enter(GameSession session)
    {
        var node = session.CurrentNode;
        if (node.Poi?.Kind != PoiKind.Settlement || node.Poi.Name == null)
            return null;

        session.Mode = SessionMode.AtSettlement;
        session.Player.CurrentSettlementId = node.Poi.Name;

        var tier = node.Region?.Tier ?? 1;
        var biome = node.Region?.Terrain.ToString().ToLowerInvariant() ?? "plains";
        var size = node.Poi.Size ?? SettlementSize.Camp;

        // Initialize settlement state on first visit
        if (!session.Player.Settlements.ContainsKey(node.Poi.Name))
        {
            var seed = session.Player.Seed ^ node.Poi.Name.GetHashCode();
            var rng = new Random(seed);
            var state = Market.InitializeSettlement(
                node.Poi.Name, biome, tier, size,
                session.Player, session.Balance, rng);
            session.Player.Settlements[node.Poi.Name] = state;
        }

        // Restock for elapsed days
        var settlement = session.Player.Settlements[node.Poi.Name];
        Market.Restock(settlement, size, session.Player.Day, session.Balance, session.Rng);

        var services = new List<string> { "market", "inn" };
        if (node == session.Map.StartingCity)
            services.Add("chapterhouse");

        return new SettlementData(node.Poi.Name, tier, biome, size, services);
    }

    public static void Leave(GameSession session)
    {
        session.Mode = SessionMode.Exploring;
        session.Player.CurrentSettlementId = null;
    }
}
