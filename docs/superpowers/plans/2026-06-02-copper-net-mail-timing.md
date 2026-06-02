# Copper Net Mail Timing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the copper fishing net intro mail trigger at fishing level 1 while arriving through Stardew Valley's normal next-morning mail flow.

**Architecture:** Keep `QuestProgressTracker` as the single source of unlock decisions, but remove its immediate-mail output. `ModEntry` should only unlock recipes and queue mail in `player.mailForTomorrow`; no path should add `FishingNet_WillyRequest` to `player.mailbox`.

**Tech Stack:** C# net6.0, SMAPI, Stardew Valley APIs, xUnit, `dotnet test`.

---

## File Structure

- Modify `FishingNetMod.Tests/Quests/QuestProgressTrackerTests.cs`: encode the conventional mail expectations and remove tests for same-day copper mail delivery.
- Modify `FishingNetMod/Quests/QuestUnlockPlan.cs`: remove `MailToDeliverNow` from the unlock plan model.
- Modify `FishingNetMod/Quests/QuestProgressTracker.cs`: remove the `deliverCopperMailNow` parameter and route all copper mail starts through `MailToQueue`.
- Modify `FishingNetMod/ModEntry.cs`: call `EvaluateUnlocks` without an immediate-delivery option and remove the mailbox insertion loop.

### Task 1: Write Failing Quest Tests

**Files:**
- Modify: `FishingNetMod.Tests/Quests/QuestProgressTrackerTests.cs`
- Test: `FishingNetMod.Tests/Quests/QuestProgressTrackerTests.cs`

- [ ] **Step 1: Replace the copper mail timing tests**

In `FishingNetMod.Tests/Quests/QuestProgressTrackerTests.cs`, make these exact test changes:

1. In `EvaluateUnlocksQueuesCopperQuestMailForTomorrowAtFishingLevelOneBeforeUnlockingCopperRecipe`, remove the assertion against `plan.MailToDeliverNow`.
2. Delete `EvaluateUnlocksCanDeliverCopperQuestMailNowAtFishingLevelOne`.
3. Keep the method name `EvaluateUnlocksUnlocksCopperRecipeAfterMailArrivesAtFishingLevelOne`, and call `tracker.EvaluateUnlocks(snapshot)` without a second argument.
4. Keep `CopperQuestFishingLevelRequirementIsTemporarilyOne`.
5. In `EvaluateUnlocksDoesNotStartCopperQuestAtFishingLevelZero`, call `tracker.EvaluateUnlocks(snapshot)` without a second argument and remove the assertion against `plan.MailToDeliverNow`.
6. Replace `EvaluateUnlocksRedeliversCopperQuestMailIfQueuedFlagExistsButMailIsMissing` with this test:

```csharp
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
```

7. Delete `EvaluateUnlocksDeliversCopperQuestMailNowWhenOnlyQueuedForTomorrow`.
8. Add this test after the level requirement test:

```csharp
[Fact]
public void QuestUnlockPlanDoesNotExposeImmediateMailDelivery()
{
    Assert.Null(typeof(QuestUnlockPlan).GetProperty("MailToDeliverNow"));
}
```

- [ ] **Step 2: Run quest tests and verify the intended failure**

Run:

```powershell
dotnet test FishingNetMod.Tests\FishingNetMod.Tests.csproj --filter QuestProgressTrackerTests
```

Expected: FAIL. `QuestUnlockPlanDoesNotExposeImmediateMailDelivery` fails because `QuestUnlockPlan.MailToDeliverNow` still exists.

### Task 2: Remove Immediate Copper Mail Delivery

**Files:**
- Modify: `FishingNetMod/Quests/QuestUnlockPlan.cs`
- Modify: `FishingNetMod/Quests/QuestProgressTracker.cs`
- Modify: `FishingNetMod/ModEntry.cs`
- Test: `FishingNetMod.Tests/Quests/QuestProgressTrackerTests.cs`

- [ ] **Step 1: Simplify `QuestUnlockPlan`**

Replace the entire contents of `FishingNetMod/Quests/QuestUnlockPlan.cs` with:

```csharp
namespace FishingNetMod.Quests;

internal sealed record QuestUnlockPlan(IReadOnlyList<string> RecipesToUnlock, IReadOnlyList<string> MailToQueue)
{
    public bool HasChanges => this.RecipesToUnlock.Count > 0 || this.MailToQueue.Count > 0;
}
```

- [ ] **Step 2: Remove the immediate-delivery parameter from `QuestProgressTracker`**

In `FishingNetMod/Quests/QuestProgressTracker.cs`, replace `EvaluateUnlocks` with:

```csharp
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
```

Replace `AddCopperUnlockIfNeeded` with:

```csharp
private void AddCopperUnlockIfNeeded(
    QuestPlayerSnapshot player,
    List<string> recipesToUnlock,
    List<string> mailToQueue)
{
    if (player.KnownRecipes.Contains(FishingNetIds.CopperNetRecipe))
        return;

    if (player.FishingLevel < FishingLevelRequiredForCopperQuest)
        return;

    IReadOnlySet<string> receivedMailFlags = player.ReceivedMailFlags ?? player.KnownMailFlags;

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
```

- [ ] **Step 3: Remove mailbox insertion from `ModEntry`**

In `FishingNetMod/ModEntry.cs`, update the `ApplyQuestUnlocks` call sites in `OnSaveLoaded`, `OnDayStarted`, `OnDayEnding`, and `OnUpdateTicked`:

```csharp
this.ApplyQuestUnlocks(Game1.player);
```

Keep the `OnUpdateTicked` event registration in `Entry`, and make the method call the same no-argument unlock path:

```csharp
private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
{
    if (!Context.IsWorldReady || !e.IsMultipleOf(60))
        return;

    this.ApplyQuestUnlocks(Game1.player);
}
```

Replace `ApplyQuestUnlocks` with:

```csharp
private void ApplyQuestUnlocks(Farmer player)
{
    QuestUnlockPlan plan = this.questProgressTracker!.EvaluateUnlocks(QuestPlayerSnapshot.FromFarmer(player));
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
```

- [ ] **Step 4: Run quest tests and verify they pass**

Run:

```powershell
dotnet test FishingNetMod.Tests\FishingNetMod.Tests.csproj --filter QuestProgressTrackerTests
```

Expected: PASS. The quest tracker should no longer expose or use immediate mail delivery.

### Task 3: Verify Full Suite and Commit

**Files:**
- Verify: `FishingNetMod.Tests/FishingNetMod.Tests.csproj`
- Commit: `FishingNetMod.Tests/Quests/QuestProgressTrackerTests.cs`, `FishingNetMod/Quests/QuestUnlockPlan.cs`, `FishingNetMod/Quests/QuestProgressTracker.cs`, `FishingNetMod/ModEntry.cs`, `docs/superpowers/plans/2026-06-02-copper-net-mail-timing.md`

- [ ] **Step 1: Run the full test suite**

Run:

```powershell
dotnet test FishingNetMod.Tests\FishingNetMod.Tests.csproj
```

Expected: PASS. Existing item, data, mechanics, menu, quest, and structure tests all pass.

- [ ] **Step 2: Inspect the final diff**

Run:

```powershell
git diff -- FishingNetMod.Tests/Quests/QuestProgressTrackerTests.cs FishingNetMod/Quests/QuestUnlockPlan.cs FishingNetMod/Quests/QuestProgressTracker.cs FishingNetMod/ModEntry.cs docs/superpowers/plans/2026-06-02-copper-net-mail-timing.md
```

Expected: The diff removes `MailToDeliverNow`, removes `deliverCopperMailNow`, keeps `FishingLevelRequiredForCopperQuest = 1`, and leaves mail delivery as `player.mailForTomorrow.Add(mailId)`.

- [ ] **Step 3: Commit the implementation**

Run:

```powershell
git add FishingNetMod.Tests/Quests/QuestProgressTrackerTests.cs FishingNetMod/Quests/QuestUnlockPlan.cs FishingNetMod/Quests/QuestProgressTracker.cs FishingNetMod/ModEntry.cs docs/superpowers/plans/2026-06-02-copper-net-mail-timing.md
git commit -m "fix: use normal mail timing for copper fishing net"
```

Expected: A commit containing the plan, tests, and code changes for conventional copper net mail timing.
