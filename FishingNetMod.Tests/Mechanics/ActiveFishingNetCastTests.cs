using FishingNetMod.Data;
using FishingNetMod.Mechanics;
using StardewValley;
using Xunit;

namespace FishingNetMod.Tests.Mechanics;

public sealed class ActiveFishingNetCastTests
{
    [Fact]
    public void EmptyCastUsesNoFishMessage()
    {
        var cast = new ActiveFishingNetCast(NetLevelData.Get(NetLevel.Copper), Array.Empty<Item>(), Attempts: 3);

        Assert.Equal(0, cast.CaughtCount);
        Assert.Equal("没有捕到鱼。", cast.GetResultMessage());
    }

    [Fact]
    public void CastStoresNetDataCaughtItemsAndAttempts()
    {
        Item[] caughtItems = Array.Empty<Item>();
        NetLevelData data = NetLevelData.Get(NetLevel.Gold);

        var cast = new ActiveFishingNetCast(data, caughtItems, Attempts: 5);

        Assert.Same(data, cast.NetData);
        Assert.Same(caughtItems, cast.CaughtItems);
        Assert.Equal(5, cast.Attempts);
    }
}
