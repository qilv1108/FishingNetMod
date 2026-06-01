namespace FishingNetMod.Mechanics;

internal enum NetHarvestChallengeStatus
{
    Running,
    Completed,
    Failed
}

internal sealed class NetHarvestChallenge
{
    private readonly IReadOnlyList<int> targetNumbers;
    private TimeSpan timeRemaining;
    private int nextTargetIndex;

    public NetHarvestChallenge(int targetCount = 5, TimeSpan? duration = null)
        : this(Enumerable.Range(1, Math.Max(1, targetCount)).ToArray(), duration)
    {
    }

    public NetHarvestChallenge(IReadOnlyList<int> targetNumbers, TimeSpan? duration = null)
    {
        this.targetNumbers = targetNumbers.Count > 0 ? targetNumbers.ToArray() : new[] { 1 };
        this.timeRemaining = duration ?? TimeSpan.FromSeconds(30);
    }

    public int NextTarget => this.nextTargetIndex + 1;

    public int NextTargetNumber => this.Status == NetHarvestChallengeStatus.Running
        ? this.targetNumbers[this.nextTargetIndex]
        : -1;

    public int TargetCount => this.targetNumbers.Count;

    public IReadOnlyList<int> TargetNumbers => this.targetNumbers;

    public TimeSpan TimeRemaining => this.timeRemaining < TimeSpan.Zero ? TimeSpan.Zero : this.timeRemaining;

    public NetHarvestChallengeStatus Status { get; private set; } = NetHarvestChallengeStatus.Running;

    public bool Click(int number)
    {
        if (this.Status != NetHarvestChallengeStatus.Running)
            return false;

        if (number != this.NextTargetNumber)
            return false;

        this.nextTargetIndex++;
        if (this.nextTargetIndex >= this.targetNumbers.Count)
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
