using FishingNetMod.Data;
using StardewValley;
using SObject = StardewValley.Object;

namespace FishingNetMod.Items;

public sealed class FishingNetItemFactory
{
    public const string NetLevelModDataKey = "ChenJianCan.FishingNetMod/NetLevel";

    private readonly Func<string, int, Item> createItem;

    public FishingNetItemFactory()
        : this((qualifiedItemId, stack) => ItemRegistry.Create(qualifiedItemId, stack))
    {
    }

    internal FishingNetItemFactory(Func<string, int, Item> createItem)
    {
        this.createItem = createItem;
    }

    public Item Create(NetLevelData data)
    {
        Item item;
        try
        {
            item = this.createItem($"(O){data.ItemId}", 1);
        }
        catch
        {
            item = new SObject("771", 1);
        }

        item.modData[NetLevelModDataKey] = data.CommandName;
        item.Name = data.DisplayName;
        return item;
    }

    public bool TryGetNetData(Item? item, out NetLevelData? data)
    {
        data = null;

        if (item is null)
            return false;

        if (!item.modData.TryGetValue(NetLevelModDataKey, out string? value))
            return TryGetNetDataQualifiedItemId(item.QualifiedItemId, out data);

        return TryGetNetDataValue(value, out data);
    }

    public static bool TryGetNetDataValue(string? value, out NetLevelData? data)
    {
        return NetLevelData.TryParse(value, out data);
    }

    public static bool TryGetNetDataQualifiedItemId(string? qualifiedItemId, out NetLevelData? data)
    {
        return NetLevelData.TryParseItemId(qualifiedItemId, out data);
    }
}
