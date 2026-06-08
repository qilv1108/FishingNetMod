using FishingNetMod.Data;
using FishingNetMod.Mechanics;
using Microsoft.Xna.Framework;
using Xunit;

namespace FishingNetMod.Tests.Mechanics;

public sealed class PassiveNetRendererTests
{
    [Theory]
    [InlineData(NetLevel.Copper, 0)]
    [InlineData(NetLevel.Iron, 16)]
    [InlineData(NetLevel.Gold, 32)]
    [InlineData(NetLevel.Iridium, 48)]
    public void GetSourceRectMapsLevelToSpriteColumn(NetLevel level, int expectedX)
    {
        Rectangle source = PassiveNetRenderer.GetSourceRect(level);

        Assert.Equal(new Rectangle(expectedX, 0, 16, 16), source);
    }
}
