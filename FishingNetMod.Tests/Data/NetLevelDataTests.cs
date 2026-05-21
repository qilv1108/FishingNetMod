using FishingNetMod.Data;
using Xunit;

namespace FishingNetMod.Tests.Data;

public sealed class NetLevelDataTests
{
    [Theory]
    [InlineData("copper", NetLevel.Copper, "Copper Fishing Net", 2, 3, 10)]
    [InlineData("iron", NetLevel.Iron, "Iron Fishing Net", 3, 4, 8)]
    [InlineData("gold", NetLevel.Gold, "Gold Fishing Net", 4, 5, 6)]
    [InlineData("iridium", NetLevel.Iridium, "Iridium Fishing Net", 5, 7, 4)]
    public void TryParseReturnsExpectedLevelData(string input, NetLevel expectedLevel, string expectedName, int expectedMin, int expectedMax, int expectedStamina)
    {
        bool parsed = NetLevelData.TryParse(input, out NetLevelData? data);

        Assert.True(parsed);
        Assert.NotNull(data);
        Assert.Equal(expectedLevel, data.Level);
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
}
