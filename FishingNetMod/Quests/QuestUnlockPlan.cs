namespace FishingNetMod.Quests;

internal sealed record QuestUnlockPlan(IReadOnlyList<string> RecipesToUnlock, IReadOnlyList<string> MailToQueue)
{
    public bool HasChanges => this.RecipesToUnlock.Count > 0 || this.MailToQueue.Count > 0;
}
