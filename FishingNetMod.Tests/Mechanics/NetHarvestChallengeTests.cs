using FishingNetMod.Mechanics;
using Xunit;

namespace FishingNetMod.Tests.Mechanics;

public sealed class NetHarvestChallengeTests
{
    [Fact]
    public void NewChallengeStartsAtOneWithThirtySecondsRemaining()
    {
        var challenge = new NetHarvestChallenge();

        Assert.Equal(1, challenge.NextTarget);
        Assert.Equal(TimeSpan.FromSeconds(30), challenge.TimeRemaining);
        Assert.Equal(NetHarvestChallengeStatus.Running, challenge.Status);
    }

    [Fact]
    public void ClickIgnoresNumbersOutOfOrder()
    {
        var challenge = new NetHarvestChallenge(targetCount: 5);

        bool accepted = challenge.Click(2);

        Assert.False(accepted);
        Assert.Equal(1, challenge.NextTarget);
        Assert.Equal(NetHarvestChallengeStatus.Running, challenge.Status);
    }

    [Fact]
    public void ClickAdvancesThroughTargetsInOrder()
    {
        var challenge = new NetHarvestChallenge(targetCount: 5);

        Assert.True(challenge.Click(1));
        Assert.True(challenge.Click(2));

        Assert.Equal(3, challenge.NextTarget);
        Assert.Equal(NetHarvestChallengeStatus.Running, challenge.Status);
    }

    [Fact]
    public void ChallengeCompletesAfterFinalTarget()
    {
        var challenge = new NetHarvestChallenge(targetCount: 3);

        challenge.Click(1);
        challenge.Click(2);
        bool accepted = challenge.Click(3);

        Assert.True(accepted);
        Assert.Equal(NetHarvestChallengeStatus.Completed, challenge.Status);
    }

    [Fact]
    public void ChallengeFailsAfterThirtySeconds()
    {
        var challenge = new NetHarvestChallenge();

        challenge.Update(TimeSpan.FromSeconds(30));

        Assert.Equal(TimeSpan.Zero, challenge.TimeRemaining);
        Assert.Equal(NetHarvestChallengeStatus.Failed, challenge.Status);
    }

    [Fact]
    public void ChallengeAcceptsExplicitDigitSequenceIncludingZero()
    {
        var challenge = new NetHarvestChallenge(new[] { 3, 0, 7 }, TimeSpan.FromSeconds(30));

        Assert.Equal(new[] { 3, 0, 7 }, challenge.TargetNumbers);
        Assert.Equal(3, challenge.NextTargetNumber);

        Assert.True(challenge.Click(3));
        Assert.Equal(0, challenge.NextTargetNumber);
        Assert.True(challenge.Click(0));
        Assert.Equal(7, challenge.NextTargetNumber);
        Assert.True(challenge.Click(7));
        Assert.Equal(NetHarvestChallengeStatus.Completed, challenge.Status);
    }

    [Fact]
    public void ExplicitDigitSequenceRejectsWrongDigitWithoutAdvancing()
    {
        var challenge = new NetHarvestChallenge(new[] { 9, 1 }, TimeSpan.FromSeconds(30));

        Assert.False(challenge.Click(1));

        Assert.Equal(9, challenge.NextTargetNumber);
        Assert.Equal(NetHarvestChallengeStatus.Running, challenge.Status);
    }
}
