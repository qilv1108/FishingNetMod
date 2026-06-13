using FishingNetMod.Data;
using StardewModdingAPI;
using StardewValley;

namespace FishingNetMod.Mechanics;

internal sealed record ActiveFishingNetCast(NetLevelData NetData, IReadOnlyList<Item> CaughtItems, int Attempts)
{
    public int CaughtCount => this.CaughtItems.Count;

    public string GetResultMessage(ITranslationHelper? translation = null)
    {
        if (this.CaughtCount > 0)
        {
            string msg = translation?.Get("cast.caught")
                .Tokens(new { count = this.CaughtCount }).ToString()
                ?? $"捕获了 {this.CaughtCount} 条鱼！";
            return msg;
        }

        return translation?.Get("cast.no-fish").ToString() ?? "没有捕到鱼。";
    }
}
