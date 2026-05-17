using Dreamlands.Rules;

namespace Dreamlands.Game;

public static class HaulDelivery
{
    public record DeliveryResult(string HaulDefId, string DisplayName, int Payout, string? DeliveryFlavor);

    public static List<DeliveryResult> Deliver(
        PlayerState player,
        string settlementId,
        IReadOnlyDictionary<string, HaulDef> hauls,
        Random rng,
        BalanceData? balance = null)
    {
        var negotiationTier = (int)player.Skills.GetValueOrDefault(Skill.Negotiation);
        // Untrained=0 → 1.0×, Trained=1 → 1.2×, Expert=2 → 1.4×
        var payoutMultiplier = 1.0 + 0.2 * negotiationTier;

        var results = new List<DeliveryResult>();
        for (int i = player.Pack.Count - 1; i >= 0; i--)
        {
            var item = player.Pack[i];
            if (item.HaulDefId != null && item.DestinationSettlementId == settlementId)
            {
                player.Pack.RemoveAt(i);
                var basePayout = item.Payout ?? 0;
                var payout = (int)Math.Round(basePayout * payoutMultiplier);
                player.Gold += payout;

                string? deliveryFlavor = null;
                if (hauls.TryGetValue(item.HaulDefId, out var def))
                    deliveryFlavor = def.IsGeneric
                        ? HaulDef.GenericDeliveryFlavors[rng.Next(HaulDef.GenericDeliveryFlavors.Length)]
                        : def.DeliveryFlavor;

                results.Add(new DeliveryResult(item.HaulDefId, item.DisplayName, payout, deliveryFlavor));
            }
        }
        return results;
    }
}
