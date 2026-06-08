using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FishingNetMod.Data;
using StardewValley;

namespace FishingNetMod.Mechanics;

internal sealed class PassiveNetRenderer
{
    internal const string TextureAssetName = "Mods/ChenJianCan.FishingNetMod/FishingNet";

    internal static Rectangle GetSourceRect(NetLevel level)
        => new Rectangle((int)level * 16, 0, 16, 16);

    public void Draw(SpriteBatch spriteBatch, IEnumerable<PassiveNetData> nets)
    {
        Texture2D texture = Game1.content.Load<Texture2D>(TextureAssetName);

        foreach (PassiveNetData net in nets)
        {
            if (Game1.currentLocation?.Name != net.LocationName)
                continue;

            Vector2 position = Game1.GlobalToLocal(Game1.viewport, net.Tile * Game1.tileSize);
            Rectangle source = GetSourceRect(net.Level);
            spriteBatch.Draw(texture, position, source, Color.White, 0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 1f);
        }
    }
}
