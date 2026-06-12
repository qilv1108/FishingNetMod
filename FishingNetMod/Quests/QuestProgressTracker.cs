using FishingNetMod.Data;
using FishingNetMod.Items;
using StardewModdingAPI;
using StardewValley;

namespace FishingNetMod.Quests;

internal sealed class QuestProgressTracker
{
    public const int SilverFishRequiredForIronNet = 10;
    public const int GoldFishRequiredForGoldNet = 10;
    public const int FishingLevelRequiredForCopperQuest = 1;

    private const int SilverQuality = 1;
    private const int GoldQuality = 2;
    private const int SeasonsRequiredForIridiumNet = 4;

    private readonly Dictionary<long, QuestProgress> progressByPlayer = new();

    internal QuestProgress GetOrCreateProgress(long playerId)
    {
        if (!this.progressByPlayer.TryGetValue(playerId, out QuestProgress? progress))
        {
            progress = new QuestProgress();
            this.progressByPlayer[playerId] = progress;
        }

        return progress;
    }

    private static string SaveKey(long playerId) => $"QuestProgress_{playerId}";

    public QuestProgress Progress
    {
        get => this.GetOrCreateProgress(Game1.player?.UniqueMultiplayerID ?? 0);
        set
        {
            long id = Game1.player?.UniqueMultiplayerID ?? 0;
            this.progressByPlayer[id] = value;
        }
    }

    public void Load(IModHelper helper, long playerId)
    {
        QuestProgress? progress = Normalize(helper.Data.ReadSaveData<QuestProgress>(SaveKey(playerId)));
        if (progress is not null)
            this.progressByPlayer[playerId] = progress;
    }

    public void Load(IModHelper helper) => this.Load(helper, Game1.player?.UniqueMultiplayerID ?? 0);

    public void Save(IModHelper helper, long playerId)
    {
        if (this.progressByPlayer.TryGetValue(playerId, out QuestProgress? progress))
            helper.Data.WriteSaveData(SaveKey(playerId), Normalize(progress));
    }

    public void Save(IModHelper helper) => this.Save(helper, Game1.player?.UniqueMultiplayerID ?? 0);

    public void RecordPassiveCatch(long playerId, NetLevel netLevel, int quality, string season)
    {
        this.RecordNetCatch(playerId, netLevel, quality, season);
    }

    public void RecordPassiveCatch(NetLevel netLevel, int quality, string season) =>
        this.RecordPassiveCatch(Game1.player?.UniqueMultiplayerID ?? 0, netLevel, quality, season);

    public void RecordNetCatch(long playerId, NetLevel netLevel, int quality, string season)
    {
        QuestProgress progress = this.GetOrCreateProgress(playerId);

        switch (netLevel)
        {
            case NetLevel.Copper when quality == SilverQuality:
                progress.SilverFishCount++;
                break;

            case NetLevel.Iron when quality >= GoldQuality:
                progress.GoldFishCount++;
                break;

            case NetLevel.Gold when !string.IsNullOrWhiteSpace(season):
                progress.SeasonsFished.Add(season.Trim().ToLowerInvariant());
                break;
        }
    }

    public void RecordNetCatch(NetLevel netLevel, int quality, string season) =>
        this.RecordNetCatch(Game1.player?.UniqueMultiplayerID ?? 0, netLevel, quality, season);

    public QuestUnlockPlan EvaluateUnlocks(QuestPlayerSnapshot player)
    {
        var recipesToUnlock = new List<string>();
        var mailToQueue = new List<string>();

        AddCopperUnlockIfNeeded(player, recipesToUnlock, mailToQueue);

        AddUnlockIfNeeded(
            this.Progress.SilverFishCount >= SilverFishRequiredForIronNet,
            FishingNetIds.IronNetRecipe,
            FishingNetIds.IronQuestMailId,
            player,
            recipesToUnlock,
            mailToQueue);

        AddUnlockIfNeeded(
            this.Progress.GoldFishCount >= GoldFishRequiredForGoldNet,
            FishingNetIds.GoldNetRecipe,
            FishingNetIds.GoldQuestMailId,
            player,
            recipesToUnlock,
            mailToQueue);

        AddUnlockIfNeeded(
            this.Progress.SeasonsFished.Count >= SeasonsRequiredForIridiumNet,
            FishingNetIds.IridiumNetRecipe,
            FishingNetIds.IridiumQuestMailId,
            player,
            recipesToUnlock,
            mailToQueue);

        return new QuestUnlockPlan(recipesToUnlock, mailToQueue);
    }

    public QuestUnlockPlan EvaluateCopperUnlocks(QuestPlayerSnapshot player)
    {
        var recipesToUnlock = new List<string>();
        var mailToQueue = new List<string>();

        AddCopperUnlockIfNeeded(player, recipesToUnlock, mailToQueue);

        return new QuestUnlockPlan(recipesToUnlock, mailToQueue);
    }

    private void AddCopperUnlockIfNeeded(
        QuestPlayerSnapshot player,
        List<string> recipesToUnlock,
        List<string> mailToQueue)
    {
        if (player.FishingLevel < FishingLevelRequiredForCopperQuest)
            return;

        IReadOnlySet<string> receivedMailFlags = player.ReceivedMailFlags ?? player.KnownMailFlags;

        if (player.KnownRecipes.Contains(FishingNetIds.CopperNetRecipe))
        {
            if (player.KnownMailFlags.Contains(FishingNetIds.CopperQuestMailId))
                this.Progress.CopperQuestMailQueued = true;
            else
            {
                mailToQueue.Add(FishingNetIds.CopperQuestMailId);
                this.Progress.CopperQuestMailQueued = true;
            }

            return;
        }

        if (receivedMailFlags.Contains(FishingNetIds.CopperQuestMailId))
        {
            this.Progress.CopperQuestMailQueued = true;
            recipesToUnlock.Add(FishingNetIds.CopperNetRecipe);
            return;
        }

        if (this.Progress.CopperQuestMailQueued && player.KnownMailFlags.Contains(FishingNetIds.CopperQuestMailId))
            return;

        mailToQueue.Add(FishingNetIds.CopperQuestMailId);
        this.Progress.CopperQuestMailQueued = true;
    }

    private static void AddUnlockIfNeeded(
        bool conditionMet,
        string recipe,
        string mailId,
        QuestPlayerSnapshot player,
        List<string> recipesToUnlock,
        List<string> mailToQueue)
    {
        if (!conditionMet || player.KnownRecipes.Contains(recipe))
            return;

        recipesToUnlock.Add(recipe);
        if (!player.KnownMailFlags.Contains(mailId))
            mailToQueue.Add(mailId);
    }

    private static QuestProgress Normalize(QuestProgress? progress)
    {
        QuestProgress normalized = progress ?? new QuestProgress();
        normalized.Version = QuestProgress.CurrentVersion;
        normalized.SeasonsFished = new HashSet<string>(
            normalized.SeasonsFished ?? Enumerable.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);
        return normalized;
    }
}
