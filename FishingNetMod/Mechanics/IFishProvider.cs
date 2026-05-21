using Microsoft.Xna.Framework;
using StardewValley;

namespace FishingNetMod.Mechanics;

internal interface IFishProvider
{
    Item? GetFish(GameLocation location, Farmer player, Vector2 targetTile);
}
