using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AntigravityMoon
{
    public class Structure : Entity
    {
        public int Width { get; set; }
        public int Height { get; set; }

        // Farming
        public bool IsGrowing { get; private set; }
        public float GrowthTimer { get; private set; }
        public string CropType { get; private set; }
        public bool IsReadyToHarvest => ReadyCount > 0;
        public int PlantedCount { get; private set; }
        public int ReadyCount { get; private set; }
        public int MaxPlantedCount { get; private set; }
        
        // Repair System
        public int RepairStage { get; set; } = 0;

        public Structure(Vector2 position, string type, int width, int height) 
            : base(position, type, false, false, true) // Default: Solid, Not Harvestable
        {
            Width = width;
            Height = height;

            // Loot Types (Rock, Crystal) should be harvestable and not solid
            if (type == "Rock" || type == "Crystal")
            {
                IsHarvestable = true;
                IsSolid = false;
            }
        }

        public void UpgradeRepairStage()
        {
            RepairStage++;
        }

        public float MaxGrowthTimer { get; private set; } // The target time for a crop to grow

        public void StartGrowing(string crop, float maxGrowthTime = 10f)
        {
            if (PlantedCount == 0 && ReadyCount == 0) // Only reset if nothing is currently in the queue or ready
            {
                MaxPlantedCount = 0;
            }
            
            PlantedCount++;
            MaxPlantedCount++;
            CropType = crop;
            if (!IsGrowing)
            {
                IsGrowing = true;
                GrowthTimer = 0f;
                MaxGrowthTimer = maxGrowthTime;
            }
        }

        public void Update(float dt)
        {
            if (IsGrowing)
            {
                GrowthTimer += dt;
                if (GrowthTimer >= MaxGrowthTimer) // Dynamic growth time

                {
                    PlantedCount--;
                    ReadyCount++;
                    
                    if (PlantedCount > 0)
                    {
                        GrowthTimer -= MaxGrowthTimer;
                    }
                    else
                    {
                        IsGrowing = false;
                        GrowthTimer = 0f;
                        // Keep MaxPlantedCount as is until Harvest fully clears it or a new batch starts
                    }
                }
            }
        }

        public string Harvest()
        {
            if (ReadyCount > 0)
            {
                ReadyCount--;
                return CropType;
            }
            return null;
        }

        public override Rectangle GetBounds()
        {
            int w = Width > 0 ? Width : 32;
            int h = Height > 0 ? Height : 32;
            return new Rectangle((int)Position.X, (int)Position.Y, w, h);
        }

        public override void Draw(SpriteBatch spriteBatch, Texture2D texture, Vector2 mouseWorldPos)
        {
            Rectangle bounds = GetBounds();

            Color color = Color.White;
            // if (Type == "Greenhouse") color = Color.LimeGreen; // Removed tint
            // else if (Type == "Workbench") color = Color.Brown; // Removed tint

            spriteBatch.Draw(texture, bounds, color);
            
            // Draw Label only if hovering
            if (bounds.Contains(mouseWorldPos) && !string.IsNullOrEmpty(Type))
            {
                PixelTextRenderer.DrawText(spriteBatch, texture, Type, new Vector2(Position.X, Position.Y - 10), Color.White, 1);
            }
        }
    }
}
