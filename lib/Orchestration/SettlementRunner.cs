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

        // Generate haul offers if below cap (exclude hauls player already carries)
        GenerateHauls(session, node.Poi.SettlementId, node.Terrain, settlement);

        // Stock storylet offers
        StockStorylets(session, node, settlement);

        var isChapterhouse = node == session.Map.StartingCity;
        var services = new List<string> { "market", "bank", isChapterhouse ? "chapterhouse" : "inn" };
        if (settlement.StoryletOffers.Count > 0)
            services.Add("notices");

        return new SettlementData(node.Poi.Name ?? node.Poi.SettlementId, tier, biome, size, services);
    }

    private static void StockStorylets(GameSession session, Node node, SettlementState state)
    {
        var max = session.Balance.Settlements.MaxStorylets;
        var restockDays = session.Balance.Settlements.StoryletRestockDays;

        // Prune offers for encounters the player has since used (non-recurring only)
        state.StoryletOffers.RemoveAll(id =>
        {
            var enc = session.Bundle.GetById(id);
            return enc != null && !enc.Recurring && session.Player.UsedEncounterIds.Contains(id);
        });

        // Determine how many slots are available
        var slotsAvailable = max;
        if (state.LastStoryletStockDay > 0 && state.StoryletOffers.Count < max)
        {
            var elapsed = session.Player.Day - state.LastStoryletStockDay;
            var regained = elapsed / restockDays;
            slotsAvailable = Math.Min(max, state.StoryletOffers.Count + Math.Max(regained, 0));
        }

        if (state.StoryletOffers.Count >= slotsAvailable) return;

        var eligible = EncounterSelection.GetEligibleStorylets(session, node, state.StoryletOffers);
        var toAdd = slotsAvailable - state.StoryletOffers.Count;
        var candidates = eligible.ToList();

        while (toAdd > 0 && candidates.Count > 0)
        {
            var idx = session.Rng.Next(candidates.Count);
            state.StoryletOffers.Add(candidates[idx].Id);
            candidates.RemoveAt(idx);
            toAdd--;
        }

        state.LastStoryletStockDay = session.Player.Day;
    }

    private static void GenerateHauls(GameSession session, string settlementId, Terrain biome, SettlementState state)
    {
        var graph = session.Graph;
        if (graph == null || !graph.Settlements.TryGetValue(settlementId, out var info)) return;

        var isLeaf = info.ChildIds.Count == 0 && info.ParentId != null;
        var restockDays = isLeaf
            ? session.Balance.Settlements.HaulRestockDaysLeaf
            : session.Balance.Settlements.HaulRestockDaysHub;

        // First visit: populate fully and stamp the day
        if (state.LastHaulStockDay == 0)
        {
            FillHaulSlots(session, info, biome, isLeaf, state);
            state.LastHaulStockDay = session.Player.Day;
            return;
        }

        // Restock: clear old offers, regenerate based on elapsed ticks
        var elapsed = session.Player.Day - state.LastHaulStockDay;
        if (elapsed < restockDays) return;

        var ticks = elapsed / restockDays;
        var cap = isLeaf ? 1 : 2;
        var slots = Math.Min(ticks, cap);

        state.HaulOffers.Clear();
        FillHaulSlots(session, info, biome, isLeaf, state, slots);
        state.LastHaulStockDay = session.Player.Day;
    }

    private static void FillHaulSlots(
        GameSession session, SettlementInfo info,
        Terrain biome, bool isLeaf, SettlementState state, int? maxSlots = null)
    {
        // Weighted hop distance: 25% 1-hop, 60% 2-hop, 15% 3-hop, with fallback
        var roll = session.Rng.NextDouble();
        var preferredHop = roll < 0.25 ? 1 : roll < 0.85 ? 2 : 3;
        var candidates = session.Graph!.GetSettlementsAtHop(info.Id, preferredHop);
        if (candidates.Count == 0)
        {
            foreach (var fallback in new[] { 1, 2, 3 }.Where(h => h != preferredHop))
            {
                candidates = session.Graph.GetSettlementsAtHop(info.Id, fallback);
                if (candidates.Count > 0) break;
            }
        }
        if (candidates.Count == 0) return;

        var visited = session.Player.VisitedNodes;
        var currentDepth = info.Depth;
        var downward = candidates.Where(s => s.Depth > currentDepth)
            .Select(s => new HaulGeneration.HaulDestination(
                s.Id, s.Name, s.Biome, s.X, s.Y, s.Depth,
                visited.Contains(PlayerState.EncodePosition(s.X, s.Y))))
            .ToList();
        var upward = candidates.Where(s => s.Depth <= currentDepth)
            .Select(s => new HaulGeneration.HaulDestination(
                s.Id, s.Name, s.Biome, s.X, s.Y, s.Depth,
                visited.Contains(PlayerState.EncodePosition(s.X, s.Y))))
            .ToList();

        var playerHauls = session.Player.Pack.Where(i => i.HaulDefId != null).ToList();

        var settlementBalance = session.Balance.Settlements;
        var newOffers = HaulGeneration.Generate(
            info.X, info.Y, info.Name, biome, isLeaf,
            downward, session.Balance.Hauls,
            state.HaulOffers, playerHauls, session.Rng, settlementBalance, maxSlots);
        state.HaulOffers.AddRange(newOffers);

        if (upward.Count > 0)
        {
            var moreOffers = HaulGeneration.Generate(
                info.X, info.Y, info.Name, biome, isLeaf,
                upward, session.Balance.Hauls,
                state.HaulOffers, playerHauls, session.Rng, settlementBalance, maxSlots);
            state.HaulOffers.AddRange(moreOffers);
        }
    }
}
