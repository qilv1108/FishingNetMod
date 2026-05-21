using FishingNetMod.Data;
using FishingNetMod.Mechanics;
using Microsoft.Xna.Framework;
using Xunit;

namespace FishingNetMod.Tests.Mechanics;

public sealed class PassiveNetManagerTests
{
    [Fact]
    public void PassiveNetDataStoresOwnerLocationLevelAndHarvest()
    {
        var fish = new PassiveNetHarvestData("(O)128", 2);
        var data = new PassiveNetData(1234L, "Beach", new Vector2(10, 20), NetLevel.Copper, new List<PassiveNetHarvestData> { fish });

        Assert.Equal(1234L, data.OwnerId);
        Assert.Equal("Beach", data.LocationName);
        Assert.Equal(new Vector2(10, 20), data.Tile);
        Assert.Equal(NetLevel.Copper, data.Level);
        Assert.Single(data.Harvest);
        Assert.Equal("(O)128", data.Harvest[0].QualifiedItemId);
        Assert.Equal(2, data.Harvest[0].Stack);
    }

    [Fact]
    public void TryAddRejectsSecondNetForSameOwner()
    {
        var manager = new PassiveNetManager();
        var first = new PassiveNetData(1234L, "Beach", new Vector2(10, 20), NetLevel.Copper, new List<PassiveNetHarvestData>());
        var second = new PassiveNetData(1234L, "Town", new Vector2(30, 40), NetLevel.Iron, new List<PassiveNetHarvestData>());

        Assert.True(manager.TryAdd(first, out string? firstError));
        Assert.Null(firstError);
        Assert.False(manager.TryAdd(second, out string? secondError));
        Assert.Equal("你已经放置了一个渔网。", secondError);
    }

    [Fact]
    public void TryAddRejectsOccupiedTile()
    {
        var manager = new PassiveNetManager();
        var first = new PassiveNetData(1234L, "Beach", new Vector2(10, 20), NetLevel.Copper, new List<PassiveNetHarvestData>());
        var second = new PassiveNetData(5678L, "Beach", new Vector2(10, 20), NetLevel.Iron, new List<PassiveNetHarvestData>());

        Assert.True(manager.TryAdd(first, out string? firstError));
        Assert.Null(firstError);
        Assert.False(manager.TryAdd(second, out string? secondError));
        Assert.Equal("这里已经有渔网了。", secondError);
    }

    [Theory]
    [InlineData(NetLevel.Copper, 1, 1)]
    [InlineData(NetLevel.Iron, 1, 2)]
    [InlineData(NetLevel.Gold, 1, 2)]
    [InlineData(NetLevel.Iridium, 2, 2)]
    public void GetDailyProductionRangeMatchesDesign(NetLevel level, int expectedMin, int expectedMax)
    {
        var range = PassiveNetManager.GetDailyProductionRange(level);

        Assert.Equal(expectedMin, range.Min);
        Assert.Equal(expectedMax, range.Max);
    }
}
