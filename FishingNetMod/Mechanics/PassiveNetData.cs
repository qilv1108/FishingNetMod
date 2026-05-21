using FishingNetMod.Data;
using Microsoft.Xna.Framework;

namespace FishingNetMod.Mechanics;

internal sealed record PassiveNetData(
    long OwnerId,
    string LocationName,
    Vector2 Tile,
    NetLevel Level,
    List<PassiveNetHarvestData> Harvest);
