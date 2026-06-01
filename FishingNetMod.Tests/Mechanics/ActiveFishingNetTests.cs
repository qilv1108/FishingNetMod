using FishingNetMod.Data;
using FishingNetMod.Mechanics;
using StardewValley;
using Xunit;

namespace FishingNetMod.Tests.Mechanics;

public sealed class ActiveFishingNetTests
{
    [Theory]
    [InlineData(NetLevel.Copper, 2, 3)]
    [InlineData(NetLevel.Iron, 3, 4)]
    [InlineData(NetLevel.Gold, 4, 5)]
    [InlineData(NetLevel.Iridium, 5, 7)]
    public void NetLevelDataDefinesActiveFishingAttemptRange(NetLevel level, int expectedMin, int expectedMax)
    {
        NetLevelData data = NetLevelData.Get(level);

        Assert.Equal(expectedMin, data.MinCatch);
        Assert.Equal(expectedMax, data.MaxCatch);
    }

    [Fact]
    public void ActiveFishingNetExposesPrepareAndCompleteFlow()
    {
        Type type = typeof(ActiveFishingNet);

        Assert.NotNull(type.GetMethod(
            "TryUse",
            new[] { typeof(Farmer), typeof(GameLocation), typeof(ActiveFishingNetCast).MakeByRefType() }));
        Assert.NotNull(type.GetMethod(
            "CompleteCatch",
            new[] { typeof(Farmer), typeof(GameLocation), typeof(ActiveFishingNetCast) }));
    }
}
