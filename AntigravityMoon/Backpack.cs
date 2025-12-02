using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AntigravityMoon
{
    public class Backpack : Entity
    {
        public Inventory Storage { get; private set; }

        public Backpack(Vector2 position, Inventory inventory) : base(position, "Backpack", false, false)
        {
            Storage = inventory;
        }

        public override void Draw(SpriteBatch spriteBatch, Texture2D texture, Vector2 mouseWorldPos)
        {
            if (texture != null)
            {
                spriteBatch.Draw(texture, new Rectangle((int)Position.X, (int)Position.Y, 32, 32), Color.White);
            }
            
            // Draw Label only if hovering
            Rectangle bounds = new Rectangle((int)Position.X, (int)Position.Y, 32, 32);
            if (bounds.Contains(mouseWorldPos))
            {
                // We need a pixel texture for text background if we want one, or just draw text.
                // Since we don't have pixel texture passed here easily without breaking signature, 
                // we'll rely on the caller to handle text rendering or just draw text directly if we had font.
                // But PixelTextRenderer needs a texture.
                // Let's just assume the passed texture can be used for text pixels if needed, or ignore label for now.
                // Actually, let's just use the passed texture for text if it's 1x1, but it's likely the backpack texture.
                // Wait, Entity.Draw uses 'texture' for text drawing? 
                // "PixelTextRenderer.DrawText(spriteBatch, texture, Type..." 
                // If texture is the backpack sprite, using it for text might look weird if it's not a solid block.
                // But let's stick to the pattern.
                PixelTextRenderer.DrawText(spriteBatch, texture, "Backpack", new Vector2(Position.X, Position.Y - 10), Color.White, 1);
            }
        }
    }
}
