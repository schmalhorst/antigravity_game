using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace AntigravityMoon
{
    public class Alien : Entity
    {
        public const float AggroRadius = 150f;
        public const float DeaggroRadius = 600f;

        public float Speed { get; set; } = 100f;
        public float WanderSpeed { get; set; } = 35f;
        public int HitsTaken { get; private set; } = 0;
        public float DamageCooldown { get; private set; } = 0f;
        public bool IsDead { get; private set; } = false;
        public float Health { get; private set; } = 100f;
        public bool IsAggroed { get; private set; } = false;

        public int Width { get; private set; }
        public int Height { get; private set; }

        private Vector2 _wanderDirection = Vector2.Zero;
        private float _wanderTimer = 0f;
        private bool _isIdling = false;
        private Vector2 _spawnPosition;
        private static readonly Random _alienRandom = new Random();

        public Alien(Vector2 position, string alienType = "metro_alien") 
            : base(position, alienType, true, false, false) // Movable, Not Harvestable, Not Solid (so it can overlap/hit)
        {
            _spawnPosition = position;
            if (alienType == "the_destroyer")
            {
                Width = 64;
                Height = 64;
                Speed = 110f;
            }
            else
            {
                Width = 128;
                Height = 64;
                Speed = 95f;
            }

            // Stagger initial wander state so mobs don't all move in unison
            _wanderTimer = (float)(_alienRandom.NextDouble() * 2.0);
            PickNewWanderState();
        }

        private void PickNewWanderState()
        {
            // 35% chance to idle, 65% chance to wander
            if (_alienRandom.NextDouble() < 0.35)
            {
                _isIdling = true;
                _wanderDirection = Vector2.Zero;
                _wanderTimer = (float)(_alienRandom.NextDouble() * 1.5 + 1.0); // Idle 1.0 - 2.5s
            }
            else
            {
                _isIdling = false;
                // If mob has wandered far from spawn position, steer back towards spawn
                if (Vector2.Distance(Position, _spawnPosition) > 350f)
                {
                    Vector2 dirToSpawn = _spawnPosition - Position;
                    if (dirToSpawn != Vector2.Zero)
                    {
                        dirToSpawn.Normalize();
                        float jitter = (float)((_alienRandom.NextDouble() - 0.5) * 0.6);
                        _wanderDirection = Vector2.Transform(dirToSpawn, Matrix.CreateRotationZ(jitter));
                    }
                    else
                    {
                        float angle = (float)(_alienRandom.NextDouble() * Math.PI * 2);
                        _wanderDirection = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                    }
                }
                else
                {
                    float angle = (float)(_alienRandom.NextDouble() * Math.PI * 2);
                    _wanderDirection = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                }
                _wanderTimer = (float)(_alienRandom.NextDouble() * 2.5 + 1.5); // Move 1.5 - 4.0s
            }
        }

        private bool IsPositionBlocked(Vector2 pos, EntityManager entityManager, TileMap tileMap)
        {
            Rectangle alienRect = new Rectangle((int)pos.X, (int)pos.Y, Width, Height);

            if (entityManager != null)
            {
                foreach (var entity in entityManager.GetEntities())
                {
                    if (entity.IsSolid && alienRect.Intersects(entity.GetBounds()))
                    {
                        return true;
                    }
                }
            }

            if (tileMap != null)
            {
                int minTileX = (int)Math.Floor(pos.X / TileMap.TileSize);
                int maxTileX = (int)Math.Floor((pos.X + Width - 1) / TileMap.TileSize);
                int minTileY = (int)Math.Floor(pos.Y / TileMap.TileSize);
                int maxTileY = (int)Math.Floor((pos.Y + Height - 1) / TileMap.TileSize);

                for (int tx = minTileX; tx <= maxTileX; tx++)
                {
                    for (int ty = minTileY; ty <= maxTileY; ty++)
                    {
                        int tile = tileMap.GetTile(tx, ty);
                        if (tile == 2 || tile == 5 || tile == 7) // Crater rim wall, Cave wall, Lava
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private void MoveWithCollision(Vector2 moveVec, EntityManager entityManager, TileMap tileMap)
        {
            Vector2 nextPos = Position + moveVec;
            if (!IsPositionBlocked(nextPos, entityManager, tileMap))
            {
                Position = nextPos;
                return;
            }

            // Sliding collision: try moving only along X
            if (moveVec.X != 0)
            {
                Vector2 nextPosX = new Vector2(Position.X + moveVec.X, Position.Y);
                if (!IsPositionBlocked(nextPosX, entityManager, tileMap))
                {
                    Position = nextPosX;
                    return;
                }
            }

            // Sliding collision: try moving only along Y
            if (moveVec.Y != 0)
            {
                Vector2 nextPosY = new Vector2(Position.X, Position.Y + moveVec.Y);
                if (!IsPositionBlocked(nextPosY, entityManager, tileMap))
                {
                    Position = nextPosY;
                    return;
                }
            }
        }

        public void Update(float dt, Player player, EntityManager entityManager, TileMap tileMap = null)
        {
            if (IsDead) return;

            // Cooldown Management
            if (DamageCooldown > 0)
            {
                DamageCooldown -= dt;
            }

            float distanceToPlayer = Vector2.Distance(Position, player.Position);

            // Aggro State Management
            if (!IsAggroed)
            {
                // Aggro if player steps within proximity detection radius
                if (distanceToPlayer <= AggroRadius)
                {
                    IsAggroed = true;
                }
            }
            else
            {
                // De-aggro if player escapes beyond de-aggro distance
                if (distanceToPlayer > DeaggroRadius)
                {
                    IsAggroed = false;
                    PickNewWanderState();
                }
            }

            if (IsAggroed)
            {
                // Chase Player
                Vector2 direction = player.Position - Position;
                if (direction != Vector2.Zero)
                {
                    direction.Normalize();
                    MoveWithCollision(direction * Speed * dt, entityManager, tileMap);
                }

                // Attack Player only when aggroed
                if (distanceToPlayer < 36f)
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
            else
            {
                // Peaceful Wandering Logic
                _wanderTimer -= dt;
                if (_wanderTimer <= 0)
                {
                    PickNewWanderState();
                }

                if (!_isIdling && _wanderDirection != Vector2.Zero)
                {
                    Vector2 prevPos = Position;
                    MoveWithCollision(_wanderDirection * WanderSpeed * dt, entityManager, tileMap);
                    // If blocked by obstacle, pick a new direction
                    if (Position == prevPos)
                    {
                        PickNewWanderState();
                    }
                }
            }
        }

        public void TakeDamage(float amount)
        {
            IsAggroed = true; // Aggro immediately when attacked
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
            // Draw alien using assigned dimensions
            Rectangle bounds = new Rectangle((int)Position.X, (int)Position.Y, Width, Height);
            spriteBatch.Draw(texture, bounds, Color.White);
            
            // Draw HP Bar
            int barWidth = Math.Min(100, Width);
            int barHeight = 8;
            int barX = (int)Position.X + (Width - barWidth) / 2;
            int barY = (int)Position.Y - 18;
            
            // Background
            Color bgColor = IsAggroed ? new Color(60, 15, 15, 220) : new Color(30, 30, 30, 180);
            spriteBatch.Draw(texture, new Rectangle(barX, barY, barWidth, barHeight), new Rectangle(0,0,1,1), bgColor);
            
            // Foreground (LimeGreen when neutral, OrangeRed when hostile)
            int currentHealthWidth = (int)(barWidth * (Health / 100f));
            Color hpColor = IsAggroed ? Color.OrangeRed : Color.LimeGreen;
            spriteBatch.Draw(texture, new Rectangle(barX, barY, currentHealthWidth, barHeight), new Rectangle(0,0,1,1), hpColor);

            // Hostile Alert Icon above HP bar
            if (IsAggroed)
            {
                PixelTextRenderer.DrawText(spriteBatch, texture, "!", new Vector2(Position.X + Width / 2f - 4, barY - 16), Color.Red, 1.2f);
            }

            // Draw Label with status only if hovering
            if (bounds.Contains(mouseWorldPos))
            {
                string status = IsAggroed ? "HOSTILE" : "NEUTRAL";
                Color statusColor = IsAggroed ? Color.OrangeRed : Color.LightGreen;
                string label = $"{Type} [{status}]";
                PixelTextRenderer.DrawText(spriteBatch, texture, label, new Vector2(Position.X, barY - (IsAggroed ? 28 : 16)), statusColor, 0.9f);
            }
        }
    }
}
