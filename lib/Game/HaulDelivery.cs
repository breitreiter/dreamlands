using Dreamlands.Rules;

namespace Dreamlands.Game;

public static class HaulDelivery
{
    public record DeliveryResult(string HaulDefId, string DisplayName, int Payout, string? DeliveryFlavor);

    public static List<DeliveryResult> Deliver(
        PlayerState player,
        string settlementId,
        IReadOnlyDictionary<string, HaulDef> hauls)
    {
        var results = new List<DeliveryResult>();
        for (int i = player.Pack.Count - 1; i >= 0; i--)
        {
            var item = player.Pack[i];
            if (item.HaulDefId != null && item.DestinationSettlementId == settlementId)
            {
                player.Pack.RemoveAt(i);
                var payout = item.Payout ?? 0;
                player.Gold += payout;
                var deliveryFlavor = hauls.TryGetValue(item.HaulDefId, out var def) ? def.DeliveryFlavor : null;
                results.Add(new DeliveryResult(item.HaulDefId, item.DisplayName, payout, deliveryFlavor));
            }
        }
        return results;
    }
}
