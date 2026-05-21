using FishingNetMod.Data;
using FishingNetMod.Items;
using FishingNetMod.Mechanics;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace FishingNetMod;

internal sealed class ModEntry : Mod
{
    private FishingNetItemFactory? itemFactory;
    private ActiveFishingNet? activeFishingNet;
    private PassiveNetManager? passiveNetManager;
    private PassiveNetRenderer? passiveNetRenderer;

    public override void Entry(IModHelper helper)
    {
        this.itemFactory = new FishingNetItemFactory();
        this.activeFishingNet = new ActiveFishingNet(this.Monitor, this.itemFactory, new VanillaFishProvider());
        this.passiveNetManager = new PassiveNetManager(this.itemFactory, new VanillaFishProvider());
        this.passiveNetRenderer = new PassiveNetRenderer();

        helper.ConsoleCommands.Add(
            "fishing_net",
            "Fishing Net Mod debug command. Usage: fishing_net give <copper|iron|gold|iridium>",
            this.HandleFishingNetCommand);

        helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.GameLoop.Saving += this.OnSaving;
        helper.Events.GameLoop.DayStarted += this.OnDayStarted;
        helper.Events.Display.RenderedWorld += this.OnRenderedWorld;
        this.Monitor.Log("Fishing Net Mod loaded.", LogLevel.Info);
    }

    private void HandleFishingNetCommand(string command, string[] args)
    {
        if (args.Length != 2 || !string.Equals(args[0], "give", StringComparison.OrdinalIgnoreCase))
        {
            this.Monitor.Log("Usage: fishing_net give <copper|iron|gold|iridium>", LogLevel.Info);
            return;
        }

        if (!Context.IsWorldReady)
        {
            this.Monitor.Log("Load a save before using this command.", LogLevel.Info);
            return;
        }

        if (!NetLevelData.TryParse(args[1], out NetLevelData? data) || data is null)
        {
            this.Monitor.Log("Unknown net level. Valid levels: copper, iron, gold, iridium.", LogLevel.Info);
            return;
        }

        Item item = this.itemFactory!.Create(data);
        Item? leftover = Game1.player.addItemToInventory(item);
        if (leftover is not null)
        {
            Game1.createItemDebris(leftover, Game1.player.getStandingPosition(), Game1.player.FacingDirection, Game1.player.currentLocation);
            this.Monitor.Log($"Inventory full; dropped {data.DisplayName} at the player's feet.", LogLevel.Info);
            return;
        }

        this.Monitor.Log($"Added {data.DisplayName} to your inventory.", LogLevel.Info);
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        if (e.Button.IsUseToolButton())
        {
            if (this.activeFishingNet!.TryUse(Game1.player, Game1.currentLocation))
                this.Helper.Input.Suppress(e.Button);
            return;
        }

        if (!e.Button.IsActionButton())
            return;

        Vector2 targetTile = this.GetFacingTile(Game1.player);
        if (this.passiveNetManager!.TryHarvest(Game1.player, Game1.currentLocation, targetTile, out string? harvestError))
        {
            this.Helper.Input.Suppress(e.Button);
            if (harvestError is not null)
                Game1.showRedMessage(harvestError);
            return;
        }

        if (!this.itemFactory!.TryGetNetData(Game1.player.CurrentItem, out NetLevelData? data) || data is null)
            return;

        if (!this.passiveNetManager.TryPlace(Game1.player, Game1.currentLocation, targetTile, data, out string? placeError))
        {
            if (placeError is not null)
                Game1.showRedMessage(placeError);
            this.Helper.Input.Suppress(e.Button);
            return;
        }

        Game1.addHUDMessage(new HUDMessage("渔网已放置。", HUDMessage.newQuest_type));
        this.Helper.Input.Suppress(e.Button);
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

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        this.passiveNetManager!.Load(this.Helper);
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        this.passiveNetManager!.Save(this.Helper);
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        foreach (GameLocation location in Game1.locations)
            this.passiveNetManager!.ProduceDaily(location);
    }

    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        this.passiveNetRenderer!.Draw(e.SpriteBatch, this.passiveNetManager!.Nets);
    }
}
