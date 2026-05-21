using FishingNetMod.Mechanics;
using Microsoft.Xna.Framework;
using StardewValley;
using Xunit;

namespace FishingNetMod.Tests.Mechanics;

public sealed class VanillaFishProviderTests
{
    [Fact]
    public void VanillaFishProviderImplementsFishProvider()
    {
        IFishProvider provider = new VanillaFishProvider();

        Assert.IsAssignableFrom<IFishProvider>(provider);
    }

    [Fact]
    public void FishProviderDefinesLocationPlayerAndTargetTileInputs()
    {
        Type interfaceType = typeof(IFishProvider);

        var method = interfaceType.GetMethod(nameof(IFishProvider.GetFish));

        Assert.NotNull(method);
        Assert.Equal(typeof(Item), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Collection(
            parameters,
            parameter => Assert.Equal(typeof(GameLocation), parameter.ParameterType),
            parameter => Assert.Equal(typeof(Farmer), parameter.ParameterType),
            parameter => Assert.Equal(typeof(Vector2), parameter.ParameterType));
    }
}
