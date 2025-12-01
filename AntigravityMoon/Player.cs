using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace AntigravityMoon
{
    public class Player
    {
        public Vector2 Position { get; private set; }
        public Inventory Inventory { get; private set; }
        private float _speed = 200f;
        private Entity _heldEntity; // For antigravity tool
        
        // Build Mode
        public bool IsBuildMode { get; private set; }
        private string _selectedStructure = "Greenhouse"; // Default

        private bool _prevBuildKeyPressed = false;
        private Vector2 _currentMouseWorldPos;
        private MouseState _prevMouseState;

        public float Hunger { get; private set; } = 100f;
        private float _hungerDecayRate = 1.0f; // Per second (approx 100s to starve)
        private Vector2 _spawnPoint;

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

        public void StartPlacing(string structureType)
        {
            IsPlacing = true;
            _structureToPlace = structureType;
            _heldEntity = null;
            _prevBuildKeyPressed = true; // Prevent immediate placement from menu click
        }

        public void StartMoving(Structure structure)
        {
            _movingStructure = structure;
            _movingStructure.Position = new Vector2(-1000, -1000); // Hide original by moving it far away
        }

        public void CancelBuild()
        {
            IsPlacing = false;
            _structureToPlace = null;
            _heldEntity = null;
            if (_movingStructure != null)
            {
                 // Simplest: Just drop it at current mouse pos snapped.
                 int x = ((int)_currentMouseWorldPos.X / 32) * 32;
                 int y = ((int)_currentMouseWorldPos.Y / 32) * 32;
                 _movingStructure.Position = new Vector2(x, y);
                 _movingStructure = null;
            }
        }

        public void Update(GameTime gameTime, EntityManager entityManager, Camera camera)
        {
            var kstate = Keyboard.GetState();
            var mstate = Mouse.GetState();
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Movement
            Vector2 movement = Vector2.Zero;
            if (kstate.IsKeyDown(Keys.W)) movement.Y -= 1;
            if (kstate.IsKeyDown(Keys.S)) movement.Y += 1;
            if (kstate.IsKeyDown(Keys.A)) movement.X -= 1;
            if (kstate.IsKeyDown(Keys.D)) movement.X += 1;

            if (movement != Vector2.Zero)
            {
                // Hunger Decay (Only when moving)
                Hunger -= _hungerDecayRate * dt;
                if (Hunger <= 0)
                {
                    Hunger = 0;
                    Die();
                }

                movement.Normalize();
                Vector2 nextPos = Position + movement * _speed * dt;
                
                // Collision Detection
                bool collision = false;
                Rectangle playerRect = new Rectangle((int)nextPos.X, (int)nextPos.Y, 32, 32);
                foreach (var entity in entityManager.GetEntities())
                {
                    if (entity.IsSolid)
                    {
                        Rectangle entityRect = new Rectangle((int)entity.Position.X, (int)entity.Position.Y, 32, 32);
                        // Structure might be bigger
                        if (entity is Structure s)
                        {
                            entityRect.Width = s.Width;
                            entityRect.Height = s.Height;
                        }

                        if (playerRect.Intersects(entityRect))
                        {
                            collision = true;
                            break;
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
            
            _prevMouseState = mstate;
        }

        private void HandlePlacement(MouseState mstate, EntityManager entityManager, Vector2 mousePos)
        {
            if (mstate.LeftButton == ButtonState.Pressed && !_prevBuildKeyPressed) // Simple debounce check
            {
                // Place structure
                // Snap to grid (32x32)
                int x = ((int)mousePos.X / 32) * 32;
                int y = ((int)mousePos.Y / 32) * 32;
                Vector2 pos = new Vector2(x, y);

                // Check if space is clear (simple check)
                bool clear = true;
                foreach (var entity in entityManager.GetEntities())
                {
                    if (Vector2.Distance(entity.Position, pos) < 32)
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

                    if (canBuild)
                    {
                        int w = 64; // 2x2 tiles
                        int h = 64;
                        if (_structureToPlace == "Workbench") { w = 32; h = 32; }

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
            if (mstate.LeftButton == ButtonState.Pressed && _prevMouseState.LeftButton == ButtonState.Released)
            {
                // Snap to grid
                int x = ((int)mousePos.X / 32) * 32;
                int y = ((int)mousePos.Y / 32) * 32;
                _movingStructure.Position = new Vector2(x, y);
                
                _movingStructure = null;
            }
        }

        private void HandleInput(MouseState mstate, EntityManager entityManager, Vector2 mousePos)
        {
            // Left Click: Harvest
            if (mstate.LeftButton == ButtonState.Pressed)
            {
                Entity target = null;
                foreach (var entity in entityManager.GetEntities())
                {
                    if (Vector2.Distance(entity.Position, mousePos) < 32 && entity.IsHarvestable)
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
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D playerTexture, Dictionary<string, Texture2D> textures)
        {
            // Draw player at 32x32 size
            spriteBatch.Draw(playerTexture, new Rectangle((int)Position.X, (int)Position.Y, 32, 32), Color.White);
            
            // Draw Build Ghost (Placing OR Moving)
            if (IsPlacing || _movingStructure != null)
            {
                int x = ((int)_currentMouseWorldPos.X / 32) * 32;
                int y = ((int)_currentMouseWorldPos.Y / 32) * 32;
                
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
            Respawn();
        }

        private void Respawn()
        {
            Position = _spawnPoint;
            Inventory.Clear();
            Hunger = 100f;
        }
    }
}
