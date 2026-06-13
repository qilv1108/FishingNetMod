using System.Reflection;
using System.Runtime.Serialization;
using FishingNetMod.Data;
using FishingNetMod.Items;
using FishingNetMod.Mechanics;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Framework.Logging;
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
    public void TryUse_BlocksWhenPassiveNetAtTile()
    {
        // 1. 放置一个被动网在 (1, 0) —— 面朝上的 tile
        var passiveNetManager = new PassiveNetManager();
        passiveNetManager.TryAdd(new PassiveNetData(
            OwnerId: 9999L,
            LocationName: "Farm",
            Tile: new Vector2(1, 0),
            Level: NetLevel.Copper,
            Harvest: new List<PassiveNetHarvestData>()), out _);

        // 2. 构造 ActiveFishingNet，注入 stub 委托跳过所有游戏依赖
        var activeNet = new ActiveFishingNet(
            new DummyMonitor(),
            new FishingNetItemFactory(),
            new VanillaFishProvider(),
            getHeldNet: _ => NetLevelData.Get(NetLevel.Copper),
            isWaterTileFunc: (loc, x, y) => true,
            getTargetTile: _ => new Vector2(1, 0),
            showRedMessage: _ => { });

        // 3. 构造最小 player（不需要 CurrentItem/Tile/FacingDirection，均已注入委托）
        var player = (Farmer)FormatterServices.GetUninitializedObject(typeof(Farmer));
        SetUniqueMultiplayerID(player, 1111L);

        // 4. 构造名为 "Farm" 的 location（不需要 isWaterTile 工作）
        var location = (GameLocation)FormatterServices.GetUninitializedObject(typeof(GameLocation));
        SetLocationName(location, "Farm");

        // 5. 验证冲突检查
        bool result = activeNet.TryUse(player, location, out var cast, passiveNetManager);

        Assert.True(result);
        Assert.Null(cast);
        Assert.Single(passiveNetManager.Nets);
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
        var field = typeof(GameLocation).GetField("name", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    ?? typeof(GameLocation).GetField("<Name>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field is not null)
        {
            var netString = (Netcode.NetString)FormatterServices.GetUninitializedObject(typeof(Netcode.NetString));
            typeof(Netcode.NetString).GetProperty("Value")?.SetValue(netString, name);
            field.SetValue(location, netString);
        }
    }

    private sealed class DummyMonitor : IMonitor
    {
        public void Log(string message, LogLevel level = LogLevel.Trace) { }
        public void LogOnce(string message, LogLevel level = LogLevel.Trace) { }
        public void VerboseLog(string message) { }
        public void VerboseLog(ref VerboseLogStringHandler message) { }
        public bool IsVerbose => false;
        public string Name => "Test";
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
