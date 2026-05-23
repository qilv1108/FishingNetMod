namespace FishingNetMod.Mechanics;

internal enum NetHarvestChallengeStatus
{
    Running,
    Completed,
    Failed
}

internal sealed class NetHarvestChallenge
{
    private readonly int targetCount;
    private TimeSpan timeRemaining;
    private int nextTarget = 1;

    public NetHarvestChallenge(int targetCount = 5, TimeSpan? duration = null)
    {
        this.targetCount = Math.Max(1, targetCount);
        this.timeRemaining = duration ?? TimeSpan.FromSeconds(30);
    }

    public int NextTarget => this.nextTarget;

    public int TargetCount => this.targetCount;

    public TimeSpan TimeRemaining => this.timeRemaining < TimeSpan.Zero ? TimeSpan.Zero : this.timeRemaining;

    public NetHarvestChallengeStatus Status { get; private set; } = NetHarvestChallengeStatus.Running;

    public bool Click(int number)
    {
        if (this.Status != NetHarvestChallengeStatus.Running)
            return false;

        if (number != this.nextTarget)
            return false;

        this.nextTarget++;
        if (this.nextTarget > this.targetCount)
            this.Status = NetHarvestChallengeStatus.Completed;

        return true;
    }

    public void Update(TimeSpan elapsed)
    {
        if (this.Status != NetHarvestChallengeStatus.Running)
            return;

        this.timeRemaining -= elapsed;
        if (this.timeRemaining <= TimeSpan.Zero)
        {
            this.timeRemaining = TimeSpan.Zero;
            this.Status = NetHarvestChallengeStatus.Failed;
        }
    }
}
