using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace AntigravityMoon
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Texture2D _pixelTexture;
        private Dictionary<string, Texture2D> _textures;

        private TileMap _tileMap;
        private EntityManager _entityManager;
        private Player _player;
        private Camera _camera;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 1920;
            _graphics.PreferredBackBufferHeight = 1080;
            _graphics.ApplyChanges();
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _camera = new Camera(GraphicsDevice.Viewport);
            _tileMap = new TileMap(); // Infinite map
            _entityManager = new EntityManager();
            _player = new Player(Vector2.Zero); // Start at world origin

            // Add some test entities
            _entityManager.AddEntity(new Entity(new Vector2(300, 300), "Rock", true, true, true)); // Rock is harvestable and solid
            _entityManager.AddEntity(new Entity(new Vector2(400, 200), "Crystal", true, true, true)); // Crystal is harvestable and solid

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Create a 1x1 white texture for drawing primitives
            _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });

            // Load Textures
            _textures = new Dictionary<string, Texture2D>();
            _textures["Pixel"] = _pixelTexture; // Add Pixel texture for fallback
            LoadTexture("astronaut");
            LoadTexture("moon_ground");
            LoadTexture("rock");
            LoadTexture("crystal");
            LoadTexture("greenhouse");
            LoadTexture("workbench");
            LoadTexture("corn");
            LoadTexture("spaceship");
            LoadTexture("metro_alien");
            _spaceshipTexture = _textures["spaceship"];
            _spaceshipPosition = new Vector2(400, -100); // Start off screen
        }

        private void LoadTexture(string name)
        {
            string path = Path.Combine("Content", name + ".png");
            if (File.Exists(path))
            {
                using (var stream = File.OpenRead(path))
                {
                    _textures[name] = Texture2D.FromStream(GraphicsDevice, stream);
                }
            }
            else
            {
                // Fallback to pixel texture if file missing
                _textures[name] = _pixelTexture; 
            }
        }

        private KeyboardState _prevKeyboardState;
        private MouseState _prevMouseState;
        private bool _showInventory = false;
        private float _respawnTimer = 0f;
        private System.Random _random = new System.Random();

        // UI State
        private bool _showWorkbenchMenu = false;
        private bool _showGreenhouseMenu = false;
        private bool _showSpaceshipMenu = false;
        private Structure _interactedStructure;
        
        // Context Menu
        private bool _showInventoryContextMenu = false;
        private Vector2 _contextMenuPos;
        private string _contextMenuItem;
        private Point _contextMenuItemGridPos;

        // Build System
        public enum BuildModeState { None, Menu, Placing, Editing }
        private BuildModeState _buildModeState = BuildModeState.None;
        private string _selectedBuildStructure;
        
        // Edit Mode Context Menu
        private bool _showEditContextMenu = false;
        private Vector2 _editContextMenuPos;
        private Structure _editTargetStructure;

        // Game State
        public enum GameState { StartMenu, Intro, Playing }
        private GameState _currentGameState = GameState.StartMenu;
        private float _introTimer = 0f;
        private Vector2 _spaceshipPosition;
        private Texture2D _spaceshipTexture;
        
        // Alien Logic
        private List<Alien> _aliens = new List<Alien>();
        private float _alienSpawnTimer = 0f;
        private bool _alienSpawned = false;
        
        // Death Screen
        private bool _showDeathScreen = false;

        // Laser System
        private struct Laser
        {
            public Vector2 Start;
            public Vector2 End;
            public float Duration;
            public float MaxDuration;
        }
        private List<Laser> _lasers = new List<Laser>();

        // Minimap
        private bool _showMinimap = true;
        private int _minimapSize = 1; // 1 = Small, 2 = Large
        private const int MinimapScaleSmall = 4;
        private const int MinimapScaleLarge = 8;

        private Vector2 GetInventoryPosition()
        {
            int cellSize = 40;
            int padding = 5;
            int width = _player.Inventory.Cols * (cellSize + padding) + padding;
            int height = _player.Inventory.Rows * (cellSize + padding) + padding;
            
            int x = (1920 - width) / 2; // Centered
            int y = 1080 - height - 20; // 20px from bottom
            
            return new Vector2(x, y);
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState currentKeyboardState = Keyboard.GetState();
            MouseState currentMouseState = Mouse.GetState();
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || currentKeyboardState.IsKeyDown(Keys.Escape))
                Exit();

            if (_currentGameState == GameState.StartMenu)
            {
                if (currentKeyboardState.IsKeyDown(Keys.Enter))
                {
                    _currentGameState = GameState.Intro;
                    _spaceshipPosition = new Vector2(400 - 32, -100); // Center horizontally (assuming 64 width)
                }
            }
            else if (_currentGameState == GameState.Intro)
            {
                _spaceshipPosition.Y += 200f * dt; // Speed
                _camera.Position = _spaceshipPosition + new Vector2(32, 32); // Follow center

                if (_spaceshipPosition.Y >= 200) // Land higher (Y=200)
                {
                    _spaceshipPosition.Y = 200;
                    
                    // Add a small delay/pause before switching to playing
                    _introTimer += dt;
                    if (_introTimer > 1.0f)
                    {
                         _currentGameState = GameState.Playing;
                         _introTimer = 0f;

                        // Create persistent spaceship structure
                        // Position, Type, Width, Height
                        _entityManager.AddEntity(new Structure(_spaceshipPosition, "Spaceship", 64, 64));
                        
                        // Ensure player is at spawn point (304) which is below the ship (200+64=264)
                        _player.Position = new Vector2(400, 304);
                    }
                }
            }
            else if (_currentGameState == GameState.Playing)
            {
            // Check for player death
            if (_player.IsDead && !_showDeathScreen)
            {
                _showDeathScreen = true;
            }

            // Handle Death Screen Input
            if (_showDeathScreen)
            {
                if (currentMouseState.LeftButton == ButtonState.Pressed && _prevMouseState.LeftButton == ButtonState.Released)
                {
                    // Respawn button: 960-150, 540+50, 300, 60
                    Rectangle respawnBtn = new Rectangle(960 - 150, 540 + 50, 300, 60);
                    if (respawnBtn.Contains(currentMouseState.Position))
                    {
                        _showDeathScreen = false;
                        _player.DoRespawn();
                        // Reset game state for respawn
                        _alienSpawned = false;
                        _alienSpawnTimer = 0f;
                        _aliens.Clear();
                  }
                }
                // Skip normal game logic while death screen is showing
                _prevKeyboardState = currentKeyboardState;
                _prevMouseState = currentMouseState;
                base.Update(gameTime);
                return;
            }

            if (currentKeyboardState.IsKeyDown(Keys.I) && !_prevKeyboardState.IsKeyDown(Keys.I))
            {
                _showInventory = !_showInventory;
                _showWorkbenchMenu = false;
                _showGreenhouseMenu = false;
                _showSpaceshipMenu = false;
                _showInventoryContextMenu = false;
            }

            // Minimap Toggle
            if (currentKeyboardState.IsKeyDown(Keys.M) && !_prevKeyboardState.IsKeyDown(Keys.M))
            {
                _minimapSize++;
                if (_minimapSize > 2) _minimapSize = 1;
            }

            // Update Exploration
            _tileMap.Explore(_player.Position, 300f); // 300px radius

            // Inventory Context Menu Logic
            if (_showInventory && _showInventoryContextMenu)
            {
                if (currentMouseState.LeftButton == ButtonState.Pressed && _prevMouseState.LeftButton == ButtonState.Released)
                {
                    bool isFood = _contextMenuItem == "Corn";
                    int menuHeight = isFood ? 50 : 25;
                    
                    Rectangle eatRect = isFood ? new Rectangle((int)_contextMenuPos.X, (int)_contextMenuPos.Y, 60, 25) : Rectangle.Empty;
                    Rectangle dropRect = isFood ? 
                        new Rectangle((int)_contextMenuPos.X, (int)_contextMenuPos.Y + 25, 60, 25) : 
                        new Rectangle((int)_contextMenuPos.X, (int)_contextMenuPos.Y, 60, 25);
                    
                    if (isFood && eatRect.Contains(currentMouseState.Position))
                    {
                        // Eat
                        if (_contextMenuItem != null)
                        {
                            _player.Inventory.RemoveItem((int)_contextMenuItemGridPos.X, (int)_contextMenuItemGridPos.Y);
                            _player.Eat(100f);
                        }
                        _showInventoryContextMenu = false;
                    }
                    else if (dropRect.Contains(currentMouseState.Position))
                    {
                        // Drop
                        if (_contextMenuItem != null)
                        {
                            _player.Inventory.RemoveItem((int)_contextMenuItemGridPos.X, (int)_contextMenuItemGridPos.Y);
                            // Spawn item entity at player position
                            // Offset slightly so it's not directly under player
                            Vector2 dropPos = _player.Position + new Vector2(0, 32);
                            _entityManager.AddEntity(new Entity(dropPos, _contextMenuItem, true, true));
                        }
                        _showInventoryContextMenu = false;
                    }
                    else
                    {
                        _showInventoryContextMenu = false;
                    }
                }
            }
            else if (_showInventory)
            {
                // Right Click on Item to open Context Menu
                if (currentMouseState.RightButton == ButtonState.Pressed && _prevMouseState.RightButton == ButtonState.Released)
                {
                    // Calculate grid pos
                    Vector2 invPos = GetInventoryPosition();
                    int startX = (int)invPos.X;
                    int startY = (int)invPos.Y;
                    int cellSize = 40;
                    int padding = 5;
                    
                    int mx = currentMouseState.X;
                    int my = currentMouseState.Y;
                    
                    if (mx >= startX && my >= startY)
                    {
                        int col = (mx - startX) / (cellSize + padding);
                        int row = (my - startY) / (cellSize + padding);
                        
                        if (col >= 0 && col < _player.Inventory.Cols && row >= 0 && row < _player.Inventory.Rows)
                        {
                            string item = _player.Inventory.GetItem(col, row);
                            if (item != null)
                            {
                                _showInventoryContextMenu = true;
                                _contextMenuPos = new Vector2(mx, my);
                                _contextMenuItem = item;
                                _contextMenuItemGridPos = new Point(col, row);
                            }
                        }
                    }
                }
            }

            // Close Menus with Escape or moving away
            if (_showWorkbenchMenu || _showGreenhouseMenu)
            {
                if (currentKeyboardState.IsKeyDown(Keys.Escape))
                {
                    _showWorkbenchMenu = false;
                    _showGreenhouseMenu = false;
                    _interactedStructure = null;
                }
                else if (_interactedStructure != null && Vector2.Distance(_player.Position, _interactedStructure.Position) > 100)
                {
                    _showWorkbenchMenu = false;
                    _showGreenhouseMenu = false;
                    _interactedStructure = null;
                }
                
                // Handle Menu Clicks (Simple UI logic)
                if (currentMouseState.LeftButton == ButtonState.Pressed && _prevMouseState.LeftButton == ButtonState.Released)
                {
                    // Check UI bounds (Hardcoded for now)
                    Vector2 mousePos = new Vector2(currentMouseState.X, currentMouseState.Y);
                    
                    // Menu Rect: 960-300=660, 540-200=340. Size 600x400.
                    Rectangle menuRect = new Rectangle(660, 340, 600, 400);

                    // Close Button (Right - 40, Top + 10) -> (660+600-40, 340+10) = (1220, 350, 30, 30)
                    if (new Rectangle(menuRect.Right - 40, menuRect.Top + 10, 30, 30).Contains(mousePos))
                    {
                        _showWorkbenchMenu = false;
                        _showGreenhouseMenu = false;
                        _interactedStructure = null;
                        return;
                    }

                    if (_showWorkbenchMenu)
                    {
                        // Upgrade Backpack Button (Rect: X+50, Y+100, 300, 60) -> (710, 440, 300, 60)
                        if (new Rectangle(menuRect.X + 50, menuRect.Top + 100, 300, 60).Contains(mousePos))
                        {
                            int cost = 0;
                            if (_player.Inventory.UpgradeLevel == 0) cost = 10;
                            else if (_player.Inventory.UpgradeLevel == 1) cost = 20;

                            if (cost > 0 && _player.Inventory.CountItem("Crystal") >= cost)
                            {
                                if (_player.Inventory.RemoveItems("Crystal", cost))
                                {
                                    _player.Inventory.Upgrade();
                                }
                            }
                        }
                    }
                    else if (_showGreenhouseMenu)
                    {
                        // Plant Corn Button (Rect: X+50, Y+100, 300, 60) -> (710, 440, 300, 60)
                        if (new Rectangle(menuRect.X + 50, menuRect.Top + 100, 300, 60).Contains(mousePos))
                        {
                            if (_interactedStructure != null && !_interactedStructure.IsGrowing && !_interactedStructure.IsReadyToHarvest)
                            {
                                if (_player.Inventory.RemoveItems("Crystal", 1))
                                {
                                    _interactedStructure.StartGrowing("Corn");
                                }
                            }
                        }
                        // Harvest Button (Rect: X+50, Y+200, 300, 60) -> (710, 540, 300, 60)
                        if (new Rectangle(menuRect.X + 50, menuRect.Top + 200, 300, 60).Contains(mousePos))
                        {
                             if (_interactedStructure != null && _interactedStructure.IsReadyToHarvest)
                             {
                                 string crop = _interactedStructure.Harvest();
                                 if (crop != null)
                                 {
                                     _player.Inventory.AddItem(crop);
                                 }
                             }
                        }
                    }
                }
            }
            else if (!_showInventory)
            {
                _player.Update(gameTime, _entityManager, _camera, _tileMap);
                _camera.Position = _player.Position; // Follow player

                // Check for interactions
                if (currentMouseState.LeftButton == ButtonState.Pressed && _prevMouseState.LeftButton == ButtonState.Released)
                {
                    Vector2 mouseWorldPos = _camera.ScreenToWorld(new Vector2(currentMouseState.X, currentMouseState.Y));
                    
                    // Check for alien clicks first
                    bool hitAlien = false;
                    foreach (var alien in _aliens)
                    {
                        Rectangle alienBounds = new Rectangle((int)alien.Position.X, (int)alien.Position.Y, 128, 64);
                        if (alienBounds.Contains(mouseWorldPos))
                        {
                            alien.TakeDamage(10f); // 10% damage per click
                            // Add Laser
                            _lasers.Add(new Laser { Start = _player.Position + new Vector2(16, 16), End = mouseWorldPos, Duration = 0.2f, MaxDuration = 0.2f });
                            hitAlien = true;
                            break;
                        }
                    }
                    
                    // Only check structure interactions if we didn't hit an alien
                    if (!hitAlien)
                    {
                        foreach (var entity in _entityManager.GetEntities())
                    {
                        if (entity is Structure s && Vector2.Distance(s.Position, mouseWorldPos) < 40)
                        {
                            // Add Laser for object interaction too
                            _lasers.Add(new Laser { Start = _player.Position + new Vector2(16, 16), End = mouseWorldPos, Duration = 0.2f, MaxDuration = 0.2f });

                            if (Vector2.Distance(_player.Position, s.Position) < 100) // Must be close
                            {
                                if (s.Type == "Workbench")
                                {
                                    _showWorkbenchMenu = true;
                                    _interactedStructure = s;
                                }
                                else if (s.Type == "Greenhouse")
                                {
                                    _showGreenhouseMenu = true;
                                    _interactedStructure = s;
                                }
                                else if (s.Type == "Spaceship")
                                {
                                    // Open Spaceship Menu
                                    _showSpaceshipMenu = true;
                                    _interactedStructure = s;
                                }
                            }
                        }
                    }
                    }
                }
                
                // Eat Corn Logic (Press E to eat if have Corn)
                if (currentKeyboardState.IsKeyDown(Keys.E) && !_prevKeyboardState.IsKeyDown(Keys.E))
                {
                    if (_player.Inventory.RemoveItems("Corn", 1))
                    {
                        _player.Eat(100f);
                    }
                }

                // Update Structures
                foreach (var entity in _entityManager.GetEntities())
                {
                    if (entity is Structure s)
                    {
                        s.Update(dt);
                    }
                }

                // Auto-Respawn Logic
                _respawnTimer += dt;
                if (_respawnTimer >= 10f)
                {
                    _respawnTimer = 0f;
                    SpawnRandomEntity();
                }

                // Alien Spawning Logic (Spawn after 30s)
                if (!_alienSpawned)
                {
                    _alienSpawnTimer += dt;
                    if (_alienSpawnTimer >= 30f)
                    {
                        _alienSpawned = true;
                        // Spawn Alien at random position away from player
                        Vector2 spawnPos = _player.Position + new Vector2(400, 0); // 400px to the right
                        _aliens.Add(new Alien(spawnPos));
                    }
                }

                if (_showSpaceshipMenu)
            {
                if (currentMouseState.LeftButton == ButtonState.Pressed && _prevMouseState.LeftButton == ButtonState.Released)
                {
                    // Menu Position (Centered)
                    int menuWidth = 300;
                    int menuHeight = 150;
                    int menuX = (1920 - menuWidth) / 2;
                    int menuY = (1080 - menuHeight) / 2;
                    
                    Rectangle menuRect = new Rectangle(menuX, menuY, menuWidth, menuHeight);
                    
                    // Refill Button
                    Rectangle refillBtn = new Rectangle(menuX + 50, menuY + 60, 200, 40);
                    // Exit Button
                    Rectangle exitBtn = new Rectangle(menuX + 250, menuY + 10, 40, 40);
                    
                    if (refillBtn.Contains(currentMouseState.Position))
                    {
                        // Cost: 2 Rocks
                        if (_player.Inventory.RemoveItems("Rock", 2))
                        {
                            _player.RefillOxygen();
                            _showSpaceshipMenu = false;
                        }
                    }
                    else if (exitBtn.Contains(currentMouseState.Position))
                    {
                        _showSpaceshipMenu = false;
                    }
                    else if (!menuRect.Contains(currentMouseState.Position))
                    {
                        _showSpaceshipMenu = false;
                    }
                }
                return; // Block other input
            }

            // Update Aliens
                for (int i = _aliens.Count - 1; i >= 0; i--)
                {
                    _aliens[i].Update(dt, _player);
                    if (_aliens[i].IsDead)
                    {
                        _aliens.RemoveAt(i);
                    }
                }

            // Update Lasers
            for (int i = _lasers.Count - 1; i >= 0; i--)
            {
                Laser l = _lasers[i];
                l.Duration -= dt;
                _lasers[i] = l; // Update struct in list
                if (l.Duration <= 0)
                {
                    _lasers.RemoveAt(i);
                }
            }          }
            }



            // Toggle Build Menu
            if (currentKeyboardState.IsKeyDown(Keys.B) && !_prevKeyboardState.IsKeyDown(Keys.B))
            {
                if (_buildModeState == BuildModeState.None) _buildModeState = BuildModeState.Menu;
                else _buildModeState = BuildModeState.None;
                
                _showEditContextMenu = false;
                _player.CancelBuild(); // Reset player build state
            }

            if (_buildModeState != BuildModeState.None)
            {
                // Handle Menu Clicks
                if (_buildModeState == BuildModeState.Menu || _buildModeState == BuildModeState.Editing)
                {
                    if (currentMouseState.LeftButton == ButtonState.Pressed && _prevMouseState.LeftButton == ButtonState.Released)
                    {
                        int menuWidth = 60;
                        int menuHeight = 6 * 50 + 7 * 5;
                        int menuX = 10;
                        int menuY = (1080 - menuHeight) / 2;
                        
                        // Check Slots
                        for (int i = 0; i < 6; i++)
                        {
                            Rectangle slotRect = new Rectangle(menuX + 5, menuY + 5 + i * 55, 50, 50);
                            if (slotRect.Contains(currentMouseState.Position))
                            {
                                if (i == 0) // Greenhouse
                                {
                                    if (_player.Inventory.CountItem("Rock") >= 2 && _player.Inventory.CountItem("Crystal") >= 1)
                                    {
                                        _selectedBuildStructure = "Greenhouse";
                                        _buildModeState = BuildModeState.Placing;
                                        _player.StartPlacing("Greenhouse");
                                    }
                                }
                                else if (i == 1) // Workbench
                                {
                                    if (_player.Inventory.CountItem("Rock") >= 3)
                                    {
                                        _selectedBuildStructure = "Workbench";
                                        _buildModeState = BuildModeState.Placing;
                                        _player.StartPlacing("Workbench");
                                    }
                                }
                                else if (i == 5) // Edit Button
                                {
                                    if (_buildModeState == BuildModeState.Menu) _buildModeState = BuildModeState.Editing;
                                    else _buildModeState = BuildModeState.Menu;
                                    _showEditContextMenu = false;
                                }
                            }
                        }
                    }
                }

                // Handle Edit Mode Interactions
                if (_buildModeState == BuildModeState.Editing)
                {
                    if (_showEditContextMenu)
                    {
                        // Handle Context Menu Clicks
                        if (currentMouseState.LeftButton == ButtonState.Pressed && _prevMouseState.LeftButton == ButtonState.Released)
                        {
                            Rectangle moveRect = new Rectangle((int)_editContextMenuPos.X, (int)_editContextMenuPos.Y, 80, 25);
                            Rectangle deleteRect = new Rectangle((int)_editContextMenuPos.X, (int)_editContextMenuPos.Y + 25, 80, 25);
                            
                            if (moveRect.Contains(currentMouseState.Position))
                            {
                                // Move
                                _player.StartMoving(_editTargetStructure);
                                _showEditContextMenu = false;
                            }
                            else if (deleteRect.Contains(currentMouseState.Position))
                            {
                                // Delete
                                _entityManager.RemoveEntity(_editTargetStructure);
                                _showEditContextMenu = false;
                            }
                            else
                            {
                                _showEditContextMenu = false;
                            }
                        }
                    }
                    else
                    {
                        // Right Click on Structure to Open Menu
                        if (currentMouseState.RightButton == ButtonState.Pressed && _prevMouseState.RightButton == ButtonState.Released)
                        {
                            Vector2 mouseWorldPos = _camera.ScreenToWorld(new Vector2(currentMouseState.X, currentMouseState.Y));
                            foreach (var entity in _entityManager.GetEntities())
                            {
                                if (entity is Structure s && Vector2.Distance(s.Position, mouseWorldPos) < 32)
                                {
                                    _editTargetStructure = s;
                                    _editContextMenuPos = new Vector2(currentMouseState.X, currentMouseState.Y);
                                    _showEditContextMenu = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                
                // Handle Placement Completion
                if (_buildModeState == BuildModeState.Placing)
                {
                    if (!_player.IsPlacing) // Player finished placing
                    {
                        _buildModeState = BuildModeState.Menu; // Return to menu
                    }
                }
            }



            _prevKeyboardState = currentKeyboardState;
            _prevMouseState = currentMouseState;

            base.Update(gameTime);
        }

        private void SpawnRandomEntity()
        {
            // Try to find a valid spot
            for (int i = 0; i < 10; i++) // 10 attempts
            {
                int x = _random.Next(0, 25) * 32; // Grid aligned
                int y = _random.Next(0, 19) * 32;
                Vector2 pos = new Vector2(x, y);

                bool clear = true;
                foreach (var entity in _entityManager.GetEntities())
                {
                    if (Vector2.Distance(entity.Position, pos) < 32)
                    {
                        clear = false;
                        break;
                    }
                }

                if (clear)
                {
                    string type = _random.NextDouble() < 0.7 ? "Rock" : "Crystal";
                    bool isHarvestable = type == "Crystal"; // Rocks harvestable? Original code said Rock is movable, Crystal harvestable. 
                    // Let's make both harvestable for resources? 
                    // Original Entity setup: Rock (Movable=true, Harvestable=false), Crystal (Movable=true, Harvestable=true)
                    // But we need Rocks for building. So Rocks must be harvestable now.
                    
                    // Update: Rocks should be harvestable to get "Rock" item.
                    // Let's assume Rocks are now harvestable.
                    
                    _entityManager.AddEntity(new Entity(pos, type, true, true, true));
                    break;
                }
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            if (_currentGameState == GameState.StartMenu)
            {
                _spriteBatch.Begin();
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "ANTIGRAVITY MOON", new Vector2(400, 400), Color.White, 8);
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "PRESS ENTER TO START", new Vector2(550, 600), Color.White, 4);
                _spriteBatch.End();
                return;
            }

            // Apply Camera Transform
            _spriteBatch.Begin(transformMatrix: _camera.GetViewMatrix(), samplerState: SamplerState.PointClamp);

    // Calculate camera view rectangle for culling
    Rectangle cameraRect = new Rectangle(
        (int)(_camera.Position.X - 960),
        (int)(_camera.Position.Y - 540),
        1920,
        1080
    );
    _tileMap.Draw(_spriteBatch, _textures.ContainsKey("moon_ground") ? _textures["moon_ground"] : _pixelTexture, cameraRect);
            
            if (_currentGameState == GameState.Intro)
            {
                 // Draw Spaceship
                 _spriteBatch.Draw(_spaceshipTexture, new Rectangle((int)_spaceshipPosition.X, (int)_spaceshipPosition.Y, 64, 64), Color.White);
            }
            else if (_currentGameState == GameState.Playing)
            {
                // Calculate Mouse World Pos for Hover Logic
                Vector2 mouseWorldPos = _camera.ScreenToWorld(new Vector2(Mouse.GetState().X, Mouse.GetState().Y));
                
                _entityManager.Draw(_spriteBatch, _textures, mouseWorldPos);
                
                // Draw Aliens
                foreach (var alien in _aliens)
                {
                    alien.Draw(_spriteBatch, _textures.ContainsKey("metro_alien") ? _textures["metro_alien"] : _pixelTexture, mouseWorldPos);
                }

                // Draw Lasers
                foreach (var laser in _lasers)
                {
                    float alpha = laser.Duration / laser.MaxDuration;
                    DrawLine(_spriteBatch, _pixelTexture, laser.Start, laser.End, Color.Cyan * alpha, 2);
                }

                _player.Draw(_spriteBatch, _textures.ContainsKey("astronaut") ? _textures["astronaut"] : _pixelTexture, _textures);
            }

            _spriteBatch.End();

            if (_currentGameState == GameState.Playing)
            {
                // Draw UI (Inventory) separately without camera transform (Screen Space)
                _spriteBatch.Begin();
            
            if (_showInventory)
            {
                _player.Inventory.Draw(_spriteBatch, _pixelTexture, _textures, GetInventoryPosition());
            }

            // Draw Health Bar
            PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "HEALTH", new Vector2(20, 20), Color.White, 2);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(20, 50, 400, 40), Color.Gray);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(20, 50, (int)(_player.Health * 4), 40), Color.Green);

            // Draw Hunger Bar
            PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "HUNGER", new Vector2(20, 100), Color.White, 2);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(20, 130, 400, 40), Color.Gray);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(20, 130, (int)(_player.Hunger * 4), 40), Color.Orange);

            // Draw Oxygen Bar
            PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "OXYJIN", new Vector2(20, 180), Color.White, 2);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(20, 210, 400, 40), Color.Gray);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(20, 210, (int)(_player.Oxygen * 4), 40), Color.CornflowerBlue);

            // Draw Control Labels
            PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "PRESS B TO BUILD", new Vector2(1920 - 600, 20), Color.White, 2);
            PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "PRESS I FOR INVENTORY", new Vector2(1920 - 600, 60), Color.White, 2);

            // Draw Menus
            if (_showWorkbenchMenu)
            {
                // Background (Centered on 1920x1080)
                // 1920/2 = 960, 1080/2 = 540. Rect 400x300?
                // Let's make it bigger: 600x400
                Rectangle menuRect = new Rectangle(960 - 300, 540 - 200, 600, 400);
                _spriteBatch.Draw(_pixelTexture, menuRect, Color.DarkGray);
                
                // Close Button
                _spriteBatch.Draw(_pixelTexture, new Rectangle(menuRect.Right - 40, menuRect.Top + 10, 30, 30), Color.Red);
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "X", new Vector2(menuRect.Right - 32, menuRect.Top + 18), Color.White, 2);
                
                // Title
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "WORKBENCH", new Vector2(menuRect.X + 20, menuRect.Top + 20), Color.White, 4);

                // Upgrade Backpack Button
                int cost = 0;
                string costText = "";
                bool canUpgrade = false;

                if (_player.Inventory.UpgradeLevel == 0)
                {
                    cost = 10;
                    costText = "10 CRYSTAL";
                    canUpgrade = _player.Inventory.CountItem("Crystal") >= cost;
                }
                else if (_player.Inventory.UpgradeLevel == 1)
                {
                    cost = 20;
                    costText = "20 CRYSTAL";
                    canUpgrade = _player.Inventory.CountItem("Crystal") >= cost;
                }

                if (_player.Inventory.UpgradeLevel < 2)
                {
                    Rectangle btnRect = new Rectangle(menuRect.X + 50, menuRect.Top + 100, 300, 60);
                    _spriteBatch.Draw(_pixelTexture, btnRect, canUpgrade ? Color.Blue : Color.Gray);
                    PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "UPGRADE BAG", new Vector2(btnRect.X + 10, btnRect.Y + 20), Color.White, 2);
                    PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, costText, new Vector2(btnRect.X + 10, btnRect.Y + 70), canUpgrade ? Color.White : Color.Red, 2);
                }
                else
                {
                    PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "MAX LEVEL", new Vector2(menuRect.X + 50, menuRect.Top + 100), Color.Gold, 4);
                }
            }
            else if (_showGreenhouseMenu)
            {
                // Background
                Rectangle menuRect = new Rectangle(960 - 300, 540 - 200, 600, 400);
                _spriteBatch.Draw(_pixelTexture, menuRect, Color.DarkGreen);
                
                // Close Button
                _spriteBatch.Draw(_pixelTexture, new Rectangle(menuRect.Right - 40, menuRect.Top + 10, 30, 30), Color.Red);
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "X", new Vector2(menuRect.Right - 32, menuRect.Top + 18), Color.White, 2);

                // Title
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "GREENHOUSE", new Vector2(menuRect.X + 20, menuRect.Top + 20), Color.White, 4);

                // Plant Button
                Color plantColor = (_interactedStructure != null && !_interactedStructure.IsGrowing && !_interactedStructure.IsReadyToHarvest) ? Color.Yellow : Color.Gray;
                Rectangle plantBtnRect = new Rectangle(menuRect.X + 50, menuRect.Top + 100, 300, 60);
                _spriteBatch.Draw(_pixelTexture, plantBtnRect, plantColor);
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "PLANT CORN", new Vector2(plantBtnRect.X + 10, plantBtnRect.Y + 20), Color.Black, 2);
                
                // Draw Cost (1 Crystal)
                if (_textures.ContainsKey("crystal"))
                {
                    _spriteBatch.Draw(_textures["crystal"], new Rectangle(plantBtnRect.Right + 10, plantBtnRect.Y, 40, 40), Color.White);
                    PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "x1", new Vector2(plantBtnRect.Right + 60, plantBtnRect.Y + 10), Color.White, 2);
                }
                
                // Harvest Button
                Color harvestColor = (_interactedStructure != null && _interactedStructure.IsReadyToHarvest) ? Color.Green : Color.Gray;
                Rectangle harvestBtnRect = new Rectangle(menuRect.X + 50, menuRect.Top + 200, 300, 60);
                _spriteBatch.Draw(_pixelTexture, harvestBtnRect, harvestColor);
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "HARVEST", new Vector2(harvestBtnRect.X + 10, harvestBtnRect.Y + 20), Color.Black, 2);

                // Growth Timer
                if (_interactedStructure != null && _interactedStructure.IsGrowing)
                {
                    string timerText = "GROWING: " + (10 - (int)_interactedStructure.GrowthTimer).ToString() + "S";
                    PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, timerText, new Vector2(menuRect.X + 50, menuRect.Top + 300), Color.White, 2);
                }
            }

            // Draw Build Menu
            if (_buildModeState == BuildModeState.Menu || _buildModeState == BuildModeState.Editing)
            {
                // Draw Menu Background (Left Side Vertical)
                int menuWidth = 60;
                int menuHeight = 6 * 50 + 7 * 5; // 6 slots * 50px + padding
                int menuX = 10;
                int menuY = (1080 - menuHeight) / 2;
                
                _spriteBatch.Draw(_pixelTexture, new Rectangle(menuX, menuY, menuWidth, menuHeight), Color.DarkGray);

                // Draw Slots
                for (int i = 0; i < 6; i++)
                {
                    Rectangle slotRect = new Rectangle(menuX + 5, menuY + 5 + i * 55, 50, 50);
                    
                    bool canBuild = true;
                    string costText = "";
                    
                    if (i == 0) // Greenhouse
                    {
                        // Cost: 2 Rocks, 1 Crystal
                        int rockCount = _player.Inventory.CountItem("Rock");
                        int crystalCount = _player.Inventory.CountItem("Crystal");
                        canBuild = rockCount >= 2 && crystalCount >= 1;
                        costText = "2 ROCK, 1 CRYSTAL";
                        
                        if (_textures.ContainsKey("greenhouse")) 
                            _spriteBatch.Draw(_textures["greenhouse"], slotRect, canBuild ? Color.White : Color.Gray * 0.5f);
                    }
                    else if (i == 1) // Workbench
                    {
                        // Cost: 3 Rocks
                        int rockCount = _player.Inventory.CountItem("Rock");
                        canBuild = rockCount >= 3;
                        costText = "3 ROCK";

                        if (_textures.ContainsKey("workbench")) 
                            _spriteBatch.Draw(_textures["workbench"], slotRect, canBuild ? Color.White : Color.Gray * 0.5f);
                    }
                    else if (i == 5) // Edit Button
                    {
                        _spriteBatch.Draw(_pixelTexture, slotRect, _buildModeState == BuildModeState.Editing ? Color.Green : Color.Blue);
                        string btnText = _buildModeState == BuildModeState.Editing ? "OK" : "EDIT"; // Short text for vertical
                        PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, btnText, new Vector2(slotRect.X + 5, slotRect.Y + 20), Color.White, 1);
                        continue; // Skip cost logic for button
                    }
                    else
                    {
                        _spriteBatch.Draw(_pixelTexture, slotRect, Color.Gray);
                        continue; // Empty slot
                    }

                    // Draw Cost Text (Hover or Always? Let's do always for now to be clear)
                    // Draw to the right of the slot
                    Color textColor = canBuild ? Color.White : Color.Red;
                    PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, costText, new Vector2(slotRect.Right + 10, slotRect.Y + 15), textColor, 1);
                }
            }

            // Draw Edit Mode Context Menu
            if (_showEditContextMenu)
            {
                _spriteBatch.Draw(_pixelTexture, new Rectangle((int)_editContextMenuPos.X, (int)_editContextMenuPos.Y, 80, 50), Color.White);
                
                // Move Option
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "MOVE", new Vector2(_editContextMenuPos.X + 5, _editContextMenuPos.Y + 5), Color.Black, 1);
                
                // Delete Option
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "DELETE", new Vector2(_editContextMenuPos.X + 5, _editContextMenuPos.Y + 25), Color.Red, 1);
            }


            // Draw Spaceship Menu
            if (_showSpaceshipMenu)
            {
                int menuWidth = 300;
                int menuHeight = 150;
                int menuX = (1920 - menuWidth) / 2;
                int menuY = (1080 - menuHeight) / 2;
                
                // Background
                _spriteBatch.Draw(_pixelTexture, new Rectangle(menuX, menuY, menuWidth, menuHeight), Color.DarkGray);
                _spriteBatch.Draw(_pixelTexture, new Rectangle(menuX + 5, menuY + 5, menuWidth - 10, menuHeight - 10), Color.Black);
                
                // Title
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "SPACESHIP", new Vector2(menuX + 100, menuY + 20), Color.White, 2);
                
                // Exit Button
                Rectangle exitBtn = new Rectangle(menuX + 250, menuY + 10, 40, 40);
                _spriteBatch.Draw(_pixelTexture, exitBtn, Color.Red);
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "X", new Vector2(exitBtn.X + 12, exitBtn.Y + 10), Color.White, 2);

                // Refill Button
                bool canAfford = _player.Inventory.CountItem("Rock") >= 2;
                Color btnColor = canAfford ? Color.Green : Color.Gray;
                Rectangle refillBtn = new Rectangle(menuX + 50, menuY + 60, 200, 40);
                
                _spriteBatch.Draw(_pixelTexture, refillBtn, btnColor);
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "REFILL OXYJIN", new Vector2(refillBtn.X + 20, refillBtn.Y + 10), Color.Black, 2);
                
                // Cost
                Color costColor = canAfford ? Color.White : Color.Red;
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "COST: 2 ROCKS", new Vector2(menuX + 80, menuY + 110), costColor, 2);
            }

            // Draw Inventory Context Menu
            if (_showInventoryContextMenu)
            {
                bool isFood = _contextMenuItem == "Corn";
                int menuHeight = isFood ? 50 : 25;
                
                _spriteBatch.Draw(_pixelTexture, new Rectangle((int)_contextMenuPos.X, (int)_contextMenuPos.Y, 60, menuHeight), Color.White);
                
                if (isFood)
                {
                    PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "EAT", new Vector2(_contextMenuPos.X + 5, _contextMenuPos.Y + 5), Color.Black, 2);
                    PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "DROP", new Vector2(_contextMenuPos.X + 5, _contextMenuPos.Y + 30), Color.Red, 2);
                }
                else
                {
                    PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "DROP", new Vector2(_contextMenuPos.X + 5, _contextMenuPos.Y + 5), Color.Red, 2);
                }
            }

            // Draw Death Screen
            if (_showDeathScreen)
            {
                // Dark overlay
                _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, 1920, 1080), Color.Black * 0.8f);
                
                // Death message
                string deathMessage = "YOU D.E.D.";
                string causeMessage = "";
                switch (_player.LastDeathCause)
                {
                    case DeathCause.Oxygen:
                        causeMessage = "YOUR OXYJIN DEPLETED - IF YOU KNOW, YOU KNOW...";
                        break;
                    case DeathCause.Hunger:
                        causeMessage = "YOU STARVED TO DEATH - YOU SHOULD HAVE EATEN THAT CRISPY CORN";
                        break;
                    case DeathCause.Alien:
                        causeMessage = "YOU WERE KILLED BY A METRO ALIEN - THANKS OLLIE...";
                        break;
                }
                
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, deathMessage, new Vector2(960 - 200, 400), Color.Red, 6);
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, causeMessage, new Vector2(960 - 300, 500), Color.White, 3);
                
                // Respawn button
                Rectangle respawnBtn = new Rectangle(960 - 150, 540 + 50, 300, 60);
                _spriteBatch.Draw(_pixelTexture, respawnBtn, Color.Green);
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "RESPAWN", new Vector2(respawnBtn.X + 50, respawnBtn.Y + 20), Color.Black, 3);
            }

            _spriteBatch.End();

            // Draw Minimap (UI Layer)
            _spriteBatch.Begin();
            if (_showMinimap)
            {
                int scale = _minimapSize == 1 ? MinimapScaleSmall : MinimapScaleLarge;
                int mapRange = 30; // Show 60x60 tiles (30 in each direction)
                int mapWidth = mapRange * 2 * scale;
                int mapHeight = mapRange * 2 * scale;
                int mapX = 1920 - mapWidth - 20;
                int mapY = 20;

                // Background
                _spriteBatch.Draw(_pixelTexture, new Rectangle(mapX - 2, mapY - 2, mapWidth + 4, mapHeight + 4), Color.Black * 0.5f);

                // Draw Tiles around player
                int playerTileX = (int)Math.Floor(_player.Position.X / TileMap.TileSize);
                int playerTileY = (int)Math.Floor(_player.Position.Y / TileMap.TileSize);
                
                for (int x = playerTileX - mapRange; x < playerTileX + mapRange; x++)
                {
                    for (int y = playerTileY - mapRange; y < playerTileY + mapRange; y++)
                    {
                        if (_tileMap.IsExplored(x, y))
                        {
                            int tileType = _tileMap.GetTile(x, y);
                            Color color = Color.Gray;
                            if (tileType == 1) color = Color.DarkGray; // Rock
                            else if (tileType == 2) color = Color.Black; // Crater
                            
                            int screenX = mapX + (x - (playerTileX - mapRange)) * scale;
                            int screenY = mapY + (y - (playerTileY - mapRange)) * scale;
                            _spriteBatch.Draw(_pixelTexture, new Rectangle(screenX, screenY, scale, scale), color);
                        }
                    }
                }

                // Draw Player (center of minimap)
                int playerScreenX = mapX + mapRange * scale;
                int playerScreenY = mapY + mapRange * scale;
                _spriteBatch.Draw(_pixelTexture, new Rectangle(playerScreenX, playerScreenY, scale, scale), Color.Red);
            }
            _spriteBatch.End();
            }

            base.Draw(gameTime);
        }
        private void DrawLine(SpriteBatch spriteBatch, Texture2D texture, Vector2 start, Vector2 end, Color color, int thickness)
        {
            Vector2 edge = end - start;
            float angle = (float)Math.Atan2(edge.Y, edge.X);
            spriteBatch.Draw(texture, new Rectangle((int)start.X, (int)start.Y, (int)edge.Length(), thickness), null, color, angle, new Vector2(0, 0.5f), SpriteEffects.None, 0);
        }
    }
}
