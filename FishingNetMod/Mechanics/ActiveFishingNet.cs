using FishingNetMod.Data;
using FishingNetMod.Items;
using FishingNetMod.Quests;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace FishingNetMod.Mechanics;

internal sealed class ActiveFishingNet
{
    private readonly IMonitor monitor;
    private readonly FishingNetItemFactory itemFactory;
    private readonly IFishProvider fishProvider;
    private readonly QuestProgressTracker? questProgressTracker;

    public ActiveFishingNet(IMonitor monitor, FishingNetItemFactory itemFactory, IFishProvider fishProvider, QuestProgressTracker? questProgressTracker = null)
    {
        this.monitor = monitor;
        this.itemFactory = itemFactory;
        this.fishProvider = fishProvider;
        this.questProgressTracker = questProgressTracker;
    }

    public bool TryUse(Farmer player, GameLocation location,
        out ActiveFishingNetCast? cast, PassiveNetManager? passiveNetManager = null)
    {
        cast = null;

        if (!this.itemFactory.TryGetNetData(player.CurrentItem, out NetLevelData? data) || data is null)
            return false;

        Vector2 targetTile = this.GetFacingTile(player);
        if (!location.isWaterTile((int)targetTile.X, (int)targetTile.Y))
        {
            Game1.showRedMessage("这里不能撒网。");
            return true;
        }

        if (passiveNetManager?.TryGetHarvestableNet(location.Name, targetTile, out _) == true)
        {
            Game1.showRedMessage("这里已经有渔网了。");
            cast = null;
            return true;
        }

        NetLevelData netData = data;
        int attempts = Game1.random.Next(netData.MinCatch, netData.MaxCatch + 1);
        var caughtItems = new List<Item>();
        for (int i = 0; i < attempts; i++)
        {
            Item? caught = this.fishProvider.GetFish(location, player, targetTile);
            if (caught is null)
                continue;

            caughtItems.Add(caught);
        }

        player.Stamina = Math.Max(0, player.Stamina - netData.StaminaCost);
        location.playSound("waterSlosh");
        cast = new ActiveFishingNetCast(netData, caughtItems, attempts);
        this.monitor.Log($"{player.Name} cast {netData.DisplayName} and started a challenge with {caughtItems.Count} pending fish from {attempts} attempt(s).", LogLevel.Trace);
        return true;
    }

    public void CompleteCatch(Farmer player, GameLocation location, ActiveFishingNetCast cast)
    {
        foreach (Item caught in cast.CaughtItems)
        {
            this.GiveOrDrop(player, location, caught);
            this.questProgressTracker?.RecordNetCatch(player.UniqueMultiplayerID, cast.NetData.Level, caught.Quality, Game1.currentSeason);
        }

        Game1.addHUDMessage(new HUDMessage(cast.GetResultMessage(), HUDMessage.newQuest_type));
        this.monitor.Log($"{player.Name} completed {cast.NetData.DisplayName} challenge and received {cast.CaughtCount} fish from {cast.Attempts} attempt(s).", LogLevel.Trace);
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
