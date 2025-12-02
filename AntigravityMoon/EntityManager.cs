using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace AntigravityMoon
{
    public class EntityManager
    {
        private List<Entity> _entities;

        public EntityManager()
        {
            _entities = new List<Entity>();
        }

        public void AddEntity(Entity entity)
        {
            _entities.Add(entity);
        }

        public void RemoveEntity(Entity entity)
        {
            _entities.Remove(entity);
        }

        public List<Entity> GetEntities()
        {
            return _entities;
        }

        public void Draw(SpriteBatch spriteBatch, Dictionary<string, Texture2D> textures, Vector2 mouseWorldPos, TileMap tileMap)
        {
            foreach (var entity in _entities)
            {
                // Check if entity is in explored area
                int tileX = (int)System.Math.Floor(entity.Position.X / TileMap.TileSize);
                int tileY = (int)System.Math.Floor(entity.Position.Y / TileMap.TileSize);
                
                if (!tileMap.IsExplored(tileX, tileY))
                {
                    continue; // Skip drawing unexplored entities
                }
                
                string key = entity.Type.ToLower();
                if (textures.ContainsKey(key))
                {
                    entity.Draw(spriteBatch, textures[key], mouseWorldPos);
                }
                else
                {
                    // Fallback
                    entity.Draw(spriteBatch, textures["Pixel"], mouseWorldPos);
                }
            }
        }
    }
}
