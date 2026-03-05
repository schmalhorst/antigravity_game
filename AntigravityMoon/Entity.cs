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
            spriteBatch.Draw(texture, bounds, Color.White);
            
            // Draw Label only if hovering
            if (bounds.Contains(mouseWorldPos))
            {
                PixelTextRenderer.DrawText(spriteBatch, texture, Type, new Vector2(Position.X, Position.Y - 10), Color.White, 1);
            }
        }
    }
}
