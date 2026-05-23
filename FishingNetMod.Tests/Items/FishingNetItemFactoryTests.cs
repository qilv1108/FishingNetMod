using FishingNetMod.Data;
using FishingNetMod.Items;
using Xunit;

namespace FishingNetMod.Tests.Items;

public sealed class FishingNetItemFactoryTests
{
    [Fact]
    public void NetLevelModDataKeyUsesModNamespace()
    {
        Assert.Equal("ChenJianCan.FishingNetMod/NetLevel", FishingNetItemFactory.NetLevelModDataKey);
    }

    [Fact]
    public void TryGetNetDataValueReadsKnownLevel()
    {
        bool found = FishingNetItemFactory.TryGetNetDataValue("gold", out NetLevelData? data);

        Assert.True(found);
        Assert.NotNull(data);
        Assert.Equal(NetLevel.Gold, data.Level);
    }

    [Fact]
    public void TryGetNetDataValueRejectsUnknownLevel()
    {
        bool found = FishingNetItemFactory.TryGetNetDataValue("diamond", out NetLevelData? data);

        Assert.False(found);
        Assert.Null(data);
    }

    [Fact]
    public void TryGetNetDataQualifiedItemIdRecognizesContentPatcherObjects()
    {
        bool found = FishingNetItemFactory.TryGetNetDataQualifiedItemId($"(O){FishingNetIds.IridiumNetItemId}", out NetLevelData? data);

        Assert.True(found);
        Assert.NotNull(data);
        Assert.Equal(NetLevel.Iridium, data.Level);
    }
}
