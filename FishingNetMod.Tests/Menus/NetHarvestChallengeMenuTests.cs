using FishingNetMod.Menus;
using Microsoft.Xna.Framework.Input;
using Xunit;

namespace FishingNetMod.Tests.Menus;

public sealed class NetHarvestChallengeMenuTests
{
    [Theory]
    [InlineData(Keys.D0, 0)]
    [InlineData(Keys.D5, 5)]
    [InlineData(Keys.D9, 9)]
    [InlineData(Keys.NumPad0, 0)]
    [InlineData(Keys.NumPad4, 4)]
    [InlineData(Keys.NumPad9, 9)]
    public void TryGetDigitMapsNumberKeys(Keys key, int expectedDigit)
    {
        bool mapped = NetHarvestChallengeMenu.TryGetDigit(key, out int digit);

        Assert.True(mapped);
        Assert.Equal(expectedDigit, digit);
    }

    [Fact]
    public void TryGetDigitRejectsNonNumberKeys()
    {
        bool mapped = NetHarvestChallengeMenu.TryGetDigit(Keys.Space, out int digit);

        Assert.False(mapped);
        Assert.Equal(-1, digit);
    }
}
