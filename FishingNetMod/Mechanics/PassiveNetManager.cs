using FishingNetMod.Data;
using FishingNetMod.Items;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace FishingNetMod.Mechanics;

internal sealed class PassiveNetManager
{
    private const string SaveDataKey = "PassiveNets";

    private readonly List<PassiveNetData> nets = new();
    private readonly FishingNetItemFactory itemFactory;
    private readonly IFishProvider fishProvider;

    public PassiveNetManager()
        : this(new FishingNetItemFactory(), new VanillaFishProvider())
    {
    }

    public PassiveNetManager(FishingNetItemFactory itemFactory, IFishProvider fishProvider)
    {
        this.itemFactory = itemFactory;
        this.fishProvider = fishProvider;
    }

    public IReadOnlyList<PassiveNetData> Nets => this.nets;

    public bool TryAdd(PassiveNetData data, out string? error)
    {
        if (this.nets.Any(net => net.OwnerId == data.OwnerId))
        {
            error = "你已经放置了一个渔网。";
            return false;
        }

        if (this.nets.Any(net => net.LocationName == data.LocationName && net.Tile == data.Tile))
        {
            error = "这里已经有渔网了。";
            return false;
        }

        this.nets.Add(data);
        error = null;
        return true;
    }

    public bool TryPlace(Farmer player, GameLocation location, Vector2 targetTile, NetLevelData netData, out string? error)
    {
        if (!location.isWaterTile((int)targetTile.X, (int)targetTile.Y))
        {
            error = "这里不能放置渔网。";
            return false;
        }

        var data = new PassiveNetData(player.UniqueMultiplayerID, location.Name, targetTile, netData.Level, new List<PassiveNetHarvestData>());
        if (!this.TryAdd(data, out error))
            return false;

        player.removeItemFromInventory(player.CurrentItem);
        return true;
    }

    public bool TryHarvest(Farmer player, GameLocation location, Vector2 targetTile, out string? error)
    {
        PassiveNetData? data = this.nets.FirstOrDefault(net => net.LocationName == location.Name && net.Tile == targetTile);
        if (data is null)
        {
            error = null;
            return false;
        }

        foreach (PassiveNetHarvestData harvest in data.Harvest)
        {
            Item item = ItemRegistry.Create(harvest.QualifiedItemId, harvest.Stack);
            this.GiveOrDrop(player, location, item);
        }

        this.GiveOrDrop(player, location, this.itemFactory.Create(NetLevelData.Get(data.Level)));
        this.nets.Remove(data);
        error = null;
        return true;
    }

    public void ProduceDaily(GameLocation location)
    {
        foreach (PassiveNetData net in this.nets.Where(net => net.LocationName == location.Name).ToList())
        {
            var range = GetDailyProductionRange(net.Level);
            int count = Game1.random.Next(range.Min, range.Max + 1);
            for (int i = 0; i < count; i++)
            {
                Item? fish = this.fishProvider.GetFish(location, Game1.player, net.Tile);
                if (fish is null)
                    continue;

                net.Harvest.Add(new PassiveNetHarvestData(fish.QualifiedItemId, fish.Stack));
            }
        }
    }

    public void Load(IModHelper helper)
    {
        this.nets.Clear();
        List<PassiveNetData>? data = helper.Data.ReadSaveData<List<PassiveNetData>>(SaveDataKey);
        if (data is not null)
            this.nets.AddRange(data);
    }

    public void Save(IModHelper helper)
    {
        helper.Data.WriteSaveData(SaveDataKey, this.nets);
    }

    public static (int Min, int Max) GetDailyProductionRange(NetLevel level)
    {
        return level switch
        {
            NetLevel.Copper => (1, 1),
            NetLevel.Iron => (1, 2),
            NetLevel.Gold => (1, 2),
            NetLevel.Iridium => (2, 2),
            _ => (1, 1)
        };
    }

    private void GiveOrDrop(Farmer player, GameLocation location, Item item)
    {
        Item? leftover = player.addItemToInventory(item);
        if (leftover is null)
            return;

        Game1.createItemDebris(leftover, player.getStandingPosition(), player.FacingDirection, location);
    }
}
