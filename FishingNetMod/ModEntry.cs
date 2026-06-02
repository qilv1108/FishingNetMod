using FishingNetMod.Data;
using FishingNetMod.Items;
using FishingNetMod.Mechanics;
using FishingNetMod.Menus;
using FishingNetMod.Quests;
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
    private QuestProgressTracker? questProgressTracker;

    public override void Entry(IModHelper helper)
    {
        this.itemFactory = new FishingNetItemFactory();
        var fishProvider = new VanillaFishProvider();
        this.questProgressTracker = new QuestProgressTracker();
        this.activeFishingNet = new ActiveFishingNet(this.Monitor, this.itemFactory, fishProvider, this.questProgressTracker);
        this.passiveNetManager = new PassiveNetManager(this.itemFactory, fishProvider, this.questProgressTracker);
        this.passiveNetRenderer = new PassiveNetRenderer();

        helper.ConsoleCommands.Add(
            "fishing_net",
            "Fishing Net Mod debug command. Usage: fishing_net give <copper|iron|gold|iridium>",
            this.HandleFishingNetCommand);

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.GameLoop.Saving += this.OnSaving;
        helper.Events.GameLoop.DayStarted += this.OnDayStarted;
        helper.Events.GameLoop.DayEnding += this.OnDayEnding;
        helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
        helper.Events.Display.RenderedWorld += this.OnRenderedWorld;
        this.Monitor.Log("Fishing Net Mod loaded.", LogLevel.Info);
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        if (!this.Helper.ModRegistry.IsLoaded("Pathoschild.ContentPatcher"))
            this.Monitor.Log("Content Patcher is not loaded. Fishing net recipes, mail, dialogue, and custom item data may be unavailable.", LogLevel.Warn);
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

        if (Game1.activeClickableMenu is not null)
            return;

        if (e.Button.IsUseToolButton())
        {
            GameLocation fishingLocation = Game1.currentLocation;
            if (this.activeFishingNet!.TryUse(Game1.player, fishingLocation, out ActiveFishingNetCast? cast))
            {
                this.Helper.Input.Suppress(e.Button);
                if (cast is not null)
                {
                    Game1.activeClickableMenu = new NetHarvestChallengeMenu(
                        onSuccess: () => this.CompleteActiveFishing(Game1.player, fishingLocation, cast),
                        onFailure: () => Game1.showRedMessage("捕鱼失败。"),
                        targetNumbers: CreateActiveFishingChallengeDigits(),
                        title: "捕鱼挑战",
                        instruction: "按顺序输入显示的数字，在 30 秒内完成。");
                }
            }

            return;
        }

        if (!e.Button.IsActionButton())
            return;

        Vector2 targetTile = this.GetFacingTile(Game1.player);
        GameLocation location = Game1.currentLocation;
        if (this.passiveNetManager!.TryGetHarvestableNet(location.Name, targetTile, out PassiveNetData? harvestable) && harvestable is not null)
        {
            this.Helper.Input.Suppress(e.Button);
            Game1.activeClickableMenu = new NetHarvestChallengeMenu(
                onSuccess: () => this.CompletePassiveHarvest(Game1.player, location, targetTile),
                onFailure: () => Game1.showRedMessage("收网失败。"));
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

    private void CompletePassiveHarvest(Farmer player, GameLocation location, Vector2 targetTile)
    {
        if (this.passiveNetManager!.TryHarvest(player, location, targetTile, out string? harvestError))
        {
            Game1.addHUDMessage(new HUDMessage("收网成功。", HUDMessage.newQuest_type));
            return;
        }

        if (harvestError is not null)
            Game1.showRedMessage(harvestError);
    }

    private void CompleteActiveFishing(Farmer player, GameLocation location, ActiveFishingNetCast cast)
    {
        this.activeFishingNet!.CompleteCatch(player, location, cast);
    }

    private static IReadOnlyList<int> CreateActiveFishingChallengeDigits()
    {
        var digits = new int[5];
        for (int index = 0; index < digits.Length; index++)
            digits[index] = Game1.random.Next(0, 10);

        return digits;
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
        this.questProgressTracker!.Load(this.Helper);
        this.passiveNetManager!.Load(this.Helper);
        this.ApplyQuestUnlocks(Game1.player);
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        this.questProgressTracker!.Save(this.Helper);
        this.passiveNetManager!.Save(this.Helper);
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        foreach (GameLocation location in Game1.locations)
            this.passiveNetManager!.ProduceDaily(location);

        this.ApplyCopperQuestUnlocks(Game1.player);
    }

    private void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        this.ApplyQuestUnlocks(Game1.player);
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || !e.IsMultipleOf(60))
            return;

        this.ApplyCopperQuestUnlocks(Game1.player);
    }

    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        this.passiveNetRenderer!.Draw(e.SpriteBatch, this.passiveNetManager!.Nets);
    }

    private void ApplyQuestUnlocks(Farmer player)
    {
        QuestUnlockPlan plan = this.questProgressTracker!.EvaluateUnlocks(QuestPlayerSnapshot.FromFarmer(player));
        this.ApplyQuestUnlockPlan(player, plan);
    }

    private void ApplyCopperQuestUnlocks(Farmer player)
    {
        QuestUnlockPlan plan = this.questProgressTracker!.EvaluateCopperUnlocks(QuestPlayerSnapshot.FromFarmer(player));
        this.ApplyQuestUnlockPlan(player, plan);
    }

    private void ApplyQuestUnlockPlan(Farmer player, QuestUnlockPlan plan)
    {
        if (!plan.HasChanges)
            return;

        foreach (string recipe in plan.RecipesToUnlock)
        {
            player.craftingRecipes[recipe] = 0;
            this.Monitor.Log($"Unlocked fishing net recipe: {recipe}", LogLevel.Info);
        }

        foreach (string mailId in plan.MailToQueue)
        {
            if (!player.mailForTomorrow.Contains(mailId) && !player.mailbox.Contains(mailId) && !player.mailReceived.Contains(mailId))
                player.mailForTomorrow.Add(mailId);

            this.Monitor.Log($"Queued fishing net quest mail: {mailId}", LogLevel.Trace);
        }
    }
}
