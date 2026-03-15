using Dreamlands.Game;
using Dreamlands.Rules;

namespace Dreamlands.Game.Tests;

public class HaulDeliveryTests
{
    static readonly IReadOnlyDictionary<string, HaulDef> TestHauls = new Dictionary<string, HaulDef>
    {
        ["test_haul"] = new()
        {
            Id = "test_haul",
            Name = "Test Haul",
            OriginBiome = "plains",
            DestBiome = "mountains",
            OriginFlavor = "Origin story",
            DeliveryFlavor = "Delivered safely",
        },
        ["other_haul"] = new()
        {
            Id = "other_haul",
            Name = "Other Haul",
            OriginBiome = "forest",
            DestBiome = "swamp",
            OriginFlavor = "Another origin",
            DeliveryFlavor = "Another delivery",
        },
    };

    static PlayerState MakePlayer() => PlayerState.NewGame("test", 42, BalanceData.Default);

    [Fact]
    public void Delivers_matching_haul_removes_from_pack_and_adds_gold()
    {
        var player = MakePlayer();
        player.Gold = 10;
        player.Pack.Add(new ItemInstance("haul_item", "Ledger Book")
        {
            HaulDefId = "test_haul",
            DestinationSettlementId = "town_a",
            Payout = 25,
        });

        var results = HaulDelivery.Deliver(player, "town_a", TestHauls, new Random(42));

        Assert.Single(results);
        Assert.Equal("test_haul", results[0].HaulDefId);
        Assert.Equal("Ledger Book", results[0].DisplayName);
        Assert.Equal(25, results[0].Payout);
        Assert.Equal("Delivered safely", results[0].DeliveryFlavor);
        Assert.Equal(35, player.Gold);
        Assert.Empty(player.Pack);
    }

    [Fact]
    public void Ignores_hauls_for_other_settlements()
    {
        var player = MakePlayer();
        player.Pack.Add(new ItemInstance("haul_item", "Ledger Book")
        {
            HaulDefId = "test_haul",
            DestinationSettlementId = "town_b",
            Payout = 25,
        });

        var results = HaulDelivery.Deliver(player, "town_a", TestHauls, new Random(42));

        Assert.Empty(results);
        Assert.Single(player.Pack);
    }

    [Fact]
    public void Ignores_non_haul_items()
    {
        var player = MakePlayer();
        player.Pack.Add(new ItemInstance("sword", "Iron Sword"));

        var results = HaulDelivery.Deliver(player, "town_a", TestHauls, new Random(42));

        Assert.Empty(results);
        Assert.Single(player.Pack);
    }

    [Fact]
    public void Multiple_deliveries_in_one_arrival()
    {
        var player = MakePlayer();
        player.Gold = 0;
        player.Pack.Add(new ItemInstance("h1", "Haul One")
        {
            HaulDefId = "test_haul",
            DestinationSettlementId = "town_a",
            Payout = 10,
        });
        player.Pack.Add(new ItemInstance("normal", "Normal Item"));
        player.Pack.Add(new ItemInstance("h2", "Haul Two")
        {
            HaulDefId = "other_haul",
            DestinationSettlementId = "town_a",
            Payout = 15,
        });

        var results = HaulDelivery.Deliver(player, "town_a", TestHauls, new Random(42));

        Assert.Equal(2, results.Count);
        Assert.Equal(25, player.Gold);
        Assert.Single(player.Pack); // only the normal item remains
        Assert.Equal("Normal Item", player.Pack[0].DisplayName);
    }

    [Fact]
    public void Empty_pack_returns_empty_list()
    {
        var player = MakePlayer();

        var results = HaulDelivery.Deliver(player, "town_a", TestHauls, new Random(42));

        Assert.Empty(results);
    }

    [Fact]
    public void Looks_up_delivery_flavor_from_catalog()
    {
        var player = MakePlayer();
        player.Pack.Add(new ItemInstance("h", "A Haul")
        {
            HaulDefId = "other_haul",
            DestinationSettlementId = "dest",
            Payout = 5,
        });

        var results = HaulDelivery.Deliver(player, "dest", TestHauls, new Random(42));

        Assert.Single(results);
        Assert.Equal("Another delivery", results[0].DeliveryFlavor);
    }

    [Fact]
    public void Generic_haul_delivery_returns_flavor_from_generic_pool()
    {
        var hauls = new Dictionary<string, HaulDef>
        {
            ["generic_sealed_crate"] = new()
            {
                Id = "generic_sealed_crate",
                Name = "Sealed Crate",
                IsGeneric = true,
                OriginFlavor = "A crate",
            },
        };

        var player = MakePlayer();
        player.Pack.Add(new ItemInstance("h", "Sealed Crate")
        {
            HaulDefId = "generic_sealed_crate",
            DestinationSettlementId = "dest",
            Payout = 10,
        });

        var results = HaulDelivery.Deliver(player, "dest", hauls, new Random(42));

        Assert.Single(results);
        Assert.NotNull(results[0].DeliveryFlavor);
        Assert.Contains(results[0].DeliveryFlavor, HaulDef.GenericDeliveryFlavors);
    }

    [Fact]
    public void Missing_haul_def_returns_null_flavor()
    {
        var player = MakePlayer();
        player.Pack.Add(new ItemInstance("h", "Unknown Haul")
        {
            HaulDefId = "nonexistent",
            DestinationSettlementId = "dest",
            Payout = 5,
        });

        var results = HaulDelivery.Deliver(player, "dest", TestHauls, new Random(42));

        Assert.Single(results);
        Assert.Null(results[0].DeliveryFlavor);
        Assert.Equal(5, results[0].Payout);
    }

    [Fact]
    public void Mercantile_bonus_increases_payout()
    {
        var balance = BalanceData.Default;
        var player = MakePlayer();
        player.Skills[Skill.Mercantile] = 3;
        player.Gold = 0;
        player.Pack.Add(new ItemInstance("h", "Haul")
        {
            HaulDefId = "test_haul",
            DestinationSettlementId = "town_a",
            Payout = 100,
        });

        var results = HaulDelivery.Deliver(player, "town_a", TestHauls, new Random(42), balance);

        Assert.Single(results);
        var expectedPayout = (int)Math.Round(100 * (1 + 3 * balance.Trade.MercantileHaulBonusPerPoint));
        Assert.Equal(expectedPayout, results[0].Payout);
        Assert.Equal(expectedPayout, player.Gold);
    }

    [Fact]
    public void No_mercantile_no_bonus()
    {
        var balance = BalanceData.Default;
        var player = MakePlayer();
        player.Gold = 0;
        player.Pack.Add(new ItemInstance("h", "Haul")
        {
            HaulDefId = "test_haul",
            DestinationSettlementId = "town_a",
            Payout = 100,
        });

        var results = HaulDelivery.Deliver(player, "town_a", TestHauls, new Random(42), balance);

        Assert.Single(results);
        Assert.Equal(100, results[0].Payout);
        Assert.Equal(100, player.Gold);
    }
}
