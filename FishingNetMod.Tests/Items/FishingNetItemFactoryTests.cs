using FishingNetMod.Data;
using FishingNetMod.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
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

    [Fact]
    public void CreateRequestsQualifiedObjectIdForContentPatcherItems()
    {
        string? requestedItemId = null;
        int? requestedStack = null;
        var factory = new FishingNetItemFactory((qualifiedItemId, stack) =>
        {
            requestedItemId = qualifiedItemId;
            requestedStack = stack;
            return new TestItem();
        });

        factory.Create(NetLevelData.Get(NetLevel.Gold));

        Assert.Equal($"(O){FishingNetIds.GoldNetItemId}", requestedItemId);
        Assert.Equal(1, requestedStack);
    }

    private sealed class TestItem : Item
    {
        public override string TypeDefinitionId => "(O)";

        public override string DisplayName => this.Name;

        public override void drawInMenu(
            SpriteBatch spriteBatch,
            Vector2 location,
            float scaleSize,
            float transparency,
            float layerDepth,
            StackDrawType drawStackNumber,
            Color color,
            bool drawShadow)
        {
        }

        public override int maximumStackSize()
        {
            return 999;
        }

        public override string getDescription()
        {
            return string.Empty;
        }

        public override bool isPlaceable()
        {
            return false;
        }

        protected override Item GetOneNew()
        {
            return new TestItem();
        }
    }
}
