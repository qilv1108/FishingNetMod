using FishingNetMod.Data;
using FishingNetMod.Items;
using StardewModdingAPI;

namespace FishingNetMod.Quests;

internal sealed class QuestProgressTracker
{
    public const int SilverFishRequiredForIronNet = 10;
    public const int GoldFishRequiredForGoldNet = 10;
    public const int FishingLevelRequiredForCopperQuest = 1;

    private const string SaveDataKey = "QuestProgress";
    private const int SilverQuality = 1;
    private const int GoldQuality = 2;
    private const int SeasonsRequiredForIridiumNet = 4;

    public QuestProgress Progress { get; set; } = new();

    public void Load(IModHelper helper)
    {
        this.Progress = Normalize(helper.Data.ReadSaveData<QuestProgress>(SaveDataKey));
    }

    public void Save(IModHelper helper)
    {
        helper.Data.WriteSaveData(SaveDataKey, Normalize(this.Progress));
    }

    public void RecordPassiveCatch(NetLevel netLevel, int quality, string season)
    {
        this.RecordNetCatch(netLevel, quality, season);
    }

    public void RecordNetCatch(NetLevel netLevel, int quality, string season)
    {
        switch (netLevel)
        {
            case NetLevel.Copper when quality == SilverQuality:
                this.Progress.SilverFishCount++;
                break;

            case NetLevel.Iron when quality >= GoldQuality:
                this.Progress.GoldFishCount++;
                break;

            case NetLevel.Gold when !string.IsNullOrWhiteSpace(season):
                this.Progress.SeasonsFished.Add(season.Trim().ToLowerInvariant());
                break;
        }
    }

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
