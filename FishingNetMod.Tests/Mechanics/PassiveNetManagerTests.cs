using System.Reflection;
using System.Runtime.Serialization;
using FishingNetMod.Data;
using FishingNetMod.Mechanics;
using Microsoft.Xna.Framework;
using StardewValley;
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

    [Fact]
    public void TryGetHarvestableNetFindsNetByLocationAndTile()
    {
        var manager = new PassiveNetManager();
        var first = new PassiveNetData(1234L, "Beach", new Vector2(10, 20), NetLevel.Copper, new List<PassiveNetHarvestData>());
        manager.TryAdd(first, out _);

        bool found = manager.TryGetHarvestableNet("Beach", new Vector2(10, 20), out PassiveNetData? data);

        Assert.True(found);
        Assert.Same(first, data);
    }

    [Fact]
    public void TryGetHarvestableNetRejectsMissingNet()
    {
        var manager = new PassiveNetManager();

        bool found = manager.TryGetHarvestableNet("Beach", new Vector2(10, 20), out PassiveNetData? data);

        Assert.False(found);
        Assert.Null(data);
    }

    [Fact]
    public void TryHarvest_RejectsNonOwner()
    {
        var manager = new PassiveNetManager();
        var net = new PassiveNetData(
            OwnerId: 1234L,
            LocationName: "Beach",
            Tile: new Vector2(10, 20),
            Level: NetLevel.Copper,
            Harvest: new List<PassiveNetHarvestData>
            {
                new("(O)128", 1, 0)
            });
        manager.TryAdd(net, out _);

        // 使用 FormatterServices 绕过 Farmer/GameLocation 构造函数，用反射设置底层字段
        var otherPlayer = (Farmer)FormatterServices.GetUninitializedObject(typeof(Farmer));
        SetUniqueMultiplayerID(otherPlayer, 9999L);
        var location = (GameLocation)FormatterServices.GetUninitializedObject(typeof(GameLocation));
        SetLocationName(location, "Beach");

        bool result = manager.TryHarvest(otherPlayer, location, new Vector2(10, 20), out string? error);

        Assert.False(result);
        Assert.Equal("这不是你的渔网。", error);
        Assert.Single(manager.Nets);
    }

    [Fact(Skip = "Requires game initialization")]
    public void TryHarvest_AllowsOwner()
    {
        var manager = new PassiveNetManager();
        var net = new PassiveNetData(
            OwnerId: 1234L,
            LocationName: "Beach",
            Tile: new Vector2(10, 20),
            Level: NetLevel.Copper,
            Harvest: new List<PassiveNetHarvestData>
            {
                new("(O)128", 1, 0)
            });
        manager.TryAdd(net, out _);

        var owner = (Farmer)FormatterServices.GetUninitializedObject(typeof(Farmer));
        SetUniqueMultiplayerID(owner, 1234L);
        var location = (GameLocation)FormatterServices.GetUninitializedObject(typeof(GameLocation));
        SetLocationName(location, "Beach");

        bool result = manager.TryHarvest(owner, location, new Vector2(10, 20), out string? error);

        Assert.True(result);
        Assert.Null(error);
        Assert.Empty(manager.Nets);
    }

    private static void SetUniqueMultiplayerID(Farmer farmer, long id)
    {
        var field = typeof(Farmer).GetField("uniqueMultiplayerID", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    ?? typeof(Farmer).GetField("<UniqueMultiplayerID>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field is not null)
        {
            var netLong = (Netcode.NetLong)FormatterServices.GetUninitializedObject(typeof(Netcode.NetLong));
            typeof(Netcode.NetLong).GetProperty("Value")?.SetValue(netLong, id);
            field.SetValue(farmer, netLong);
        }
    }

    private static void SetLocationName(GameLocation location, string name)
    {
        // GameLocation.Name is backed by NetString field "name"
        var field = typeof(GameLocation).GetField("name", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    ?? typeof(GameLocation).GetField("<Name>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field is not null)
        {
            var netString = (Netcode.NetString)FormatterServices.GetUninitializedObject(typeof(Netcode.NetString));
            typeof(Netcode.NetString).GetProperty("Value")?.SetValue(netString, name);
            field.SetValue(location, netString);
        }
    }
}
