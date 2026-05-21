using Microsoft.Xna.Framework;
using StardewValley;

namespace FishingNetMod.Mechanics;

internal sealed class VanillaFishProvider : IFishProvider
{
    public Item? GetFish(GameLocation location, Farmer player, Vector2 targetTile)
    {
        const float millisecondsAfterNibble = 0f;
        const string bait = "0";
        const int waterDepth = 5;
        const double baitPotency = 0.0;

        return location.getFish(millisecondsAfterNibble, bait, waterDepth, player, baitPotency, targetTile, location.Name) as Item;
    }
}
