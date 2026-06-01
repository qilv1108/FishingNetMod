using FishingNetMod.Data;
using StardewValley;

namespace FishingNetMod.Mechanics;

internal sealed record ActiveFishingNetCast(NetLevelData NetData, IReadOnlyList<Item> CaughtItems, int Attempts)
{
    public int CaughtCount => this.CaughtItems.Count;

    public string GetResultMessage()
    {
        return this.CaughtCount > 0 ? $"捕获了 {this.CaughtCount} 条鱼！" : "没有捕到鱼。";
    }
}
