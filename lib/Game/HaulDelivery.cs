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
        var mercantile = player.Skills.GetValueOrDefault(Skill.Mercantile);
        var bonusRate = balance?.Trade.MercantileHaulBonusPerPoint ?? 0.0;

        var results = new List<DeliveryResult>();
        for (int i = player.Pack.Count - 1; i >= 0; i--)
        {
            var item = player.Pack[i];
            if (item.HaulDefId != null && item.DestinationSettlementId == settlementId)
            {
                player.Pack.RemoveAt(i);
                var basePayout = item.Payout ?? 0;
                var payout = (int)Math.Round(basePayout * (1 + mercantile * bonusRate));
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
