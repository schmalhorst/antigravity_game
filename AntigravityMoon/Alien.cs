using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace AntigravityMoon
{
    public class Alien : Entity
    {
        public float Speed { get; set; } = 100f;
        public int HitsTaken { get; private set; } = 0;
        public float DamageCooldown { get; private set; } = 0f;
        public bool IsDead { get; private set; } = false;
        public float Health { get; private set; } = 100f;

        public Alien(Vector2 position) 
            : base(position, "Alien", true, false, false) // Movable, Not Harvestable, Not Solid (so it can overlap/hit)
        {
        }

        public void Update(float dt, Player player)
        {
            if (IsDead) return;

            // Chase Player
            Vector2 direction = player.Position - Position;
            if (direction != Vector2.Zero)
            {
                direction.Normalize();
                Position += direction * Speed * dt;
            }

            // Cooldown Management
            if (DamageCooldown > 0)
            {
                DamageCooldown -= dt;
            }

            // Collision Logic (Simple distance check)
            float distance = Vector2.Distance(Position, player.Position);
            if (distance < 32) // Overlap
            {
                if (DamageCooldown <= 0)
                {
                    // Hit Player
                    player.TakeDamage(10f); // 10% damage
                    HitsTaken++;
                    DamageCooldown = 1.0f; // 1 second cooldown

                    if (HitsTaken >= 3)
                    {
                        Explode(player);
                    }
                }
            }
        }

        public void TakeDamage(float amount)
        {
            Health -= amount;
            if (Health <= 0)
            {
                Health = 0;
                IsDead = true;
            }
        }

        private void Explode(Player player)
        {
            IsDead = true;
            // Explosion Logic
            // If player is close, kill them
            if (Vector2.Distance(Position, player.Position) < 50)
            {
                player.TakeDamage(100f); // Kill
            }
        }

        public override void Draw(SpriteBatch spriteBatch, Texture2D texture, Vector2 mouseWorldPos)
        {
            // Draw alien wider - 64x32
            Rectangle bounds = new Rectangle((int)Position.X, (int)Position.Y, 128, 64);
            spriteBatch.Draw(texture, bounds, Color.White);
            
            // Draw Label only if hovering
            if (bounds.Contains(mouseWorldPos))
            {
                PixelTextRenderer.DrawText(spriteBatch, texture, Type, new Vector2(Position.X, Position.Y - 10), Color.White, 1);
            }
        }
    }
}
