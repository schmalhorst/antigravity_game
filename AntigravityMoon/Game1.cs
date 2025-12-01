using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

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
            _graphics.PreferredBackBufferWidth = 800;
            _graphics.PreferredBackBufferHeight = 600;
            _graphics.ApplyChanges();
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _camera = new Camera(GraphicsDevice.Viewport);
            _tileMap = new TileMap(25, 19); // Fits 800x600 roughly with 32px tiles
            _entityManager = new EntityManager();
            _player = new Player(new Vector2(400, 304));

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

        protected override void Update(GameTime gameTime)
        {
            KeyboardState currentKeyboardState = Keyboard.GetState();
            MouseState currentMouseState = Mouse.GetState();
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || currentKeyboardState.IsKeyDown(Keys.Escape))
                Exit();

            // Toggle Inventory
            if (currentKeyboardState.IsKeyDown(Keys.I) && !_prevKeyboardState.IsKeyDown(Keys.I))
            {
                _showInventory = !_showInventory;
                _showWorkbenchMenu = false;
                _showGreenhouseMenu = false;
                _showInventoryContextMenu = false;
            }

            // Inventory Context Menu Logic
            if (_showInventory && _showInventoryContextMenu)
            {
                // Close if clicked outside
                if (currentMouseState.LeftButton == ButtonState.Pressed && _prevMouseState.LeftButton == ButtonState.Released)
                {
                    Rectangle eatButtonRect = new Rectangle((int)_contextMenuPos.X, (int)_contextMenuPos.Y, 60, 20);
                    if (eatButtonRect.Contains(currentMouseState.Position))
                    {
                        // Eat Action
                        if (_contextMenuItem == "Corn")
                        {
                            _player.Inventory.RemoveItem(_contextMenuItemGridPos.X, _contextMenuItemGridPos.Y);
                            _player.Eat(100f);
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
                    // Calculate grid pos (Hardcoded inventory pos 100, 100, cell size 40, padding 5)
                    int startX = 100;
                    int startY = 100;
                    int cellSize = 40;
                    int padding = 5;
                    
                    int mx = currentMouseState.X;
                    int my = currentMouseState.Y;
                    
                    if (mx >= startX && my >= startY)
                    {
                        int col = (mx - startX) / (cellSize + padding);
                        int row = (my - startY) / (cellSize + padding);
                        
                        if (col >= 0 && col < 8 && row >= 0 && row < 8)
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
                    
                    // Close Button (Top Right of Menu: 510, 160, 30, 30)
                    if (new Rectangle(510, 160, 30, 30).Contains(mousePos))
                    {
                        _showWorkbenchMenu = false;
                        _showGreenhouseMenu = false;
                        _interactedStructure = null;
                        return;
                    }

                    if (_showWorkbenchMenu)
                    {
                        // Upgrade Backpack Button (Rect: 300, 200, 200, 50)
                        if (new Rectangle(300, 200, 200, 50).Contains(mousePos))
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
                        // Plant Corn Button (Rect: 300, 200, 200, 50)
                        if (new Rectangle(300, 200, 200, 50).Contains(mousePos))
                        {
                            if (_interactedStructure != null && !_interactedStructure.IsGrowing && !_interactedStructure.IsReadyToHarvest)
                            {
                                if (_player.Inventory.RemoveItems("Crystal", 1))
                                {
                                    _interactedStructure.StartGrowing("Corn");
                                }
                            }
                        }
                        // Harvest Button (Rect: 300, 260, 200, 50)
                        if (new Rectangle(300, 260, 200, 50).Contains(mousePos))
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
                _player.Update(gameTime, _entityManager, _camera);
                _camera.Position = _player.Position; // Follow player

                // Check for interactions
                if (currentMouseState.LeftButton == ButtonState.Pressed && _prevMouseState.LeftButton == ButtonState.Released)
                {
                    Vector2 mouseWorldPos = _camera.ScreenToWorld(new Vector2(currentMouseState.X, currentMouseState.Y));
                    foreach (var entity in _entityManager.GetEntities())
                    {
                        if (entity is Structure s && Vector2.Distance(s.Position, mouseWorldPos) < 40)
                        {
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
                        int menuY = (600 - menuHeight) / 2;
                        
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

            // Apply Camera Transform
            _spriteBatch.Begin(transformMatrix: _camera.GetViewMatrix(), samplerState: SamplerState.PointClamp);

            _tileMap.Draw(_spriteBatch, _textures.ContainsKey("moon_ground") ? _textures["moon_ground"] : _pixelTexture);
            
            // Calculate Mouse World Pos for Hover Logic
            Vector2 mouseWorldPos = _camera.ScreenToWorld(new Vector2(Mouse.GetState().X, Mouse.GetState().Y));
            
            _entityManager.Draw(_spriteBatch, _textures, mouseWorldPos);
            
            _player.Draw(_spriteBatch, _textures.ContainsKey("astronaut") ? _textures["astronaut"] : _pixelTexture, _textures);

            _spriteBatch.End();

            // Draw UI (Inventory) separately without camera transform (Screen Space)
            _spriteBatch.Begin();
            
            if (_showInventory)
            {
                _player.Inventory.Draw(_spriteBatch, _pixelTexture, _textures, new Vector2(100, 100));
            }

            // Draw Hunger Bar
            _spriteBatch.Draw(_pixelTexture, new Rectangle(10, 10, 200, 20), Color.Gray);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(10, 10, (int)(_player.Hunger * 2), 20), Color.Orange);
            // Simple text would be better but we don't have SpriteFont loaded yet. 
            // We'll assume orange bar is hunger.

            // Draw Menus
            if (_showWorkbenchMenu)
            {
                // Background
                _spriteBatch.Draw(_pixelTexture, new Rectangle(250, 150, 300, 200), Color.DarkGray);
                // Close Button
                _spriteBatch.Draw(_pixelTexture, new Rectangle(510, 160, 30, 30), Color.Red);
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "X", new Vector2(518, 168), Color.White, 2);
                
                // Title
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "WORKBENCH", new Vector2(260, 160), Color.White, 2);

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
                    _spriteBatch.Draw(_pixelTexture, new Rectangle(300, 200, 200, 50), canUpgrade ? Color.Blue : Color.Gray);
                    PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "UPGRADE BAG", new Vector2(310, 215), Color.White, 2);
                    PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, costText, new Vector2(310, 235), canUpgrade ? Color.White : Color.Red, 1);
                }
                else
                {
                    PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "MAX LEVEL", new Vector2(310, 215), Color.Gold, 2);
                }
            }
            else if (_showGreenhouseMenu)
            {
                // Background
                _spriteBatch.Draw(_pixelTexture, new Rectangle(250, 150, 300, 200), Color.DarkGreen);
                // Close Button
                _spriteBatch.Draw(_pixelTexture, new Rectangle(510, 160, 30, 30), Color.Red);
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "X", new Vector2(518, 168), Color.White, 2);

                // Title
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "GREENHOUSE", new Vector2(260, 160), Color.White, 2);

                // Plant Button
                Color plantColor = (_interactedStructure != null && !_interactedStructure.IsGrowing && !_interactedStructure.IsReadyToHarvest) ? Color.Yellow : Color.Gray;
                _spriteBatch.Draw(_pixelTexture, new Rectangle(300, 200, 200, 50), plantColor);
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "PLANT CORN", new Vector2(320, 215), Color.Black, 2);
                
                // Draw Cost (1 Crystal)
                if (_textures.ContainsKey("crystal"))
                {
                    _spriteBatch.Draw(_textures["crystal"], new Rectangle(510, 210, 30, 30), Color.White);
                    PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "x1", new Vector2(545, 220), Color.White, 2);
                }
                
                // Harvest Button
                Color harvestColor = (_interactedStructure != null && _interactedStructure.IsReadyToHarvest) ? Color.Green : Color.Gray;
                _spriteBatch.Draw(_pixelTexture, new Rectangle(300, 260, 200, 50), harvestColor);
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "HARVEST", new Vector2(340, 275), Color.Black, 2);

                // Growth Timer
                if (_interactedStructure != null && _interactedStructure.IsGrowing)
                {
                    string timerText = "GROWING: " + (10 - (int)_interactedStructure.GrowthTimer).ToString() + "S";
                    PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, timerText, new Vector2(300, 320), Color.White, 2);
                }
            }

            // Draw Build Menu
            if (_buildModeState == BuildModeState.Menu || _buildModeState == BuildModeState.Editing)
            {
                // Draw Menu Background (Left Side Vertical)
                int menuWidth = 60;
                int menuHeight = 6 * 50 + 7 * 5; // 6 slots * 50px + padding
                int menuX = 10;
                int menuY = (600 - menuHeight) / 2;
                
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


            // Draw Inventory Context Menu
            if (_showInventoryContextMenu)
            {
                _spriteBatch.Draw(_pixelTexture, new Rectangle((int)_contextMenuPos.X, (int)_contextMenuPos.Y, 60, 25), Color.White);
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "EAT", new Vector2(_contextMenuPos.X + 5, _contextMenuPos.Y + 5), Color.Black, 2);
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
