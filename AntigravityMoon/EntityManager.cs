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

        public void Clear()
        {
            _entities.Clear();
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
                
                if (string.IsNullOrEmpty(entity.Type))
                {
                    // Fallback for nameless entities
                    if (textures != null && textures.ContainsKey("Pixel"))
                         entity.Draw(spriteBatch, textures["Pixel"], mouseWorldPos);
                    continue;
                }

                string key = entity.Type.ToLower();
                if (entity is Structure s && key == "spaceship")
                {
                    if (s.RepairStage <= 1) key = "spaceship_broken1";
                    else if (s.RepairStage == 2) key = "spaceship_broken2";
                    else if (s.RepairStage == 3) key = "spaceship_broken3";
                    else key = "spaceship";
                }

                if (textures != null && textures.ContainsKey(key))
                {
                    entity.Draw(spriteBatch, textures[key], mouseWorldPos);
                }
                else if (textures != null && textures.ContainsKey("Pixel"))
                {
                    // Fallback
                    entity.Draw(spriteBatch, textures["Pixel"], mouseWorldPos);
                }
            }
        }
    }
}
