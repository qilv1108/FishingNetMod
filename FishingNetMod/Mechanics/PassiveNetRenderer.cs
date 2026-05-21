using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace FishingNetMod.Mechanics;

internal sealed class PassiveNetRenderer
{
    public void Draw(SpriteBatch spriteBatch, IEnumerable<PassiveNetData> nets)
    {
        foreach (PassiveNetData net in nets)
        {
            if (Game1.currentLocation?.Name != net.LocationName)
                continue;

            Vector2 position = Game1.GlobalToLocal(Game1.viewport, net.Tile * Game1.tileSize);
            Rectangle source = Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, 771, 16, 16);
            spriteBatch.Draw(Game1.objectSpriteSheet, position, source, Color.White, 0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 1f);
        }
    }
}
