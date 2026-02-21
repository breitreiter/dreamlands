using Dreamlands.Map;

namespace Dreamlands.Orchestration;

public record SettlementData(string Name, int Tier, string Biome, List<string> Services);

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

        // MVP: market only. Other services are phase 4.
        var services = new List<string> { "market" };

        return new SettlementData(node.Poi.Name, tier, biome, services);
    }

    public static void Leave(GameSession session)
    {
        session.Mode = SessionMode.Exploring;
        session.Player.CurrentSettlementId = null;
    }
}
