using FishingNetMod.Data;
using FishingNetMod.Items;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace FishingNetMod.Mechanics;

internal sealed class ActiveFishingNet
{
    private readonly IMonitor monitor;
    private readonly FishingNetItemFactory itemFactory;
    private readonly IFishProvider fishProvider;

    public ActiveFishingNet(IMonitor monitor, FishingNetItemFactory itemFactory, IFishProvider fishProvider)
    {
        this.monitor = monitor;
        this.itemFactory = itemFactory;
        this.fishProvider = fishProvider;
    }

    public bool TryUse(Farmer player, GameLocation location)
    {
        if (!this.itemFactory.TryGetNetData(player.CurrentItem, out NetLevelData? data) || data is null)
            return false;

        Vector2 targetTile = this.GetFacingTile(player);
        if (!location.isWaterTile((int)targetTile.X, (int)targetTile.Y))
        {
            Game1.showRedMessage("这里不能撒网。");
            return true;
        }

        NetLevelData netData = data;
        int attempts = Game1.random.Next(netData.MinCatch, netData.MaxCatch + 1);
        int caughtCount = 0;
        for (int i = 0; i < attempts; i++)
        {
            Item? caught = this.fishProvider.GetFish(location, player, targetTile);
            if (caught is null)
                continue;

            this.GiveOrDrop(player, location, caught);
            caughtCount++;
        }

        player.Stamina = Math.Max(0, player.Stamina - netData.StaminaCost);
        location.playSound("waterSlosh");
        Game1.addHUDMessage(new HUDMessage(caughtCount > 0 ? $"捕获了 {caughtCount} 条鱼！" : "没有捕到鱼。", HUDMessage.newQuest_type));
        this.monitor.Log($"{player.Name} used {netData.DisplayName} and caught {caughtCount} fish from {attempts} attempt(s).", LogLevel.Trace);
        return true;
    }

    private Vector2 GetFacingTile(Farmer player)
    {
        Vector2 tile = player.Tile;
        return player.FacingDirection switch
        {
            Game1.up => tile + new Vector2(0, -1),
            Game1.right => tile + new Vector2(1, 0),
            Game1.down => tile + new Vector2(0, 1),
            Game1.left => tile + new Vector2(-1, 0),
            _ => tile
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
