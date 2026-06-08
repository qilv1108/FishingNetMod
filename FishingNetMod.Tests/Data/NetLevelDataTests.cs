using FishingNetMod.Data;
using FishingNetMod.Items;
using Xunit;

namespace FishingNetMod.Tests.Data;

public sealed class NetLevelDataTests
{
    [Theory]
    [InlineData("copper", NetLevel.Copper, FishingNetIds.CopperNetItemId, "Copper Fishing Net", 2, 3, 10)]
    [InlineData("iron", NetLevel.Iron, FishingNetIds.IronNetItemId, "Silver Fishing Net", 3, 4, 8)]
    [InlineData("gold", NetLevel.Gold, FishingNetIds.GoldNetItemId, "Gold Fishing Net", 4, 5, 6)]
    [InlineData("iridium", NetLevel.Iridium, FishingNetIds.IridiumNetItemId, "Iridium Fishing Net", 5, 7, 4)]
    public void TryParseReturnsExpectedLevelData(string input, NetLevel expectedLevel, string expectedItemId, string expectedName, int expectedMin, int expectedMax, int expectedStamina)
    {
        bool parsed = NetLevelData.TryParse(input, out NetLevelData? data);

        Assert.True(parsed);
        Assert.NotNull(data);
        Assert.Equal(expectedLevel, data.Level);
        Assert.Equal(expectedItemId, data.ItemId);
        Assert.Equal(expectedName, data.DisplayName);
        Assert.Equal(expectedMin, data.MinCatch);
        Assert.Equal(expectedMax, data.MaxCatch);
        Assert.Equal(expectedStamina, data.StaminaCost);
    }

    [Fact]
    public void TryParseRejectsUnknownLevel()
    {
        bool parsed = NetLevelData.TryParse("diamond", out NetLevelData? data);

        Assert.False(parsed);
        Assert.Null(data);
    }

    [Fact]
    public void TryParseIgnoresCaseAndWhitespace()
    {
        bool parsed = NetLevelData.TryParse("  IRON  ", out NetLevelData? data);

        Assert.True(parsed);
        Assert.NotNull(data);
        Assert.Equal(NetLevel.Iron, data.Level);
    }

    [Fact]
    public void AllReturnsLevelsInUpgradeOrder()
    {
        NetLevel[] levels = NetLevelData.All.Select(data => data.Level).ToArray();

        Assert.Equal(new[] { NetLevel.Copper, NetLevel.Iron, NetLevel.Gold, NetLevel.Iridium }, levels);
    }

    [Theory]
    [InlineData(FishingNetIds.CopperNetItemId, NetLevel.Copper)]
    [InlineData(FishingNetIds.IronNetItemId, NetLevel.Iron)]
    [InlineData(FishingNetIds.GoldNetItemId, NetLevel.Gold)]
    [InlineData(FishingNetIds.IridiumNetItemId, NetLevel.Iridium)]
    public void TryParseItemIdReturnsExpectedLevel(string itemId, NetLevel expectedLevel)
    {
        bool parsed = NetLevelData.TryParseItemId(itemId, out NetLevelData? data);

        Assert.True(parsed);
        Assert.NotNull(data);
        Assert.Equal(expectedLevel, data.Level);
    }

    [Fact]
    public void TryParseItemIdAcceptsQualifiedObjectIds()
    {
        bool parsed = NetLevelData.TryParseItemId($"(O){FishingNetIds.GoldNetItemId}", out NetLevelData? data);

        Assert.True(parsed);
        Assert.NotNull(data);
        Assert.Equal(NetLevel.Gold, data.Level);
    }
}
