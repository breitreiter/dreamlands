using Dreamlands.Game;
using Dreamlands.Rules;

namespace Dreamlands.Game.Tests;

public class BankTests
{
    static readonly BalanceData Balance = BalanceData.Default;

    static PlayerState Fresh() => PlayerState.NewGame("test", 99, Balance);

    static SettlementState MakeSettlement() => new() { Biome = "plains" };

    [Fact]
    public void Deposit_FromPack_MovesItemToBank()
    {
        var state = Fresh();
        var settlement = MakeSettlement();
        state.Pack.Add(new ItemInstance("bodkin", "Bodkin"));

        var error = Bank.Deposit(state, "bodkin", "pack", settlement, Balance);

        Assert.Null(error);
        Assert.Empty(state.Pack.Where(i => i.DefId == "bodkin"));
        Assert.Single(settlement.Bank);
        Assert.Equal("bodkin", settlement.Bank[0].DefId);
    }

    [Fact]
    public void Deposit_FromHaversack_MovesItemToBank()
    {
        var state = Fresh();
        var settlement = MakeSettlement();
        state.Haversack.Add(new ItemInstance("bandages", "Bandages"));

        var error = Bank.Deposit(state, "bandages", "haversack", settlement, Balance);

        Assert.Null(error);
        Assert.Empty(state.Haversack);
        Assert.Single(settlement.Bank);
    }

    [Fact]
    public void Deposit_Rations_Rejected()
    {
        var state = Fresh();
        var settlement = MakeSettlement();
        state.Haversack.Add(new ItemInstance("food_ration", "Jerky"));

        var error = Bank.Deposit(state, "food_ration", "haversack", settlement, Balance);

        Assert.NotNull(error);
        Assert.Single(state.Haversack);
        Assert.Empty(settlement.Bank);
    }

    [Fact]
    public void Deposit_FromWeaponSlot_UnequipsAndBanks()
    {
        var state = Fresh();
        var settlement = MakeSettlement();
        state.Equipment.Weapon = new ItemInstance("bodkin", "Bodkin");

        var error = Bank.Deposit(state, "bodkin", "weapon", settlement, Balance);

        Assert.Null(error);
        Assert.Null(state.Equipment.Weapon);
        Assert.Single(settlement.Bank);
    }

    [Fact]
    public void Deposit_FromArmorSlot_UnequipsAndBanks()
    {
        var state = Fresh();
        var settlement = MakeSettlement();
        state.Equipment.Armor = new ItemInstance("leather_jerkin", "Leather Jerkin");

        var error = Bank.Deposit(state, "leather_jerkin", "armor", settlement, Balance);

        Assert.Null(error);
        Assert.Null(state.Equipment.Armor);
        Assert.Single(settlement.Bank);
    }

    [Fact]
    public void Deposit_FromBootsSlot_UnequipsAndBanks()
    {
        var state = Fresh();
        var settlement = MakeSettlement();
        state.Equipment.Boots = new ItemInstance("walking_boots", "Walking Boots");

        var error = Bank.Deposit(state, "walking_boots", "boots", settlement, Balance);

        Assert.Null(error);
        Assert.Null(state.Equipment.Boots);
        Assert.Single(settlement.Bank);
    }

    [Fact]
    public void Deposit_BankFull_ReturnsError()
    {
        var state = Fresh();
        var settlement = MakeSettlement();
        for (int i = 0; i < Balance.Settlements.BankCapacity; i++)
            settlement.Bank.Add(new ItemInstance($"item_{i}", $"Item {i}"));

        state.Pack.Add(new ItemInstance("bodkin", "Bodkin"));
        var error = Bank.Deposit(state, "bodkin", "pack", settlement, Balance);

        Assert.Equal("Bank is full", error);
        Assert.Single(state.Pack); // item not removed
    }

    [Fact]
    public void Deposit_ItemNotInPack_ReturnsError()
    {
        var state = Fresh();
        var settlement = MakeSettlement();

        var error = Bank.Deposit(state, "bodkin", "pack", settlement, Balance);

        Assert.Equal("Item not found in pack", error);
    }

    [Fact]
    public void Deposit_WrongWeaponEquipped_ReturnsError()
    {
        var state = Fresh();
        var settlement = MakeSettlement();
        state.Equipment.Weapon = new ItemInstance("dagger", "Dagger");

        var error = Bank.Deposit(state, "bodkin", "weapon", settlement, Balance);

        Assert.Equal("Item not equipped in weapon slot", error);
        Assert.NotNull(state.Equipment.Weapon); // weapon untouched
    }

    [Fact]
    public void Deposit_InvalidSource_ReturnsError()
    {
        var state = Fresh();
        var settlement = MakeSettlement();

        var error = Bank.Deposit(state, "bodkin", "backpack", settlement, Balance);

        Assert.StartsWith("Invalid source", error);
    }

    [Fact]
    public void Withdraw_PackItem_GoesToPack()
    {
        var state = Fresh();
        var settlement = MakeSettlement();
        settlement.Bank.Add(new ItemInstance("bodkin", "Bodkin"));

        var error = Bank.Withdraw(state, 0, settlement, Balance);

        Assert.Null(error);
        Assert.Empty(settlement.Bank);
        Assert.Contains(state.Pack, i => i.DefId == "bodkin");
    }

    [Fact]
    public void Withdraw_Consumable_GoesToHaversack()
    {
        var state = Fresh();
        var settlement = MakeSettlement();
        settlement.Bank.Add(new ItemInstance("food_ration", "Rations"));

        var error = Bank.Withdraw(state, 0, settlement, Balance);

        Assert.Null(error);
        Assert.Empty(settlement.Bank);
        Assert.Contains(state.Haversack, i => i.DefId == "food_ration");
    }

    [Fact]
    public void Withdraw_PackFull_ReturnsError()
    {
        var state = Fresh();
        state.PackCapacity = 0;
        var settlement = MakeSettlement();
        settlement.Bank.Add(new ItemInstance("bodkin", "Bodkin"));

        var error = Bank.Withdraw(state, 0, settlement, Balance);

        Assert.Equal("Pack is full", error);
        Assert.Single(settlement.Bank); // item stays in bank
    }

    [Fact]
    public void Withdraw_HaversackFull_ReturnsError()
    {
        var state = Fresh();
        state.HaversackCapacity = 0;
        var settlement = MakeSettlement();
        settlement.Bank.Add(new ItemInstance("food_ration", "Rations"));

        var error = Bank.Withdraw(state, 0, settlement, Balance);

        Assert.Equal("Haversack is full", error);
        Assert.Single(settlement.Bank);
    }

    [Fact]
    public void Withdraw_InvalidIndex_ReturnsError()
    {
        var state = Fresh();
        var settlement = MakeSettlement();

        Assert.Equal("Invalid bank slot", Bank.Withdraw(state, 0, settlement, Balance));
        Assert.Equal("Invalid bank slot", Bank.Withdraw(state, -1, settlement, Balance));
    }

    [Fact]
    public void Deposit_ThenWithdraw_RoundTrips()
    {
        var state = Fresh();
        var settlement = MakeSettlement();
        var item = new ItemInstance("bodkin", "Bodkin");
        state.Pack.Add(item);

        Bank.Deposit(state, "bodkin", "pack", settlement, Balance);
        Assert.Empty(state.Pack.Where(i => i.DefId == "bodkin"));

        Bank.Withdraw(state, 0, settlement, Balance);
        Assert.Contains(state.Pack, i => i.DefId == "bodkin");
        Assert.Empty(settlement.Bank);
    }

    [Fact]
    public void Banks_ArePerSettlement()
    {
        var state = Fresh();
        var settlement1 = MakeSettlement();
        var settlement2 = MakeSettlement();

        state.Pack.Add(new ItemInstance("bodkin", "Bodkin"));
        Bank.Deposit(state, "bodkin", "pack", settlement1, Balance);

        Assert.Single(settlement1.Bank);
        Assert.Empty(settlement2.Bank);
    }

    [Fact]
    public void BankCapacity_Is10()
    {
        Assert.Equal(10, Balance.Settlements.BankCapacity);
    }
}
