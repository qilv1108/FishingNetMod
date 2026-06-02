using FishingNetMod.Data;
using FishingNetMod.Items;
using FishingNetMod.Quests;
using Xunit;

namespace FishingNetMod.Tests.Quests;

public sealed class QuestProgressTrackerTests
{
    [Fact]
    public void RecordPassiveCatchCountsCopperSilverFishOnly()
    {
        var tracker = new QuestProgressTracker();

        tracker.RecordPassiveCatch(NetLevel.Copper, quality: 1, season: "spring");
        tracker.RecordPassiveCatch(NetLevel.Copper, quality: 0, season: "spring");
        tracker.RecordPassiveCatch(NetLevel.Iron, quality: 1, season: "spring");

        Assert.Equal(1, tracker.Progress.SilverFishCount);
    }

    [Fact]
    public void RecordNetCatchCountsTheSameMilestonesUsedByQuestFlow()
    {
        var tracker = new QuestProgressTracker();

        tracker.RecordNetCatch(NetLevel.Copper, quality: 1, season: "spring");
        tracker.RecordNetCatch(NetLevel.Iron, quality: 2, season: "summer");
        tracker.RecordNetCatch(NetLevel.Gold, quality: 0, season: "fall");

        Assert.Equal(1, tracker.Progress.SilverFishCount);
        Assert.Equal(1, tracker.Progress.GoldFishCount);
        Assert.Equal(new[] { "fall" }, tracker.Progress.SeasonsFished.ToArray());
    }

    [Fact]
    public void RecordPassiveCatchCountsIronGoldAndIridiumFishOnly()
    {
        var tracker = new QuestProgressTracker();

        tracker.RecordPassiveCatch(NetLevel.Iron, quality: 2, season: "summer");
        tracker.RecordPassiveCatch(NetLevel.Iron, quality: 4, season: "summer");
        tracker.RecordPassiveCatch(NetLevel.Iron, quality: 1, season: "summer");
        tracker.RecordPassiveCatch(NetLevel.Gold, quality: 4, season: "summer");

        Assert.Equal(2, tracker.Progress.GoldFishCount);
    }

    [Fact]
    public void RecordPassiveCatchStoresDistinctGoldNetSeasons()
    {
        var tracker = new QuestProgressTracker();

        tracker.RecordPassiveCatch(NetLevel.Gold, quality: 0, season: "spring");
        tracker.RecordPassiveCatch(NetLevel.Gold, quality: 2, season: "spring");
        tracker.RecordPassiveCatch(NetLevel.Gold, quality: 1, season: "winter");
        tracker.RecordPassiveCatch(NetLevel.Iridium, quality: 4, season: "fall");

        Assert.Equal(new[] { "spring", "winter" }, tracker.Progress.SeasonsFished.OrderBy(season => season).ToArray());
    }

    [Fact]
    public void EvaluateUnlocksQueuesCopperQuestMailForTomorrowAtFishingLevelOneBeforeUnlockingCopperRecipe()
    {
        var tracker = new QuestProgressTracker();
        var snapshot = new QuestPlayerSnapshot(
            FishingLevel: 1,
            KnownRecipes: new HashSet<string>(),
            KnownMailFlags: new HashSet<string>(),
            ReceivedMailFlags: new HashSet<string>());

        QuestUnlockPlan plan = tracker.EvaluateUnlocks(snapshot);

        Assert.Equal(new[] { FishingNetIds.CopperQuestMailId }, plan.MailToQueue);
        Assert.Empty(plan.RecipesToUnlock);
        Assert.True(tracker.Progress.CopperQuestMailQueued);
    }

    [Fact]
    public void EvaluateUnlocksUnlocksCopperRecipeAfterMailArrivesAtFishingLevelOne()
    {
        var tracker = new QuestProgressTracker();
        tracker.Progress.CopperQuestMailQueued = true;
        var snapshot = new QuestPlayerSnapshot(
            FishingLevel: 1,
            KnownRecipes: new HashSet<string>(),
            KnownMailFlags: new HashSet<string> { FishingNetIds.CopperQuestMailId },
            ReceivedMailFlags: new HashSet<string> { FishingNetIds.CopperQuestMailId });

        QuestUnlockPlan plan = tracker.EvaluateUnlocks(snapshot);

        Assert.Equal(new[] { FishingNetIds.CopperNetRecipe }, plan.RecipesToUnlock);
        Assert.Empty(plan.MailToQueue);
    }

    [Fact]
    public void CopperQuestFishingLevelRequirementIsOne()
    {
        Assert.Equal(1, QuestProgressTracker.FishingLevelRequiredForCopperQuest);
    }

    [Fact]
    public void QuestUnlockPlanDoesNotExposeImmediateMailDelivery()
    {
        Assert.Null(typeof(QuestUnlockPlan).GetProperty("MailToDeliverNow"));
    }

    [Fact]
    public void EvaluateUnlocksDoesNotStartCopperQuestAtFishingLevelZero()
    {
        var tracker = new QuestProgressTracker();
        var snapshot = new QuestPlayerSnapshot(
            FishingLevel: 0,
            KnownRecipes: new HashSet<string>(),
            KnownMailFlags: new HashSet<string>(),
            ReceivedMailFlags: new HashSet<string>());

        QuestUnlockPlan plan = tracker.EvaluateUnlocks(snapshot);

        Assert.Empty(plan.MailToQueue);
        Assert.Empty(plan.RecipesToUnlock);
        Assert.False(tracker.Progress.CopperQuestMailQueued);
    }

    [Fact]
    public void EvaluateUnlocksRequeuesCopperQuestMailIfQueuedFlagExistsButMailIsMissing()
    {
        var tracker = new QuestProgressTracker();
        tracker.Progress.CopperQuestMailQueued = true;
        var snapshot = new QuestPlayerSnapshot(
            FishingLevel: 1,
            KnownRecipes: new HashSet<string>(),
            KnownMailFlags: new HashSet<string>(),
            ReceivedMailFlags: new HashSet<string>());

        QuestUnlockPlan plan = tracker.EvaluateUnlocks(snapshot);

        Assert.Equal(new[] { FishingNetIds.CopperQuestMailId }, plan.MailToQueue);
        Assert.Empty(plan.RecipesToUnlock);
    }

    [Fact]
    public void EvaluateUnlocksUnlocksIronRecipeAfterTenCopperSilverFish()
    {
        var tracker = new QuestProgressTracker();
        tracker.Progress.SilverFishCount = QuestProgressTracker.SilverFishRequiredForIronNet;
        var snapshot = new QuestPlayerSnapshot(
            FishingLevel: 0,
            KnownRecipes: new HashSet<string> { FishingNetIds.CopperNetRecipe },
            KnownMailFlags: new HashSet<string>());

        QuestUnlockPlan plan = tracker.EvaluateUnlocks(snapshot);

        Assert.Equal(new[] { FishingNetIds.IronNetRecipe }, plan.RecipesToUnlock);
        Assert.Equal(new[] { FishingNetIds.IronQuestMailId }, plan.MailToQueue);
    }

    [Fact]
    public void EvaluateUnlocksUnlocksGoldRecipeAfterTenIronGoldFish()
    {
        var tracker = new QuestProgressTracker();
        tracker.Progress.GoldFishCount = QuestProgressTracker.GoldFishRequiredForGoldNet;
        var snapshot = new QuestPlayerSnapshot(
            FishingLevel: 0,
            KnownRecipes: new HashSet<string> { FishingNetIds.CopperNetRecipe, FishingNetIds.IronNetRecipe },
            KnownMailFlags: new HashSet<string>());

        QuestUnlockPlan plan = tracker.EvaluateUnlocks(snapshot);

        Assert.Equal(new[] { FishingNetIds.GoldNetRecipe }, plan.RecipesToUnlock);
        Assert.Equal(new[] { FishingNetIds.GoldQuestMailId }, plan.MailToQueue);
    }

    [Fact]
    public void EvaluateUnlocksUnlocksIridiumRecipeAfterGoldNetRecordsFourSeasons()
    {
        var tracker = new QuestProgressTracker();
        tracker.Progress.SeasonsFished.UnionWith(new[] { "spring", "summer", "fall", "winter" });
        var snapshot = new QuestPlayerSnapshot(
            FishingLevel: 0,
            KnownRecipes: new HashSet<string>
            {
                FishingNetIds.CopperNetRecipe,
                FishingNetIds.IronNetRecipe,
                FishingNetIds.GoldNetRecipe
            },
            KnownMailFlags: new HashSet<string>());

        QuestUnlockPlan plan = tracker.EvaluateUnlocks(snapshot);

        Assert.Equal(new[] { FishingNetIds.IridiumNetRecipe }, plan.RecipesToUnlock);
        Assert.Equal(new[] { FishingNetIds.IridiumQuestMailId }, plan.MailToQueue);
    }

    [Fact]
    public void EvaluateUnlocksSkipsKnownRecipesAndAlreadyQueuedMail()
    {
        var tracker = new QuestProgressTracker();
        tracker.Progress.SilverFishCount = QuestProgressTracker.SilverFishRequiredForIronNet;
        var snapshot = new QuestPlayerSnapshot(
            FishingLevel: 2,
            KnownRecipes: new HashSet<string> { FishingNetIds.CopperNetRecipe },
            KnownMailFlags: new HashSet<string> { FishingNetIds.IronQuestMailId });

        QuestUnlockPlan plan = tracker.EvaluateUnlocks(snapshot);

        Assert.Equal(new[] { FishingNetIds.IronNetRecipe }, plan.RecipesToUnlock);
        Assert.Empty(plan.MailToQueue);
    }
}
