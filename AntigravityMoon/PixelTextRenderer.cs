using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AntigravityMoon
{
    public static class PixelTextRenderer
    {
        public static SpriteFont Font { get; set; }

        public static void Init()
        {
            // Initialization handled via Content.Load in Game1.cs
        }

        public static void DrawText(SpriteBatch spriteBatch, Texture2D texture, string text, Vector2 position, Color color, float scale = 1f)
        {
            if (Font != null)
            {
                // Adjust scale downward because SpriteFont's baseline 24pt size is much taller than the original 3x5 pixel map. 
                // Increased multiplier from 0.2x to 0.45x because VT323 renders visually smaller than standard fonts at the same point-size.
                float actualScale = scale * 0.45f;
                // Add uppercase conversion for stylistic conformity with original pixel font design
                spriteBatch.DrawString(Font, text.ToUpper(), position, color, 0f, Vector2.Zero, actualScale, SpriteEffects.None, 0f);
            }
        }
    }
}
