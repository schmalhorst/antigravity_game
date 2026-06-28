using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AntigravityMoon
{
    public class Entity
    {
        public Vector2 Position { get; set; }
        public string Type { get; set; } // "Rock", "Structure", "Item"
        public bool IsMovable { get; set; }
        public bool IsHarvestable { get; set; }
        public bool IsSolid { get; set; }
        public float Rotation { get; set; } = 0f;
        public Color TintColor { get; set; } = Color.White;

        public Entity(Vector2 position, string type, bool movable, bool harvestable, bool solid = false)
        {
            Position = position;
            Type = type;
            IsMovable = movable;
            IsHarvestable = harvestable;
            IsSolid = solid;
        }

        public virtual Rectangle GetBounds()
        {
            return new Rectangle((int)Position.X, (int)Position.Y, 32, 32);
        }

        public virtual void Draw(SpriteBatch spriteBatch, Texture2D texture, Vector2 mouseWorldPos)
        {
            Rectangle bounds = GetBounds();
            Color color = TintColor;
            
            if (color == Color.White)
            {
                if (Type == "Ice Crystal") color = Color.LightCyan;
                else if (Type == "Volcanic Ore") color = Color.Red;
                else if (Type == "Radioactive Slag") color = Color.Green;
            }

            if (Rotation != 0f)
            {
                Vector2 center = new Vector2(Position.X + 16f, Position.Y + 16f); // Entity default size is 32x32
                spriteBatch.Draw(
                    texture,
                    new Rectangle((int)center.X, (int)center.Y, 32, 32),
                    null,
                    color,
                    Rotation,
                    new Vector2(texture.Width / 2f, texture.Height / 2f),
                    SpriteEffects.None,
                    0f
                );
            }
            else
            {
                spriteBatch.Draw(texture, bounds, color);
            }
            
            // Draw Label only if hovering
            if (bounds.Contains(mouseWorldPos))
            {
                PixelTextRenderer.DrawText(spriteBatch, texture, Type, new Vector2(Position.X, Position.Y - 10), Color.White, 1);
            }
        }
    }
}
