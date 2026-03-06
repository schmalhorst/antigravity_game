using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace AntigravityMoon
{
    public enum DeathCause
    {
        None,
        Oxygen,
        Hunger,
        Alien
    }

    public class Player
    {
        public Vector2 Position { get; set; }
        public Inventory Inventory { get; private set; }
        private float _speed = 200f;
        private Entity _heldEntity; // For antigravity tool
        
        public event Action OnInventoryFull;
        
        // Build Mode
        public bool IsBuildMode { get; private set; }
        private string _selectedStructure = "Greenhouse"; // Default

        private bool _prevBuildKeyPressed = false;
        private Vector2 _currentMouseWorldPos;
        private MouseState _prevMouseState;

        public float Hunger { get; set; } = 100f;
        public float Health { get; private set; } = 100f;
        public float Oxygen { get; set; } = 100f;
        public DeathCause LastDeathCause { get; private set; } = DeathCause.None;
        public bool IsDead { get; private set; } = false;
        private float _hungerDecayRate = 3.0f; // Per second
        private float _oxygenDecayRate = 0.5f; // Per second (200s supply)
        private Vector2 _spawnPoint;

        public int SuitLevel { get; set; } = 1;

        public class ActiveBuff
        {
            public int HungerPercentage { get; set; }
            public int OxygenPercentage { get; set; }
            public float TimeRemaining { get; set; }
        }

        private List<ActiveBuff> _activeBuffs = new List<ActiveBuff>();

        public Player(Vector2 startPosition)
        {
            _spawnPoint = startPosition;
            Position = startPosition;
            Inventory = new Inventory();
        }

        // Build System
        public bool IsPlacing { get; private set; }
        private string _structureToPlace;
        
        // Edit Mode
        private Structure _movingStructure;
        public bool JustPlacedStructure { get; private set; }

        public void StartPlacing(string structureType)
        {
            IsPlacing = true;
            _structureToPlace = structureType;
            _heldEntity = null;
            _prevBuildKeyPressed = true; // Prevent immediate placement from menu click
            JustPlacedStructure = false;
        }

        public void StartMoving(Structure structure)
        {
            _movingStructure = structure;
            _movingStructure.Position = new Vector2(-1000, -1000); // Hide original by moving it far away
            _prevBuildKeyPressed = true; // Prevent immediate drop from menu click
            JustPlacedStructure = false;
        }

        public void CancelBuild()
        {
            IsPlacing = false;
            _structureToPlace = null;
            _heldEntity = null;
            if (_movingStructure != null)
            {
                 // Simplest: Just drop it at current mouse pos snapped.
                 int x = ((int)_currentMouseWorldPos.X / 40) * 40;
                 int y = ((int)_currentMouseWorldPos.Y / 40) * 40;
                 _movingStructure.Position = new Vector2(x, y);
                 _movingStructure = null;
            }
        }

        public void Update(GameTime gameTime, EntityManager entityManager, Camera camera, TileMap tileMap, bool allowInput)
        {
            JustPlacedStructure = false; // Reset every frame
            var kstate = Keyboard.GetState();
            var mstate = Mouse.GetState();
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Buff Tick
            for (int i = _activeBuffs.Count - 1; i >= 0; i--)
            {
                _activeBuffs[i].TimeRemaining -= dt;
                if (_activeBuffs[i].TimeRemaining <= 0)
                {
                    _activeBuffs.RemoveAt(i);
                }
            }

            // Movement
            Vector2 movement = Vector2.Zero;
            if (kstate.IsKeyDown(Keys.W)) movement.Y -= 1;
            if (kstate.IsKeyDown(Keys.S)) movement.Y += 1;
            if (kstate.IsKeyDown(Keys.A)) movement.X -= 1;
            if (kstate.IsKeyDown(Keys.D)) movement.X += 1;

            if (movement != Vector2.Zero)
            {
                // Hunger Decay (Only when moving)
                float currentHungerDecayRate = _hungerDecayRate * (1.0f - (GetTotalHungerBuff() / 100f));
                Hunger -= currentHungerDecayRate * dt;
                if (Hunger <= 0)
                {
                    Hunger = 0;
                    LastDeathCause = DeathCause.Hunger;
                    Die();
                }

                // Oxygen Decay (Always, or maybe just when moving? Let's do always for now, but inside Update loop it's only when moving... wait. 
                // Original code had hunger decay inside "if (movement != Vector2.Zero)". 
                // Oxygen should probably decay always. Let's move it outside the movement check.
                // But for now, to minimize diff, let's keep it here or move it.
                // The prompt says "Modify Player.cs to add Oxygen property and decay".
                // I should probably move both out, but let's stick to the pattern. 
                // Actually, oxygen should decay even if standing still.
                // I will move it outside the movement check in a separate edit if needed, or just add it here for now if that's the pattern.
                // The current code structure has hunger decay inside the movement check.
                // Let's look at the file content again.
                // Lines 84: if (movement != Vector2.Zero)
                // Lines 86-92: Hunger decay.
                // I should probably move hunger decay out too, but I shouldn't change existing behavior unless asked.
                // However, oxygen definitely decays always.
                // So I will add Oxygen decay outside the movement check.
                
                movement.Normalize();
                Vector2 nextPos = Position + movement * _speed * dt;
                
                // Collision Detection
                bool collision = false;
                Rectangle playerRect = new Rectangle((int)nextPos.X, (int)nextPos.Y, 40, 40);
                // Check for crater tiles (type 2) using proper rectangle intersection
                // Player is 40x40, tiles are 48x48
                int minTileX = (int)Math.Floor(nextPos.X / TileMap.TileSize);
                int maxTileX = (int)Math.Floor((nextPos.X + 39) / TileMap.TileSize);
                int minTileY = (int)Math.Floor(nextPos.Y / TileMap.TileSize);
                int maxTileY = (int)Math.Floor((nextPos.Y + 39) / TileMap.TileSize);
                
                for (int tx = minTileX; tx <= maxTileX && !collision; tx++)
                {
                    for (int ty = minTileY; ty <= maxTileY && !collision; ty++)
                    {
                        int tileType = tileMap.GetTile(tx, ty);
                        if (tileType == 2) // Crater
                        {
                            // Check actual rectangle intersection
                            Rectangle craterRect = new Rectangle(tx * TileMap.TileSize, ty * TileMap.TileSize, TileMap.TileSize, TileMap.TileSize);
                            if (playerRect.Intersects(craterRect))
                            {
                                collision = true;
                            }
                        }
                    }
                }
                
                // Check entity collision
                if (!collision)
                {
                    foreach (var entity in entityManager.GetEntities())
                    {
                        if (entity.IsSolid)
                        {
                            Rectangle entityRect = entity.GetBounds();

                            if (playerRect.Intersects(entityRect))
                            {
                                collision = true;
                                break;
                            }
                        }
                    }
                }

                if (!collision)
                {
                    Position = nextPos;
                }
            }

            // Convert Mouse to World Coordinates
            Vector2 mousePos = camera.ScreenToWorld(new Vector2(mstate.X, mstate.Y));
            _currentMouseWorldPos = mousePos;

            if (allowInput)
            {
                if (IsPlacing)
                {
                    HandlePlacement(mstate, entityManager, mousePos);
                }
                else if (_movingStructure != null)
                {
                    HandleMoving(mstate, mousePos);
                }
                else
                {
                    // Antigravity Laser Logic (Harvesting)
                    HandleInput(mstate, entityManager, mousePos);
                }
            }
            
            _prevMouseState = mstate;

            // Oxygen Decay (Always)
            float currentOxygenDecayRate = _oxygenDecayRate * (1.0f - (GetTotalOxygenBuff() / 100f));
            Oxygen -= currentOxygenDecayRate * dt;
            if (Oxygen <= 0)
            {
                Oxygen = 0;
                LastDeathCause = DeathCause.Oxygen;
                Die();
            }
        }

        private void HandlePlacement(MouseState mstate, EntityManager entityManager, Vector2 mousePos)
        {
            if (mstate.LeftButton == ButtonState.Pressed && !_prevBuildKeyPressed) // Simple debounce check
            {
                // Place structure
                // Snap to grid (40x40)
                int x = ((int)mousePos.X / 40) * 40;
                int y = ((int)mousePos.Y / 40) * 40;
                Vector2 pos = new Vector2(x, y);

                int placeW = 80; int placeH = 80;
                if (_structureToPlace == "Workbench") { placeW = 40; placeH = 40; }
                else if (_structureToPlace == "HAB") { placeW = 80; placeH = 80; }
                else if (_structureToPlace == "Machinery") { placeW = 160; placeH = 160; }
                Rectangle buildRect = new Rectangle((int)pos.X, (int)pos.Y, placeW, placeH);

                // Check if space is clear (simple check)
                bool clear = true;
                foreach (var entity in entityManager.GetEntities())
                {
                    if (entity.GetBounds().Intersects(buildRect))
                    {
                        clear = false;
                        break;
                    }
                }

                if (clear)
                {
                    // Resource Costs
                    bool canBuild = false;
                    if (_structureToPlace == "Greenhouse")
                    {
                        // Cost: 2 Rocks, 1 Crystal
                        if (Inventory.CountItem("Rock") >= 2 && Inventory.CountItem("Crystal") >= 1)
                        {
                            Inventory.RemoveItems("Rock", 2);
                            Inventory.RemoveItems("Crystal", 1);
                            canBuild = true;
                        }
                    }
                    else if (_structureToPlace == "Workbench")
                    {
                        // Cost: 3 Rocks
                        if (Inventory.CountItem("Rock") >= 3)
                        {
                            Inventory.RemoveItems("Rock", 3);
                            canBuild = true;
                        }
                    }
                    else if (_structureToPlace == "Reactor")
                    {
                        if (Inventory.CountItem("Rock") >= 50 && Inventory.CountItem("Crystal") >= 30)
                        {
                            Inventory.RemoveItems("Rock", 50);
                            Inventory.RemoveItems("Crystal", 30);
                            canBuild = true;
                        }
                    }
                    else if (_structureToPlace == "HAB")
                    {
                         if (Inventory.CountItem("Rock") >= 60 && Inventory.CountItem("Crystal") >= 20)
                        {
                            Inventory.RemoveItems("Rock", 60);
                            Inventory.RemoveItems("Crystal", 20);
                            canBuild = true;
                        }
                    }
                    else if (_structureToPlace == "Bionic Tech")
                    {
                         if (Inventory.CountItem("Rock") >= 40 && Inventory.CountItem("Crystal") >= 50)
                        {
                            Inventory.RemoveItems("Rock", 40);
                            Inventory.RemoveItems("Crystal", 50);
                            canBuild = true;
                        }
                    }
                    else if (_structureToPlace == "Machinery")
                    {
                         if (Inventory.CountItem("Rock") >= 80 && Inventory.CountItem("Crystal") >= 40)
                        {
                            Inventory.RemoveItems("Rock", 80);
                            Inventory.RemoveItems("Crystal", 40);
                            canBuild = true;
                        }
                    }
                    else if (_structureToPlace == "Radar")
                    {
                         if (Inventory.CountItem("Rock") >= 40 && Inventory.CountItem("Crystal") >= 40)
                        {
                            Inventory.RemoveItems("Rock", 40);
                            Inventory.RemoveItems("Crystal", 40);
                            canBuild = true;
                        }
                    }
                    else if (_structureToPlace == "WormHole")
                    {
                         if (Inventory.CountItem("Rock") >= 100 && Inventory.CountItem("Crystal") >= 100)
                        {
                            Inventory.RemoveItems("Rock", 100);
                            Inventory.RemoveItems("Crystal", 100);
                            canBuild = true;
                        }
                    }

                    if (canBuild)
                    {
                        int w = 80; // Default building size (+25% from 64)
                        int h = 80;
                        if (_structureToPlace == "Workbench") { w = 40; h = 40; }
                        else if (_structureToPlace == "HAB") { w = 80; h = 80; }
                        else if (_structureToPlace == "Machinery") { w = 160; h = 160; }

                        entityManager.AddEntity(new Structure(pos, _structureToPlace, w, h));
                        IsPlacing = false; // Finish placing
                    }
                }
            }
            
            // Cancel with Right Click
            if (mstate.RightButton == ButtonState.Pressed)
            {
                IsPlacing = false;
            }
            
            _prevBuildKeyPressed = mstate.LeftButton == ButtonState.Pressed;
        }

        private void HandleMoving(MouseState mstate, Vector2 mousePos)
        {
            // Ghost follows cursor (visuals handled in Draw)
            
            // Place with Left Click (Debounced)
            if (mstate.LeftButton == ButtonState.Pressed && !_prevBuildKeyPressed)
            {
                // Snap to grid
                int x = ((int)mousePos.X / 40) * 40;
                int y = ((int)mousePos.Y / 40) * 40;
                _movingStructure.Position = new Vector2(x, y);
                
                _movingStructure = null;
                JustPlacedStructure = true;
            }
            
            _prevBuildKeyPressed = mstate.LeftButton == ButtonState.Pressed;
        }

        private void HandleInput(MouseState mstate, EntityManager entityManager, Vector2 mousePos)
        {
            // Left Click: Harvest
            if (mstate.LeftButton == ButtonState.Pressed)
            {
                Entity target = null;
                foreach (var entity in entityManager.GetEntities())
                {
                    if (entity.GetBounds().Contains(mousePos) && entity.IsHarvestable)
                    {
                        target = entity;
                        break;
                    }
                }

                if (target != null)
                {
                    if (Inventory.AddItem(target.Type))
                    {
                        entityManager.RemoveEntity(target);
                    }
                    else
                    {
                        // Inventory Full
                        OnInventoryFull?.Invoke();
                    }
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D playerTexture, Dictionary<string, Texture2D> textures)
        {
            // Draw player at 40x40 size (+25% from 32)
            spriteBatch.Draw(playerTexture, new Rectangle((int)Position.X, (int)Position.Y, 40, 40), Color.White);
            
            // Draw Build Ghost (Placing OR Moving)
            if (IsPlacing || _movingStructure != null)
            {
                int x = ((int)_currentMouseWorldPos.X / 40) * 40;
                int y = ((int)_currentMouseWorldPos.Y / 40) * 40;
                
                string structureType = IsPlacing ? _structureToPlace : _movingStructure.Type;
                
                int w = 64;
                int h = 64;
                if (structureType == "Workbench") { w = 32; h = 32; }

                Texture2D ghostTexture = playerTexture; // Fallback
                string key = structureType.ToLower();
                if (textures != null && textures.ContainsKey(key))
                {
                    ghostTexture = textures[key];
                }

                if (_movingStructure != null)
                {
                     // Draw glow outline for moving structure
                     float pulse = 1.0f + (float)Math.Sin(DateTime.Now.TimeOfDay.TotalSeconds * 6) * 0.2f;
                     Color glowColor = Color.DeepSkyBlue * pulse;

                     // Need a generic pixel texture for outline... Wait, Player doesn't have pixel texture reference natively right now.
                     // I will just use the ghostTexture itself but scaled out slightly.
                     spriteBatch.Draw(ghostTexture, new Rectangle(x - 4, y - 4, w + 8, h + 8), glowColor * 0.5f);
                }

                spriteBatch.Draw(ghostTexture, new Rectangle(x, y, w, h), Color.White * 0.5f);
            }
        }
        public void Eat(float amount)
        {
            Hunger += amount;
            if (Hunger > 100f) Hunger = 100f;
        }

        private void Die()
        {
            IsDead = true;
        }

        public void DoRespawn()
        {
            Respawn();
            IsDead = false;
        }

        public void RefillOxygen()
        {
            Oxygen = 100f;
        }

        public void TakeDamage(float amount)
        {
            Health -= amount;
            if (Health <= 0)
            {
                Health = 0;
                LastDeathCause = DeathCause.Alien;
                Die();
            }
        }

        public int GetTotalHungerBuff()
        {
            int total = 0;
            foreach (var buff in _activeBuffs) total += buff.HungerPercentage;
            return total;
        }

        public int GetTotalOxygenBuff()
        {
            int total = 0;
            foreach (var buff in _activeBuffs) total += buff.OxygenPercentage;
            return total;
        }

        public void AddBuff(int hungerReward, int oxygenReward, float duration)
        {
            if (hungerReward == 0 && oxygenReward == 0) return;

            // If at max capacity based on SuitLevel, remove the buff with shortest remaining time
            if (_activeBuffs.Count >= SuitLevel)
            {
                ActiveBuff shortest = _activeBuffs[0];
                foreach (var buff in _activeBuffs)
                {
                    if (buff.TimeRemaining < shortest.TimeRemaining)
                        shortest = buff;
                }
                _activeBuffs.Remove(shortest);
            }

            _activeBuffs.Add(new ActiveBuff
            {
                HungerPercentage = hungerReward,
                OxygenPercentage = oxygenReward,
                TimeRemaining = duration
            });
        }

        private void Respawn()
        {
            Position = _spawnPoint;
            Inventory.Clear();
            Hunger = 100f;
            Health = 100f;
            Oxygen = 100f;
            LastDeathCause = DeathCause.None;
        }
    }
}
