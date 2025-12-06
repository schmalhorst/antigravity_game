using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AntigravityMoon
{
    public class Structure : Entity
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        // Farming
        public bool IsGrowing { get; private set; }
        public float GrowthTimer { get; private set; }
        public string CropType { get; private set; }
        public bool IsReadyToHarvest { get; private set; }
        
        // Repair System
        public int RepairStage { get; private set; } = 0;

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

        public void StartGrowing(string crop)
        {
            if (!IsGrowing && !IsReadyToHarvest)
            {
                IsGrowing = true;
                CropType = crop;
                GrowthTimer = 0f;
            }
        }

        public void Update(float dt)
        {
            if (IsGrowing)
            {
                GrowthTimer += dt;
                if (GrowthTimer >= 10f) // 10 seconds to grow
                {
                    IsGrowing = false;
                    IsReadyToHarvest = true;
                }
            }
        }

        public string Harvest()
        {
            if (IsReadyToHarvest)
            {
                IsReadyToHarvest = false;
                return CropType;
            }
            return null;
        }

        public override void Draw(SpriteBatch spriteBatch, Texture2D texture, Vector2 mouseWorldPos)
        {
            // Draw structure as a colored rectangle
            int w = Width > 0 ? Width : 32;
            int h = Height > 0 ? Height : 32;
            
            Rectangle bounds = new Rectangle((int)Position.X, (int)Position.Y, w, h);

            Color color = Color.White;
            // if (Type == "Greenhouse") color = Color.LimeGreen; // Removed tint
            // else if (Type == "Workbench") color = Color.Brown; // Removed tint

            spriteBatch.Draw(texture, bounds, color);
            
            // Draw Label only if hovering
            if (bounds.Contains(mouseWorldPos))
            {
                PixelTextRenderer.DrawText(spriteBatch, texture, Type, new Vector2(Position.X, Position.Y - 10), Color.White, 1);
            }
        }
    }
}
