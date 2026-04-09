using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Audio;
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

        
        private Texture2D _floppyIcon;
        private bool _saveExists;
        private const string SaveFileName = "colony_save.json";
        private float _saveAnimationTimer = 0f;
        private float _inventoryFullTimer = 0f;

        private Texture2D _vignetteTexture;
        private Song _backgroundMusic;
        private Song _tunnelMusic;   // Cinematic plucky strings — loaded from Sounds/tunnel_ambience
        private SoundEffect _laserSound;

        // Lava Tunnel state
        private bool _inTunnel = false;

        // Camera Zoom
        private float _targetZoom = 2.0f;
        private const float MinZoom = 1.0f;
        private const float MaxZoom = 4.0f;
        private int _prevScrollValue = 0;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 1920;
            _graphics.PreferredBackBufferHeight = 1080;
            _graphics.ApplyChanges();
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.AllowUserResizing = true;
        }

        protected override void Initialize()
        {
            _camera = new Camera(GraphicsDevice.Viewport);
            _camera.Zoom = 2.0f; // Default closer zoom
            _tileMap = new TileMap(); // Infinite map
            _entityManager = new EntityManager();
            _player = new Player(Vector2.Zero); // Start at world origin
            _player.OnInventoryFull += () => { _inventoryFullTimer = 2.0f; };

            // Subscribe to entity spawning from TileMap
            _tileMap.OnSpawnEntity += (position, entityType) =>
            {
                _entityManager.AddEntity(new Entity(position, entityType, true, true, true));
            };

            Window.ClientSizeChanged += (s, e) =>
            {
                 if (_camera != null)
                 {
                     _camera.UpdateViewport(GraphicsDevice.Viewport);
                 }
            };

            base.Initialize();
            _saveExists = File.Exists(SaveFileName);
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
            LoadTexture("World", "astronaut");
            LoadTexture("World", "moon_ground");
            LoadTexture("World", "rock");
            LoadTexture("World", "crystal");
            LoadTexture("World", "metal");
            LoadTexture("Structures", "greenhouse");
            LoadTexture("Structures", "workbench");
            LoadTexture("Items", "corn");
            LoadTexture("Items", "potato");
            LoadTexture("World", "metro_alien");
            LoadTexture("World", "the_destroyer");
            LoadTexture("World", "cave_entrance");
            
            // Structures
            LoadTexture("Structures", "spaceship");
            LoadTexture("Structures", "spaceship_broken1");
            LoadTexture("Structures", "spaceship_broken2");
            LoadTexture("Structures", "spaceship_broken3");
            LoadTexture("Structures", "mineral");
            LoadTexture("Structures", "bionic_tech");
            LoadTexture("Structures", "reactor");
            LoadTexture("Structures", "reactor_empty");
            LoadTexture("Structures", "radar");
            LoadTexture("Structures", "wormhole");
            LoadTexture("World", "floppy");
            LoadTexture("Structures", "hab");
            LoadTexture("Structures", "machinery");

            _textures["backpack"] = Content.Load<Texture2D>("Images/World/backpack");
            _textures["skull_crossbones"] = Content.Load<Texture2D>("Images/World/skull_crossbones");
            
            // Try loading crops.json
            string cropsPath = Path.Combine(Content.RootDirectory, "crops.json");
            if (File.Exists(cropsPath))
            {
                try
                {
                    string jsonString = File.ReadAllText(cropsPath);
                    _crops = JsonSerializer.Deserialize<List<CropData>>(jsonString);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading crops.json: {ex.Message}");
                }
            }
            
            // Generate Textures for Crops if they don't exist
            foreach (var crop in _crops)
            {
               try { _textures[crop.Id.ToLower()] = Content.Load<Texture2D>($"Images/Items/{crop.Id.ToLower()}"); }
               catch { /* Missing texture, fallback logic in drawing */ }
            }
            
            _backpackTexture = _textures["backpack"];
            _skullTexture = _textures["skull_crossbones"];
            _spaceshipTexture = _textures["spaceship"];
            _spaceshipPosition = new Vector2(400, -100); // Start off screen
            
            // Generate vignette programmatically instead of relying on an image
            _vignetteTexture = GenerateVignetteTexture(512);
            if (_textures.ContainsKey("floppy")) _floppyIcon = _textures["floppy"];

            // Load Global Font
            PixelTextRenderer.Font = Content.Load<SpriteFont>("DefaultFont");

            try
            {
                _backgroundMusic = Content.Load<Song>("Sounds/eerie_piano_loop");
                _laserSound = Content.Load<SoundEffect>("Sounds/pew");
                MediaPlayer.IsRepeating = true;
                MediaPlayer.Volume = 0.5f;
                MediaPlayer.Play(_backgroundMusic);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading background music: {ex.Message}");
            }

            // Load tunnel ambience music (cinematic plucky strings).
            // TODO: Replace tunnel_ambience.wav with proper cinematic plucky strings track.
            try
            {
                _tunnelMusic = Content.Load<Song>("Sounds/tunnel_ambience");
            }
            catch
            {
                // Fallback: reuse background music if tunnel_ambience.wav is not yet added.
                _tunnelMusic = _backgroundMusic;
            }

            // Metal texture is now loaded from Content/Images/World/metal.png above
        }

        private void LoadTexture(string folder, string name)
        {
            string path = Path.Combine("Content", "Images", folder, name + ".png");
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
        private List<CropData> _crops = new List<CropData>();
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
        private Backpack _interactedBackpack = null;
        // Edit Mode Context Menu
        private bool _showEditContextMenu = false;
        private Vector2 _editContextMenuPos;
        private Structure _editTargetStructure = null;
        private Structure _hoveredEditStructure = null;

        // Game State
        public enum GameState { StartMenu, Intro, Playing }
        private GameState _currentGameState = GameState.StartMenu;
        private float _introTimer = 0f;
        private Vector2 _spaceshipPosition;
        private Texture2D _spaceshipTexture;
        
        // Alien Logic
        private List<Alien> _aliens = new List<Alien>();
        private float _surfaceAlienTimer = 0f;
        private bool _surfaceAlienSpawned = false;
        private float _caveAlienTimer = 0f;
        private bool _caveAlienSpawned = false;
        
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

        // Electricity Particle System (for alien deaths)
        private struct ElectricityParticle
        {
            public Vector2 Start;
            public Vector2 End;
            public float Duration;
            public float MaxDuration;
            public Vector2 RandomOffset; // For flickering effect
        }
        private List<ElectricityParticle> _electricityParticles = new List<ElectricityParticle>();

        // Minimap
        private bool _showMinimap = true;
        private int _minimapSize = 1; // 0=Hidden, 1=Small, 2=Large
        private Texture2D _minimapFrameTexture; // Optional
        
        // Death Drop
        private bool _showLootMenu = false;
        private Texture2D _backpackTexture;
        private Texture2D _skullTexture;
        
        // Backpack Context Menu
        private bool _showBackpackContextMenu = false;
        private Vector2 _backpackContextMenuPos;
        private string _backpackContextMenuItem;
        private Point _backpackContextMenuItemGridPos;
        private int _backpackContextMenuItemCount; // Track stack count
        private const int MinimapScaleSmall = 4;
        private const int MinimapScaleLarge = 8;

        // Contribution Popup
        private bool _showContributePopup = false;
        private Dictionary<string, int> _contributeAmounts = new Dictionary<string, int>();
        private int _contributeFocusedField = -1; // Which field is being typed into (-1 = none)
        private string _contributeTypingBuffer = "";
        private List<string> _contributeMaterialKeys = new List<string>(); // Ordered keys for display

        private Vector2 GetInventoryPosition()
        {
            int cellSize = 40;
            int padding = 5;
            int width = _player.Inventory.Cols * (cellSize + padding) + padding;
            int height = _player.Inventory.Rows * (cellSize + padding) + padding;
            
            int x = (GraphicsDevice.Viewport.Width - width) / 2; // Centered
            int y = GraphicsDevice.Viewport.Height - height - 20; // 20px from bottom
            
            return new Vector2(x, y);
        }

        /// <summary>
        /// Returns the material costs for each spaceship upgrade stage.
        /// Extensible: just add new entries to the dictionaries for new materials.
        /// </summary>
        private Dictionary<string, int> GetSpaceshipUpgradeCosts(int currentRepairStage)
        {
            switch (currentRepairStage)
            {
                case 0: // Initial Repair - get the systems online
                case 1: // Stage 1 -> 2
                    return new Dictionary<string, int>
                    {
                        { "Metal", 500 },
                        { "Crystal", 500 },
                        { "Rock", 500 }
                    };
                case 2: // Stage 2 -> 3
                    return new Dictionary<string, int>
                    {
                        { "Metal", 10000 },
                        { "Crystal", 10000 },
                        { "Rock", 10000 }
                    };
                case 3: // Stage 3 -> 4 (Full Restoration)
                    return new Dictionary<string, int>
                    {
                        { "Metal", 999999 },
                        { "Crystal", 999999 },
                        { "Rock", 999999 }
                    };
                default:
                    return null;
            }
        }

        /// <summary>
        /// Commits the current typing buffer to the focused contribution field,
        /// clamping to the valid range (0 to min(inventory, still needed)).
        /// </summary>
        private void CommitContributeTyping()
        {
            if (_contributeFocusedField < 0 || _contributeFocusedField >= _contributeMaterialKeys.Count) return;
            
            string mat = _contributeMaterialKeys[_contributeFocusedField];
            if (int.TryParse(_contributeTypingBuffer, out int val))
            {
                int have = _player.Inventory.CountItem(mat);
                var costs = GetSpaceshipUpgradeCosts(_interactedStructure != null ? _interactedStructure.RepairStage : 0);
                int stillNeeded = (costs != null && costs.ContainsKey(mat)) ? costs[mat] - (_interactedStructure != null ? _interactedStructure.GetContributed(mat) : 0) : 0;
                int maxVal = Math.Min(have, Math.Max(stillNeeded, 0));
                _contributeAmounts[mat] = Math.Clamp(val, 0, maxVal);
            }
            else
            {
                _contributeAmounts[mat] = 0;
            }
            _contributeTypingBuffer = _contributeAmounts[mat].ToString();
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState currentKeyboardState = Keyboard.GetState();
            MouseState currentMouseState = Mouse.GetState();
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // --- Camera Zoom (scroll wheel + Z/X keys) ---
            int currentScrollValue = currentMouseState.ScrollWheelValue;
            int scrollDelta = currentScrollValue - _prevScrollValue;
            _prevScrollValue = currentScrollValue;

            if (scrollDelta > 0)
                _targetZoom = Math.Clamp(_targetZoom + 0.25f, MinZoom, MaxZoom);
            else if (scrollDelta < 0)
                _targetZoom = Math.Clamp(_targetZoom - 0.25f, MinZoom, MaxZoom);

            if (currentKeyboardState.IsKeyDown(Keys.Z) && !_prevKeyboardState.IsKeyDown(Keys.Z))
                _targetZoom = Math.Clamp(_targetZoom + 0.25f, MinZoom, MaxZoom);
            if (currentKeyboardState.IsKeyDown(Keys.X) && !_prevKeyboardState.IsKeyDown(Keys.X))
                _targetZoom = Math.Clamp(_targetZoom - 0.25f, MinZoom, MaxZoom);

            // Smooth lerp towards target zoom
            _camera.Zoom += (_targetZoom - _camera.Zoom) * Math.Min(dt * 12f, 1f);

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || (currentKeyboardState.IsKeyDown(Keys.Escape) && !_showContributePopup))
                Exit();

            // Fullscreen Toggle
            if (currentKeyboardState.IsKeyDown(Keys.F11) && !_prevKeyboardState.IsKeyDown(Keys.F11))
            {
                _graphics.ToggleFullScreen();
            }

            // Contribution Popup keyboard input
            if (_showContributePopup && _contributeFocusedField >= 0)
            {
                // Number keys (0-9)
                Keys[] numKeys = { Keys.D0, Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9 };
                Keys[] numPadKeys = { Keys.NumPad0, Keys.NumPad1, Keys.NumPad2, Keys.NumPad3, Keys.NumPad4, Keys.NumPad5, Keys.NumPad6, Keys.NumPad7, Keys.NumPad8, Keys.NumPad9 };
                for (int k = 0; k < 10; k++)
                {
                    if ((currentKeyboardState.IsKeyDown(numKeys[k]) && !_prevKeyboardState.IsKeyDown(numKeys[k])) ||
                        (currentKeyboardState.IsKeyDown(numPadKeys[k]) && !_prevKeyboardState.IsKeyDown(numPadKeys[k])))
                    {
                        _contributeTypingBuffer += k.ToString();
                        // Clamp to valid range
                        if (int.TryParse(_contributeTypingBuffer, out int val))
                        {
                            string mat = _contributeMaterialKeys[_contributeFocusedField];
                            int have = _player.Inventory.CountItem(mat);
                            var costs = GetSpaceshipUpgradeCosts(_interactedStructure.RepairStage);
                            int stillNeeded = (costs != null && costs.ContainsKey(mat)) ? costs[mat] - _interactedStructure.GetContributed(mat) : 0;
                            int maxVal = Math.Min(have, stillNeeded);
                            val = Math.Clamp(val, 0, maxVal);
                            _contributeAmounts[mat] = val;
                            _contributeTypingBuffer = val.ToString();
                        }
                    }
                }
                
                // Backspace
                if (currentKeyboardState.IsKeyDown(Keys.Back) && !_prevKeyboardState.IsKeyDown(Keys.Back))
                {
                    if (_contributeTypingBuffer.Length > 0)
                        _contributeTypingBuffer = _contributeTypingBuffer.Substring(0, _contributeTypingBuffer.Length - 1);
                    if (_contributeTypingBuffer.Length == 0) _contributeTypingBuffer = "0";
                    if (int.TryParse(_contributeTypingBuffer, out int val))
                    {
                        string mat = _contributeMaterialKeys[_contributeFocusedField];
                        _contributeAmounts[mat] = val;
                    }
                }
                
                // Tab to cycle fields
                if (currentKeyboardState.IsKeyDown(Keys.Tab) && !_prevKeyboardState.IsKeyDown(Keys.Tab))
                {
                    CommitContributeTyping();
                    _contributeFocusedField = (_contributeFocusedField + 1) % _contributeMaterialKeys.Count;
                    string nextMat = _contributeMaterialKeys[_contributeFocusedField];
                    _contributeTypingBuffer = _contributeAmounts[nextMat].ToString();
                }
                
                // Enter to confirm
                if (currentKeyboardState.IsKeyDown(Keys.Enter) && !_prevKeyboardState.IsKeyDown(Keys.Enter))
                {
                    CommitContributeTyping();
                    _contributeFocusedField = -1;
                }
            }
            
            // Escape closes contribute popup
            if (_showContributePopup && currentKeyboardState.IsKeyDown(Keys.Escape) && !_prevKeyboardState.IsKeyDown(Keys.Escape))
            {
                _showContributePopup = false;
            }

            if (_currentGameState == GameState.StartMenu)
            {
                // UI Buttons
                Rectangle newGameBtn = new Rectangle(500, 600, 300, 50);
                Rectangle loadGameBtn = new Rectangle(500, 670, 300, 50);

                // Mouse Interaction
                if (currentMouseState.LeftButton == ButtonState.Pressed && _prevMouseState.LeftButton == ButtonState.Released)
                {
                    if (newGameBtn.Contains(currentMouseState.Position))
                    {
                        // New Game
                        _currentGameState = GameState.Intro;
                        _spaceshipPosition = new Vector2(400 - 32, -100); 
                    }
                    else if (_saveExists && loadGameBtn.Contains(currentMouseState.Position))
                    {
                        // Load Game
                        LoadGame();
                        // Note: LoadGame sets state to Playing directly, skipping Intro
                    }
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
                        Structure ship = new Structure(_spaceshipPosition, "Spaceship", 384, 168);
                        ship.RepairStage = 1;
                        _entityManager.AddEntity(ship);
                        
                        // Ensure player is at a spawn point below the ship
                        _player.Position = new Vector2(_spaceshipPosition.X + 192, _spaceshipPosition.Y + 180);
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
                    Rectangle respawnBtn = new Rectangle(GraphicsDevice.Viewport.Width / 2 - 150, GraphicsDevice.Viewport.Height / 2 + 50, 300, 60);
                    if (respawnBtn.Contains(currentMouseState.Position))
                    {
                        // Drop Backpack before respawning
                        if (!_player.Inventory.IsEmpty())
                        {
                            Backpack droppedPack = new Backpack(_player.Position, _player.Inventory.Clone());
                            _entityManager.AddEntity(droppedPack);
                        }

                        _showDeathScreen = false;
                        _player.DoRespawn();
                        // Reset game state for respawn
                        _surfaceAlienSpawned = false;
                        _surfaceAlienTimer = 0f;
                        _caveAlienSpawned = false;
                        _caveAlienTimer = 0f;
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
                if (_showInventory)
                {
                    _buildModeState = BuildModeState.None; // Close Build Menu
                    _showWorkbenchMenu = false;
                    _showGreenhouseMenu = false;
                    _showSpaceshipMenu = false;
                    _showInventoryContextMenu = false;
                }
            }

            // Minimap Toggle
            if (currentKeyboardState.IsKeyDown(Keys.M) && !_prevKeyboardState.IsKeyDown(Keys.M))
            {
                _minimapSize++;
                if (_minimapSize > 2) _minimapSize = 1;
            }

            // Update Exploration
            _tileMap.Explore(_player.Position, 300f); // 300px radius

            // --- Stardew Valley Tunnel Teleportation ---
            int currentTileX = (int)Math.Floor(_player.Position.X / TileMap.TileSize);
            int currentTileY = (int)Math.Floor(_player.Position.Y / TileMap.TileSize);
            int currentTile = _tileMap.GetTile(currentTileX, currentTileY);

            bool nearEntrance = false;
            bool nearExit = false;

            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dy = -2; dy <= 2; dy++)
                {
                    int tile = _tileMap.GetTile(currentTileX + dx, currentTileY + dy);
                    if (tile == 3) nearEntrance = true;
                    if (tile == 6) nearExit = true;
                }
            }

            if (nearEntrance && currentKeyboardState.IsKeyDown(Keys.E) && !_prevKeyboardState.IsKeyDown(Keys.E))
            {
                // Warp Underground (minus exactly 1000 chunks)
                _player.Position = new Vector2(_player.Position.X, _player.Position.Y - 1_024_000);
            }
            else if (nearExit && currentKeyboardState.IsKeyDown(Keys.E) && !_prevKeyboardState.IsKeyDown(Keys.E))
            {
                // Warp Surface
                _player.Position = new Vector2(_player.Position.X, _player.Position.Y + 1_024_000);
            }

            // --- Lava Tunnel Detection ---
            bool nearTunnel = _player.Position.Y < -500_000;
            if (nearTunnel && !_inTunnel)
            {
                _inTunnel = true;
                // Swap to tunnel music
                if (_tunnelMusic != null)
                {
                    MediaPlayer.Stop();
                    MediaPlayer.IsRepeating = true;
                    MediaPlayer.Volume = 0.6f;
                    MediaPlayer.Play(_tunnelMusic);
                }
            }
            else if (!nearTunnel && _inTunnel)
            {
                _inTunnel = false;
                // Swap back to background music
                if (_backgroundMusic != null)
                {
                    MediaPlayer.Stop();
                    MediaPlayer.IsRepeating = true;
                    MediaPlayer.Volume = 0.5f;
                    MediaPlayer.Play(_backgroundMusic);
                }
            }

            // Inventory Context Menu Logic
            if (_showInventory && _showInventoryContextMenu)
            {
                if (currentMouseState.LeftButton == ButtonState.Pressed && _prevMouseState.LeftButton == ButtonState.Released)
                {
                    bool isFood = _crops.Exists(c => c.Id == _contextMenuItem);
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
                            var cropData = _crops.Find(c => c.Id == _contextMenuItem);
                            if (cropData != null)
                            {
                                _player.Inventory.RemoveItem((int)_contextMenuItemGridPos.X, (int)_contextMenuItemGridPos.Y);
                                _player.Eat(100f);
                                _player.AddBuff(cropData.HungerBuffReward, cropData.OxygenBuffReward, cropData.BuffDuration);
                            }
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
                // Calculate Inventory Rect for input detection
                Vector2 invPos = GetInventoryPosition();
                int cellSize = 40;
                int padding = 5;
                int invWidth = _player.Inventory.Cols * (cellSize + padding) + padding;
                int invHeight = _player.Inventory.Rows * (cellSize + padding) + padding;
                Rectangle inventoryRect = new Rectangle((int)invPos.X, (int)invPos.Y, invWidth, invHeight);

                // Right Click on Item to open Context Menu
                if (currentMouseState.RightButton == ButtonState.Pressed && _prevMouseState.RightButton == ButtonState.Released)
                {
                    int startX = (int)invPos.X;
                    int startY = (int)invPos.Y;
                    
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
                
                // Sort Button Logic
                int invWidthVal = _player.Inventory.Cols * 45 + 5; // Use same calc as Draw
                Rectangle sortBtn = new Rectangle((int)invPos.X + invWidthVal - 60, (int)invPos.Y - 25, 60, 20);
                
                if (currentMouseState.LeftButton == ButtonState.Pressed && _prevMouseState.LeftButton == ButtonState.Released)
                {
                    if (sortBtn.Contains(currentMouseState.Position))
                    {
                        _player.Inventory.MergeAndSort();
                    }
                    // Left Click Outside to Close (excluding sort button)
                    else if (!inventoryRect.Contains(currentMouseState.Position))
                    {
                        _showInventory = false;
                    }
                }
            }

            // Handle Loot Menu Input
            if (_showLootMenu && _interactedBackpack != null)
            {
                if (currentKeyboardState.IsKeyDown(Keys.Escape) || !_interactedBackpack.GetBounds().Intersects(new Rectangle((int)_player.Position.X - 60, (int)_player.Position.Y - 60, 160, 160)))
                {
                    _showLootMenu = false;
                    _interactedBackpack = null;
                    _showBackpackContextMenu = false;
                }
                
                // Handle Backpack Context Menu
                if (_showBackpackContextMenu)
                {
                    if (currentMouseState.LeftButton == ButtonState.Pressed && _prevMouseState.LeftButton == ButtonState.Released)
                    {
                        int menuHeight = _backpackContextMenuItemCount > 1 ? 50 : 25;
                        Rectangle lootRect = new Rectangle((int)_backpackContextMenuPos.X, (int)_backpackContextMenuPos.Y, 80, 25);
                        Rectangle lootAllRect = new Rectangle((int)_backpackContextMenuPos.X, (int)_backpackContextMenuPos.Y + 25, 80, 25);
                        
                        if (lootRect.Contains(currentMouseState.Position))
                        {
                            // Loot single item
                            if (_backpackContextMenuItem != null)
                            {
                                if (_player.Inventory.AddItem(_backpackContextMenuItem))
                                {
                                    _interactedBackpack.Storage.RemoveItem(_backpackContextMenuItemGridPos.X, _backpackContextMenuItemGridPos.Y);
                                    
                                    // If backpack empty, destroy it
                                    if (_interactedBackpack.Storage.IsEmpty())
                                    {
                                        _entityManager.RemoveEntity(_interactedBackpack);
                                        _showLootMenu = false;
                                        _interactedBackpack = null;
                                    }
                                }
                            }
                            _showBackpackContextMenu = false;
                        }
                        else if (_backpackContextMenuItemCount > 1 && lootAllRect.Contains(currentMouseState.Position))
                        {
                            // Loot all items in stack
                            if (_backpackContextMenuItem != null)
                            {
                                int count = _backpackContextMenuItemCount;
                                bool success = true;
                                
                                // Try to add all items
                                for (int i = 0; i < count; i++)
                                {
                                    if (!_player.Inventory.AddItem(_backpackContextMenuItem))
                                    {
                                        success = false;
                                        break;
                                    }
                                }
                                
                                if (success)
                                {
                                    // Remove all from backpack
                                    for (int i = 0; i < count; i++)
                                    {
                                        _interactedBackpack.Storage.RemoveItem(_backpackContextMenuItemGridPos.X, _backpackContextMenuItemGridPos.Y);
                                    }
                                    
                                    // If backpack empty, destroy it
                                    if (_interactedBackpack.Storage.IsEmpty())
                                    {
                                        _entityManager.RemoveEntity(_interactedBackpack);
                                        _showLootMenu = false;
                                        _interactedBackpack = null;
                                    }
                                }
                            }
                            _showBackpackContextMenu = false;
                        }
                        else
                        {
                            _showBackpackContextMenu = false;
                        }
                    }
                }
                else
                {
                    // Right Click to Loot Item (open context menu)
                    if (currentMouseState.RightButton == ButtonState.Pressed && _prevMouseState.RightButton == ButtonState.Released)
                    {
                        Vector2 lootPos = new Vector2(GraphicsDevice.Viewport.Width / 2 - 200, GraphicsDevice.Viewport.Height / 2 - 150);
                        int startX = (int)lootPos.X;
                        int startY = (int)lootPos.Y;
                        int cellSize = 40;
                        int padding = 5;
                        
                        int mx = currentMouseState.X;
                        int my = currentMouseState.Y;
                        
                        if (mx >= startX && my >= startY)
                        {
                            int col = (mx - startX) / (cellSize + padding);
                            int row = (my - startY) / (cellSize + padding);
                            
                            if (col >= 0 && col < _interactedBackpack.Storage.Cols && row >= 0 && row < _interactedBackpack.Storage.Rows)
                            {
                                string item = _interactedBackpack.Storage.GetItem(col, row);
                                if (item != null)
                                {
                                    _showBackpackContextMenu = true;
                                    _backpackContextMenuPos = new Vector2(mx, my);
                                    _backpackContextMenuItem = item;
                                    _backpackContextMenuItemGridPos = new Point(col, row);
                                    _backpackContextMenuItemCount = _interactedBackpack.Storage.GetItemCount(col, row);
                                }
                            }
                        }
                    }
                }
                
                // Handle Collect All button
                if (currentMouseState.LeftButton == ButtonState.Pressed && _prevMouseState.LeftButton == ButtonState.Released)
                {
                    Rectangle collectAllBtn = new Rectangle(GraphicsDevice.Viewport.Width / 2 + 220, GraphicsDevice.Viewport.Height / 2 - 150, 150, 40);
                    
                    if (collectAllBtn.Contains(currentMouseState.Position))
                    {
                        // Try to transfer all items (Best Effort)
                        bool backpackEmpty = true;
                        
                        for (int y = 0; y < _interactedBackpack.Storage.Rows; y++)
                        {
                            for (int x = 0; x < _interactedBackpack.Storage.Cols; x++)
                            {
                                string item = _interactedBackpack.Storage.GetItem(x, y);
                                if (item != null)
                                {
                                    int stackCount = _interactedBackpack.Storage.GetItemCount(x, y);
                                    
                                    // Try to transfer each item in the stack
                                    for (int i = 0; i < stackCount; i++)
                                    {
                                        if (_player.Inventory.AddItem(item))
                                        {
                                            _interactedBackpack.Storage.RemoveItem(x, y);
                                        }
                                        else
                                        {
                                            backpackEmpty = false; // Couldn't take everything
                                            // Don't break completely, try other items (maybe they stack)
                                        }
                                    }
                                    
                                    // Check if slot still has items (partially full or full)
                                    if (_interactedBackpack.Storage.GetItem(x, y) != null)
                                    {
                                        backpackEmpty = false;
                                    }
                                }
                            }
                        }
                        
                        if (_interactedBackpack.Storage.IsEmpty())
                        {
                            // Destroy backpack
                            _entityManager.RemoveEntity(_interactedBackpack);
                            _showLootMenu = false;
                            _interactedBackpack = null;
                            _showBackpackContextMenu = false;
                        }
                    }
                }
            }

            // Close Menus with Escape or moving away
            if (_showWorkbenchMenu || _showGreenhouseMenu || _showSpaceshipMenu)
            {
                if (currentKeyboardState.IsKeyDown(Keys.Escape))
                {
                    _showWorkbenchMenu = false;
                    _showGreenhouseMenu = false;
                    _showSpaceshipMenu = false;
                    _interactedStructure = null;
                }
                else if (_interactedStructure != null && !_interactedStructure.GetBounds().Intersects(new Rectangle((int)_player.Position.X - 60, (int)_player.Position.Y - 60, 160, 160)))
                {
                    _showWorkbenchMenu = false;
                    _showGreenhouseMenu = false;
                    _interactedStructure = null;
                }
                
                // Handle Menu Clicks (Simple UI logic)
                if (currentMouseState.LeftButton == ButtonState.Pressed && _prevMouseState.LeftButton == ButtonState.Released)
                {
                    // Check UI bounds (Dynamic)
                    Vector2 mousePos = new Vector2(currentMouseState.X, currentMouseState.Y);
                    
                    // Menu Rect: Centered 600x400
                    int menuWidth = Math.Min(GraphicsDevice.Viewport.Width - 40, 600);
                    int menuHeight = 400; // Default for Workbench
                    if (_showGreenhouseMenu)
                    {
                        menuHeight = Math.Min(GraphicsDevice.Viewport.Height - 40, 200 + (_crops.Count * 80) + 120); // 200 base + 80 per crop + 120 for harvest/timer
                    }
                    Rectangle menuRect = new Rectangle((GraphicsDevice.Viewport.Width - menuWidth) / 2, (GraphicsDevice.Viewport.Height - menuHeight) / 2, menuWidth, menuHeight);

                    // Close Button (Right - 40, Top + 10) -> (660+600-40, 340+10) = (1220, 350, 30, 30)
                    if (new Rectangle(menuRect.Right - 40, menuRect.Top + 10, 30, 30).Contains(mousePos))
                    {
                        _showWorkbenchMenu = false;
                        _showGreenhouseMenu = false;
                        _interactedStructure = null;
                        _prevMouseState = currentMouseState; // Update state before returning
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
                        int cropIndex = 0;
                        int startYOffset = 100;
                        int buttonSpacing = 80;
                        
                        foreach (var crop in _crops)
                        {
                            Rectangle plantBtnRect = new Rectangle(menuRect.X + 50, menuRect.Top + startYOffset + (cropIndex * buttonSpacing), 300, 60);
                            
                            if (plantBtnRect.Contains(mousePos))
                            {
                                if (_interactedStructure != null)
                                {
                                    bool canAfford = true;
                                    foreach (var cost in crop.Costs)
                                    {
                                        if (_player.Inventory.CountItem(cost.Key) < cost.Value)
                                        {
                                            canAfford = false;
                                            break;
                                        }
                                    }

                                    if (canAfford)
                                    {
                                        foreach (var cost in crop.Costs)
                                        {
                                            _player.Inventory.RemoveItems(cost.Key, cost.Value);
                                        }
                                        _interactedStructure.StartGrowing(crop.Id, crop.GrowthTime);
                                    }
                                }
                            }
                            cropIndex++;
                        }

                        // Harvest Button is below the crop list
                        int harvestYOffset = startYOffset + (_crops.Count * buttonSpacing) + 20;
                        Rectangle harvestBtnRect = new Rectangle(menuRect.X + 50, menuRect.Top + harvestYOffset, 300, 60);
                        
                        if (harvestBtnRect.Contains(mousePos))
                        {
                             if (_interactedStructure != null && _interactedStructure.IsReadyToHarvest)
                             {
                                 bool harvestedAny = false;
                                 while (_interactedStructure.ReadyCount > 0)
                                 {
                                     // Peek at what crop is ready
                                     string readyCrop = _interactedStructure.CropType;
                                     if (string.IsNullOrEmpty(readyCrop)) readyCrop = "Corn"; // Fallback just in case
                                     
                                     // Check if we can add the specific Crop 
                                     if (_player.Inventory.CanAddItem(readyCrop))
                                     {
                                         string crop = _interactedStructure.Harvest();
                                         if (crop != null)
                                         {
                                             _player.Inventory.AddItem(crop);
                                             harvestedAny = true;
                                         }
                                     }
                                     else
                                     {
                                         // Inventory Full Warning
                                         _inventoryFullTimer = 2.0f;
                                         break; // Stop harvesting if inventory is full
                                     }
                                 }
                             }
                        }
                    }
                    else if (_showSpaceshipMenu)
                    {
                        // Menu Rect: Centered 350x350
                        int spWidth = 350;
                        int spHeight = 350;
                        int spX = (GraphicsDevice.Viewport.Width - spWidth) / 2;
                        int spY = (GraphicsDevice.Viewport.Height - spHeight) / 2;
                        
                        // Refill Button
                        Rectangle refillBtn = new Rectangle(spX + 50, spY + 60, 250, 40);
                        // Upgrade Button
                        Rectangle upgradeBtn = new Rectangle(spX + 50, spY + 140, 250, 40);
                        // Exit Button
                        Rectangle exitBtn = new Rectangle(spX + 300, spY + 10, 40, 40);

                        if (refillBtn.Contains(mousePos))
                        {
                            // Cost: 2 Rocks
                            if (_player.Inventory.RemoveItems("Rock", 2))
                            {
                                _player.RefillOxygen();
                                _showSpaceshipMenu = false;
                                _prevMouseState = currentMouseState;
                                return;
                            }
                        }
                        else if (_interactedStructure != null && _interactedStructure.RepairStage < 4 && upgradeBtn.Contains(mousePos) && !_showContributePopup)
                        {
                            // Open contribution popup
                            var upgradeCosts = GetSpaceshipUpgradeCosts(_interactedStructure.RepairStage);
                            if (upgradeCosts != null)
                            {
                                _showContributePopup = true;
                                _contributeAmounts.Clear();
                                _contributeMaterialKeys.Clear();
                                _contributeFocusedField = -1;
                                _contributeTypingBuffer = "";
                                foreach (var cost in upgradeCosts)
                                {
                                    _contributeMaterialKeys.Add(cost.Key);
                                    _contributeAmounts[cost.Key] = 0;
                                }
                                _prevMouseState = currentMouseState;
                                return;
                            }
                        }
                        else if (_showContributePopup)
                        {
                            // Handle contribute popup clicks
                            int popW = 380;
                            int popH = 60 + _contributeMaterialKeys.Count * 65 + 60;
                            int popX = (GraphicsDevice.Viewport.Width - popW) / 2;
                            int popY = (GraphicsDevice.Viewport.Height - popH) / 2;
                            
                            var upgradeCosts = GetSpaceshipUpgradeCosts(_interactedStructure.RepairStage);
                            bool clickedInside = false;
                            
                            for (int i = 0; i < _contributeMaterialKeys.Count; i++)
                            {
                                string mat = _contributeMaterialKeys[i];
                                int rowY = popY + 55 + i * 65 + 18;
                                
                                // Minus button
                                Rectangle minusBtn = new Rectangle(popX + 30, rowY, 35, 30);
                                // Plus button
                                Rectangle plusBtn = new Rectangle(popX + 310, rowY, 35, 30);
                                // Number field (clickable to focus)
                                Rectangle numField = new Rectangle(popX + 75, rowY, 225, 30);
                                
                                if (minusBtn.Contains(mousePos))
                                {
                                    int current = _contributeAmounts[mat];
                                    if (current > 0) _contributeAmounts[mat] = current - 1;
                                    // Update typing buffer if this field is focused
                                    if (_contributeFocusedField == i) _contributeTypingBuffer = _contributeAmounts[mat].ToString();
                                    clickedInside = true;
                                }
                                else if (plusBtn.Contains(mousePos))
                                {
                                    int current = _contributeAmounts[mat];
                                    int have = _player.Inventory.CountItem(mat);
                                    int stillNeeded = (upgradeCosts != null && upgradeCosts.ContainsKey(mat)) ? 
                                        upgradeCosts[mat] - _interactedStructure.GetContributed(mat) : 0;
                                    int maxAdd = Math.Min(have, stillNeeded);
                                    if (current < maxAdd) _contributeAmounts[mat] = current + 1;
                                    if (_contributeFocusedField == i) _contributeTypingBuffer = _contributeAmounts[mat].ToString();
                                    clickedInside = true;
                                }
                                else if (numField.Contains(mousePos))
                                {
                                    // Commit previous field
                                    if (_contributeFocusedField >= 0 && _contributeFocusedField < _contributeMaterialKeys.Count)
                                    {
                                        CommitContributeTyping();
                                    }
                                    _contributeFocusedField = i;
                                    _contributeTypingBuffer = _contributeAmounts[mat].ToString();
                                    clickedInside = true;
                                }
                            }
                            
                            // Confirm button
                            Rectangle confirmBtn = new Rectangle(popX + 30, popY + popH - 55, 140, 40);
                            // Cancel button
                            Rectangle cancelBtn = new Rectangle(popX + 210, popY + popH - 55, 140, 40);
                            
                            if (confirmBtn.Contains(mousePos))
                            {
                                // Commit any typing in progress
                                CommitContributeTyping();
                                
                                // Actually contribute the chosen amounts
                                bool contributed = false;
                                foreach (var mat in _contributeMaterialKeys)
                                {
                                    int amount = _contributeAmounts[mat];
                                    if (amount > 0)
                                    {
                                        _player.Inventory.RemoveItems(mat, amount);
                                        _interactedStructure.ContributeMaterial(mat, amount);
                                        contributed = true;
                                    }
                                }
                                
                                // Check if all costs are now fully met
                                if (contributed && upgradeCosts != null)
                                {
                                    bool allMet = true;
                                    foreach (var cost in upgradeCosts)
                                    {
                                        if (_interactedStructure.GetContributed(cost.Key) < cost.Value)
                                        {
                                            allMet = false;
                                            break;
                                        }
                                    }
                                    if (allMet)
                                    {
                                        _interactedStructure.ClearContributions();
                                        _interactedStructure.UpgradeRepairStage();
                                    }
                                }
                                
                                _showContributePopup = false;
                                clickedInside = true;
                            }
                            else if (cancelBtn.Contains(mousePos))
                            {
                                _showContributePopup = false;
                                clickedInside = true;
                            }
                            
                            if (!clickedInside)
                            {
                                // Clicked outside the popup — unfocus field
                                if (_contributeFocusedField >= 0)
                                {
                                    CommitContributeTyping();
                                    _contributeFocusedField = -1;
                                }
                            }
                            
                            _prevMouseState = currentMouseState;
                            return;
                        }
                        else if (exitBtn.Contains(mousePos))
                        {
                            _showContributePopup = false;
                            _showSpaceshipMenu = false;
                            _prevMouseState = currentMouseState;
                            return;
                        }
                        else if (!new Rectangle(spX, spY, spWidth, spHeight).Contains(mousePos) && !_showContributePopup)
                        {
                            _showSpaceshipMenu = false;
                            _prevMouseState = currentMouseState;
                            return;
                        }
                    }
                }
            }
            // Game World Updates (Run even if inventory is open)
            _player.Update(gameTime, _entityManager, _camera, _tileMap, !_showInventory);
            _camera.Position = _player.Position; // Follow player

            if (!_showInventory && _buildModeState != BuildModeState.Editing && !_showWorkbenchMenu && !_showGreenhouseMenu && !_showSpaceshipMenu && !_showLootMenu) // Wrap normal interaction logic
            {
                // Check for interactions
                if (currentMouseState.LeftButton == ButtonState.Pressed && _prevMouseState.LeftButton == ButtonState.Released)
                {
                    Vector2 mouseWorldPos = _camera.ScreenToWorld(new Vector2(currentMouseState.X, currentMouseState.Y));
                    
                    // Check for alien clicks first
                    bool hitAlien = false;
                    foreach (var alien in _aliens)
                    {
                        Rectangle alienBounds = new Rectangle((int)alien.Position.X, (int)alien.Position.Y, alien.Width, alien.Height);
                        if (alienBounds.Contains(mouseWorldPos))
                        {
                            alien.TakeDamage(10f); // 10% damage per click
                            // Add Laser
                            _lasers.Add(new Laser { Start = _player.Position + new Vector2(16, 16), End = mouseWorldPos, Duration = 0.2f, MaxDuration = 0.2f });
                            _laserSound?.Play(0.2f, 0f, 0f);
                            hitAlien = true;
                            break;
                        }
                    }
                    
                    // Only check structure interactions if we didn't hit an alien
                    if (!hitAlien)
                    {
                        foreach (var entity in _entityManager.GetEntities())
                    {
                        if (entity is Structure s && s.GetBounds().Contains(mouseWorldPos))
                        {
                            // Add Laser for object interaction too
                            _lasers.Add(new Laser { Start = _player.Position + new Vector2(16, 16), End = mouseWorldPos, Duration = 0.2f, MaxDuration = 0.2f });
                            _laserSound?.Play(0.2f, 0f, 0f);

                            if (s.GetBounds().Intersects(new Rectangle((int)_player.Position.X - 60, (int)_player.Position.Y - 60, 160, 160))) // Must be close
                            {
                                if (s.Type == "Workbench")
                                {
                                    _showWorkbenchMenu = true;
                                    _interactedStructure = s;
                                    _showInventory = false;
                                }
                                else if (s.Type == "Greenhouse")
                                {
                                    _showGreenhouseMenu = true;
                                    _interactedStructure = s;
                                    _showInventory = false;
                                }
                                else if (s.Type == "Spaceship")
                                {
                                    // Open Spaceship Menu
                                    _showSpaceshipMenu = true;
                                    _interactedStructure = s;
                                    _showInventory = false;
                                }
                                else if (s.Type == "WormHole")
                                {
                                    // Teleport Logic
                                    float darkSideX = 1600000; // Chunk ~2083
                                    if (_player.Position.X < 800000)
                                    {
                                        // Go to Dark Side
                                        _player.Position = new Vector2(darkSideX, _player.Position.Y);
                                    }
                                    else
                                    {
                                        // Return Home
                                        _player.Position = new Vector2(0, _player.Position.Y);
                                    }
                                    _showInventory = false;
                                }
                            }
                        }
                        else if (entity is Backpack bp && bp.GetBounds().Contains(mouseWorldPos))
                        {
                             if (bp.GetBounds().Intersects(new Rectangle((int)_player.Position.X - 60, (int)_player.Position.Y - 60, 160, 160)))
                             {
                                 _showLootMenu = true;
                                 _interactedBackpack = bp;
                                 _showInventory = false; // Close player inventory for consistency
                             }
                        }
                    }
                        }

                // Handle Save Button Click
                Rectangle saveBtnRect = new Rectangle(GraphicsDevice.Viewport.Width - 50, GraphicsDevice.Viewport.Height - 50, 40, 40);
                if (currentMouseState.LeftButton == ButtonState.Pressed && _prevMouseState.LeftButton == ButtonState.Released)
                {
                    if (saveBtnRect.Contains(currentMouseState.Position))
                    {
                        SaveGame();
                    }
                }
            }

                
                if (currentKeyboardState.IsKeyDown(Keys.E) && !_prevKeyboardState.IsKeyDown(Keys.E))
                {
                    foreach (var crop in _crops)
                    {
                        if (_player.Inventory.RemoveItems(crop.Id, 1))
                        {
                            _player.Eat(100f);
                            _player.AddBuff(crop.HungerBuffReward, crop.OxygenBuffReward, crop.BuffDuration);
                            break;
                        }
                    }
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

                // Alien Spawning Logic
                bool inCave = _player.Position.Y < -500_000;
                
                if (!inCave && !_surfaceAlienSpawned)
                {
                    _surfaceAlienTimer += dt;
                    if (_surfaceAlienTimer >= 30f) // 30s on surface
                    {
                        _surfaceAlienSpawned = true;
                        Vector2 spawnPos = _player.Position + new Vector2(400, 0); // 400px to the right
                        _aliens.Add(new Alien(spawnPos, "metro_alien"));
                    }
                }
                
                if (inCave && !_caveAlienSpawned)
                {
                    _caveAlienTimer += dt;
                    if (_caveAlienTimer >= 5f) // 5s underground
                    {
                        _caveAlienSpawned = true;
                        Vector2 spawnPos = _player.Position + new Vector2(-400, 0); 
                        _aliens.Add(new Alien(spawnPos, "the_destroyer"));
                    }
                }



            // Update Aliens
                for (int i = _aliens.Count - 1; i >= 0; i--)
                {
                    _aliens[i].Update(dt, _player);
                    if (_aliens[i].IsDead)
                    {
                        // Spawn Electricity Particles
                        Vector2 deathPos = _aliens[i].Position + new Vector2(_aliens[i].Width / 2f, _aliens[i].Height / 2f); // Center of alien
                        int particleCount = _random.Next(8, 13); // 8-12 particles
                        
                        for (int p = 0; p < particleCount; p++)
                        {
                            // Random direction
                            float angle = (float)(_random.NextDouble() * Math.PI * 2);
                            float length = (float)(_random.NextDouble() * 50 + 30); // 30-80 pixels
                            
                            Vector2 direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                            Vector2 endPos = deathPos + direction * length;
                            
                            _electricityParticles.Add(new ElectricityParticle
                            {
                                Start = deathPos,
                                End = endPos,
                                Duration = (float)(_random.NextDouble() * 0.2 + 0.3), // 0.3-0.5 seconds
                                MaxDuration = 0.5f,
                                RandomOffset = Vector2.Zero
                            });
                        }
                        
                        // Loot Drop
                        _entityManager.AddEntity(new Structure(_aliens[i].Position, "Rock", 32, 32));
                        _entityManager.AddEntity(new Structure(_aliens[i].Position + new Vector2(10, 10), "Crystal", 32, 32));
                        
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
            }

            // Update Electricity Particles
            for (int i = _electricityParticles.Count - 1; i >= 0; i--)
            {
                ElectricityParticle p = _electricityParticles[i];
                p.Duration -= dt;
                // Update random offset for flickering
                p.RandomOffset = new Vector2(
                    (float)(_random.NextDouble() * 4 - 2),
                    (float)(_random.NextDouble() * 4 - 2)
                );
                _electricityParticles[i] = p;
                if (p.Duration <= 0)
                {
                    _electricityParticles.RemoveAt(i);
                }
            }
            }



            // Toggle Build Menu
            if (currentKeyboardState.IsKeyDown(Keys.B) && !_prevKeyboardState.IsKeyDown(Keys.B))
            {
                if (_buildModeState == BuildModeState.None) 
                {
                    _buildModeState = BuildModeState.Menu;
                    _showInventory = false; // Close Inventory
                }
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
                        int menuHeight = 9 * 50 + 10 * 5; // 8 Buildings + 1 Edit
                        int menuX = 10;
                        int menuY = (GraphicsDevice.Viewport.Height - menuHeight) / 2;
                        
                        // Check Slots
                        for (int i = 0; i < 9; i++)
                        {
                            Rectangle slotRect = new Rectangle(menuX + 5, menuY + 5 + i * 55, 50, 50);
                            if (slotRect.Contains(currentMouseState.Position))
                            {
                                string structure = "";
                                if (i == 0) structure = "Greenhouse";
                                else if (i == 1) structure = "Workbench";
                                else if (i == 2) structure = "Reactor";
                                else if (i == 3) structure = "HAB";
                                else if (i == 4) structure = "Bionic Tech";
                                else if (i == 5) structure = "Machinery";
                                else if (i == 6) structure = "Radar";
                                else if (i == 7) structure = "WormHole"; // Case sensitive? User said WormHoles/WormHole, let's use "WormHole"
                                
                                if (i == 8) // Edit Button
                                {
                                    if (_buildModeState == BuildModeState.Menu) _buildModeState = BuildModeState.Editing;
                                    else _buildModeState = BuildModeState.Menu;
                                    _showEditContextMenu = false;
                                }
                                else if (structure != "")
                                {
                                    // Check costs locally for UI feedback (Strict check in Player.cs)
                                    // Or just let Player.StartPlacing handle it? 
                                    // Logic here: check resources -> StartPlacing.
                                    // I'll replicate simple checks.
                                    bool canBuild = false;
                                    int rock = _player.Inventory.CountItem("Rock");
                                    int crys = _player.Inventory.CountItem("Crystal");

                                    if (structure == "Greenhouse") canBuild = rock >= 2 && crys >= 1;
                                    else if (structure == "Workbench") canBuild = rock >= 3;
                                    else if (structure == "Reactor") canBuild = rock >= 50 && crys >= 30;
                                    else if (structure == "HAB") canBuild = rock >= 60 && crys >= 20;
                                    else if (structure == "Bionic Tech") canBuild = rock >= 40 && crys >= 50;
                                    else if (structure == "Machinery") canBuild = rock >= 80 && crys >= 40;
                                    else if (structure == "Radar") canBuild = rock >= 40 && crys >= 40;
                                    else if (structure == "WormHole") canBuild = rock >= 100 && crys >= 100;

                                    if (canBuild)
                                    {
                                        _selectedBuildStructure = structure;
                                        _buildModeState = BuildModeState.Placing;
                                        _player.StartPlacing(structure);
                                    }
                                }
                            }
                        }
                    }
                }

                // Handle Edit Mode Interactions
                if (_buildModeState == BuildModeState.Editing)
                {
                    Vector2 mouseWorldPos = _camera.ScreenToWorld(new Vector2(currentMouseState.X, currentMouseState.Y));

                    // 1. Calculate Hovered Structure
                    _hoveredEditStructure = null;
                    if (!_showEditContextMenu)
                    {
                        foreach (var entity in _entityManager.GetEntities())
                        {
                            if (entity is Structure s && s.Type != "Rock" && s.Type != "Crystal" && s.Type != "Spaceship")
                            {
                                Rectangle bounds = s.GetBounds();
                                
                                if (bounds.Contains(mouseWorldPos))
                                {
                                    _hoveredEditStructure = s;
                                    break;
                                }
                            }
                        }
                    }

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
                        // Left Click on Structure to Open Menu
                        if (currentMouseState.LeftButton == ButtonState.Pressed && _prevMouseState.LeftButton == ButtonState.Released && !_player.JustPlacedStructure)
                        {
                            if (_hoveredEditStructure != null)
                            {
                                _editTargetStructure = _hoveredEditStructure;
                                _editContextMenuPos = new Vector2(currentMouseState.X, currentMouseState.Y);
                                _showEditContextMenu = true;
                            }
                        }
                    }
                }
                else
                {
                    _hoveredEditStructure = null; // Clear if not in edit mode
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
            
            // Update Save Animation
            if (_saveAnimationTimer > 0)
            {
                _saveAnimationTimer -= dt;
                if (_saveAnimationTimer < 0) _saveAnimationTimer = 0;
            }
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

        private Texture2D GenerateVignetteTexture(int size)
        {
            Texture2D tex = new Texture2D(GraphicsDevice, size, size);
            Color[] data = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float maxDist = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(center, new Vector2(x, y));
                    // Start fading out towards the edges (alpha 0 at center, 1 at edge)
                    float alpha = MathHelper.Clamp((dist - maxDist * 0.2f) / (maxDist * 0.8f), 0f, 1f);
                    
                    // smoothstep
                    alpha = alpha * alpha * (3f - 2f * alpha);

                    // Pre-multiply alpha for MonoGame
                    byte a = (byte)(alpha * 255);
                    data[y * size + x] = new Color(a, a, a, a);
                }
            }
            tex.SetData(data);
            return tex;
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            if (_currentGameState == GameState.StartMenu)
            {
                _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "ANTIGRAVITY MOON", new Vector2(400, 400), Color.White, 8);
                
                // New Colony Button
                Rectangle newGameBtn = new Rectangle(500, 600, 300, 50);
                _spriteBatch.Draw(_pixelTexture, newGameBtn, Color.Green);
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "NEW COLONY", new Vector2(newGameBtn.X + 20, newGameBtn.Y + 10), Color.White, 3);
                
                // Load Colony Button
                if (_saveExists)
                {
                    Rectangle loadGameBtn = new Rectangle(500, 670, 300, 50);
                    _spriteBatch.Draw(_pixelTexture, loadGameBtn, Color.Blue);
                    PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "LOAD COLONY", new Vector2(loadGameBtn.X + 20, loadGameBtn.Y + 10), Color.White, 3);
                }

                _spriteBatch.End();
                return;
            }

            // Apply Camera Transform
            _spriteBatch.Begin(transformMatrix: _camera.GetViewMatrix(), samplerState: SamplerState.PointClamp);

    // Calculate camera view rectangle for culling (account for zoom)
    float invZoom = 1f / _camera.Zoom;
    int viewHalfW = (int)(GraphicsDevice.Viewport.Width  * 0.5f * invZoom) + TileMap.TileSize;
    int viewHalfH = (int)(GraphicsDevice.Viewport.Height * 0.5f * invZoom) + TileMap.TileSize;
    Rectangle cameraRect = new Rectangle(
        (int)(_camera.Position.X - viewHalfW),
        (int)(_camera.Position.Y - viewHalfH),
        viewHalfW * 2,
        viewHalfH * 2
    );
    _tileMap.Draw(_spriteBatch, _textures, _pixelTexture, cameraRect, gameTime.TotalGameTime.TotalSeconds);
            
            if (_currentGameState == GameState.Intro)
            {
                 // Draw the spaceship dropping (preserving original aspect ratio)
                 int targetWidth = 384; 
                 int targetHeight = (int)(targetWidth * ((float)_spaceshipTexture.Height / _spaceshipTexture.Width));
                 
                 _spriteBatch.Draw(_spaceshipTexture, new Rectangle((int)_spaceshipPosition.X, (int)_spaceshipPosition.Y, targetWidth, targetHeight), Color.White);
            }
            else if (_currentGameState == GameState.Playing)
            {
                // Calculate Mouse World Pos for Hover Logic
                Vector2 mouseWorldPos = _camera.ScreenToWorld(new Vector2(Mouse.GetState().X, Mouse.GetState().Y));
                
                // Draw Entities
                foreach (var entity in _entityManager.GetEntities())
                {
                    // Draw Hover Glow in Edit Mode
                    bool isBeingEdited = (_showEditContextMenu && entity == _editTargetStructure);
                    bool isHovered = (entity == _hoveredEditStructure);

                    if (_buildModeState == BuildModeState.Editing && (isHovered || isBeingEdited))
                    {
                        Structure s = entity as Structure;
                        if (s != null)
                        {
                            int w = s.Width > 0 ? s.Width : 32;
                            int h = s.Height > 0 ? s.Height : 32;
                            
                            // Pulse effect for the outline
                            float pulse = 1.0f + (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * 6) * 0.2f;
                            Color glowColor = Color.DeepSkyBlue * pulse;

                            _spriteBatch.Draw(_pixelTexture, new Rectangle((int)s.Position.X - 4, (int)s.Position.Y - 4, w + 8, h + 8), glowColor);
                        }
                    }
                }
                _entityManager.Draw(_spriteBatch, _textures, mouseWorldPos, _tileMap);
                
                // Draw Greenhouse Exterior Progress
                foreach (var entity in _entityManager.GetEntities())
                {
                    if (entity is Structure s && s.Type == "Greenhouse")
                    {
                        int barWidth = 40;
                        int barHeight = 8;
                        int cw = s.Width > 0 ? s.Width : 32;
                        int barX = (int)s.Position.X + (cw / 2) - (barWidth / 2);
                        int barY = (int)s.Position.Y - 15;

                        if (s.PlantedCount > 0 && s.MaxPlantedCount > 0)
                        {
                            // Calculate total progress
                            float totalDuration = s.MaxPlantedCount * s.MaxGrowthTimer;
                            float elapsedDuration = ((s.MaxPlantedCount - s.PlantedCount) * s.MaxGrowthTimer) + s.GrowthTimer;
                            float progress = elapsedDuration / totalDuration;
                            int fillWidth = (int)(barWidth * progress);

                            // Draw Background
                            _spriteBatch.Draw(_pixelTexture, new Rectangle(barX, barY, barWidth, barHeight), Color.DarkGray);
                            // Draw Fill
                            _spriteBatch.Draw(_pixelTexture, new Rectangle(barX, barY, fillWidth, barHeight), Color.Cyan);
                            
                            // Adjust barY for the Ready Text if drawn below
                            barY -= 15; 
                        }

                        if (s.ReadyCount > 0)
                        {
                            PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, $"READY: {s.ReadyCount}", new Vector2(barX - 10, barY), Color.LimeGreen, 1);
                        }
                    }
                }
                
                // Draw Backpacks explicitly if needed, or let EntityManager handle it if we pass textures correctly?
                // EntityManager.Draw iterates and calls entity.Draw(sb, texture, mouse).
                // But EntityManager.Draw takes a Dictionary and tries to find texture by name?
                // Let's check EntityManager.Draw.
                
                // If EntityManager.Draw does:
                // foreach(e) { 
                //    tex = textures.ContainsKey(e.Type.ToLower()) ? textures[e.Type.ToLower()] : pixel;
                //    e.Draw(sb, tex, mouse);
                // }
                // Then we just need to make sure "backpack" is in textures.
                // We added "backpack" to textures in LoadContent.
                // Backpack entity Type is "Backpack". ToLower is "backpack".
                // So EntityManager should handle it correctly now that signatures match.
                
                // Draw Aliens
                foreach (var alien in _aliens)
                {
                    string textureKey = alien.Type.ToLower();
                    Texture2D tex = _textures.ContainsKey(textureKey) ? _textures[textureKey] : _pixelTexture;
                    alien.Draw(_spriteBatch, tex, mouseWorldPos);
                }

                // Draw Lasers
                foreach (var laser in _lasers)
                {
                    float alpha = laser.Duration / laser.MaxDuration;
                    DrawLine(_spriteBatch, _pixelTexture, laser.Start, laser.End, Color.Cyan * alpha, 2);
                }

                // Draw Electricity Particles
                foreach (var particle in _electricityParticles)
                {
                    float alpha = particle.Duration / particle.MaxDuration;
                    // Apply random offset for flickering effect
                    Vector2 offsetEnd = particle.End + particle.RandomOffset;
                    DrawLine(_spriteBatch, _pixelTexture, particle.Start, offsetEnd, Color.Yellow * alpha, 3);
                }

                _player.Draw(_spriteBatch, _textures.ContainsKey("astronaut") ? _textures["astronaut"] : _pixelTexture, _textures);

                // --- Lava Tube Cave world-space shadow ---
                if (_inTunnel)
                {
                    _spriteBatch.Draw(_pixelTexture,
                        new Rectangle((int)(_camera.Position.X - viewHalfW),
                                      (int)(_camera.Position.Y - viewHalfH),
                                      viewHalfW * 2, viewHalfH * 2),
                        new Color(0, 0, 0) * 0.4f); // Simple darkness overlay
                }
            }

            _spriteBatch.End();

            if (_currentGameState == GameState.Playing)
            {
                // Draw UI (Inventory) separately without camera transform (Screen Space)
                _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            
            if (_showInventory)
            {
                _player.Inventory.Draw(_spriteBatch, _pixelTexture, _textures, GetInventoryPosition());
                
                Vector2 invPos = GetInventoryPosition();
                
                // Draw Info
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, $"BACKPACK LVL: {_player.Inventory.UpgradeLevel}", new Vector2(invPos.X, invPos.Y - 65), Color.Cyan, 2);
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, $"SUIT LVL: {_player.SuitLevel}", new Vector2(invPos.X + 240, invPos.Y - 65), Color.Orange, 2);
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, $"POS: [ {(int)_player.Position.X}, {(int)_player.Position.Y} ]", new Vector2(invPos.X, invPos.Y - 40), Color.Yellow, 2);

                // Draw Sort Button
                int invWidth = _player.Inventory.Cols * 45 + 5;
                Rectangle sortBtn = new Rectangle((int)invPos.X + invWidth - 60, (int)invPos.Y - 25, 60, 20);
                
                _spriteBatch.Draw(_pixelTexture, sortBtn, Color.Gray);
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "SORT", new Vector2(sortBtn.X + 5, sortBtn.Y - 3), Color.White, 2);
            }

            // Draw Health Bar
            PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "HEALTH", new Vector2(20, 20), Color.White, 2);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(20, 50, 400, 40), Color.Gray);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(20, 50, (int)(_player.Health * 4), 40), Color.Green);

            // Draw Hunger Bar
            int hBuff = _player.GetTotalHungerBuff();
            string hText = hBuff > 0 ? $"HUNGER (+{hBuff}% SLOWER)" : "HUNGER";
            Color hColor = hBuff > 0 ? Color.LimeGreen : Color.White;
            PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, hText, new Vector2(20, 100), hColor, 2);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(20, 130, 400, 40), Color.Gray);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(20, 130, (int)(_player.Hunger * 4), 40), Color.Orange);

                // Draw Vignettes
                if (_vignetteTexture != null)
                {
                    // Draw red health vignette overlay based on missing health
                    float healthPercent = _player.Health / 100f; // max health 100
                    if (healthPercent < 1.0f)
                    {
                        float healthAlpha = (1.0f - healthPercent) * 0.9f; // intensify as it gets lower
                        if (healthPercent < 0.2f)
                        {
                            // pulse strongly when very low
                            float pulse = 0.8f + (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * 5) * 0.2f;
                            healthAlpha *= pulse;
                        }
                        _spriteBatch.Draw(_vignetteTexture, GraphicsDevice.Viewport.Bounds, Color.Red * healthAlpha);
                    }

                    // Oxygen and Hunger danger pulse
                    float danger = 0f;
                    if (_player.Oxygen < 20) danger = Math.Max(danger, (20 - _player.Oxygen) / 20f);
                    if (_player.Hunger < 20) danger = Math.Max(danger, (20 - _player.Hunger) / 20f);

                    if (danger > 0)
                    {
                        // Pulse
                        float pulse = 0.8f + (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * 5) * 0.2f;
                        float alpha = danger * pulse;
                        _spriteBatch.Draw(_vignetteTexture, GraphicsDevice.Viewport.Bounds, Color.White * alpha);
                    }
                }
            // Draw Oxygen Bar
            int oBuff = _player.GetTotalOxygenBuff();
            string oText = oBuff > 0 ? $"OXYJIN (+{oBuff}% SLOWER)" : "OXYJIN";
            Color oColor = oBuff > 0 ? Color.LimeGreen : Color.White;
            PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, oText, new Vector2(20, 180), oColor, 2);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(20, 210, 400, 40), Color.Gray);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(20, 210, (int)(_player.Oxygen * 4), 40), Color.CornflowerBlue);

            // Draw Control Labels (Centered)
            PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "PRESS B TO BUILD", new Vector2(GraphicsDevice.Viewport.Width / 2 - 140, 20), Color.White, 2);
            PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "PRESS I FOR INVENTORY", new Vector2(GraphicsDevice.Viewport.Width / 2 - 140, 60), Color.White, 2);
            PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "SCROLL / Z X TO ZOOM", new Vector2(GraphicsDevice.Viewport.Width / 2 - 140, 100), Color.Gray, 2);

            // Transition Prompts (Screen Space Center)
            int drawTileX = (int)Math.Floor(_player.Position.X / TileMap.TileSize);
            int drawTileY = (int)Math.Floor(_player.Position.Y / TileMap.TileSize);
            
            bool nearPromptEntrance = false;
            bool nearPromptExit = false;
            
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dy = -2; dy <= 2; dy++)
                {
                    int tile = _tileMap.GetTile(drawTileX + dx, drawTileY + dy);
                    if (tile == 3) nearPromptEntrance = true;
                    if (tile == 6) nearPromptExit = true;
                }
            }

            if (nearPromptEntrance)
            {
                float promptPulse = 0.8f + (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * 5) * 0.2f;
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "PRESS E TO ENTER CAVE", 
                    new Vector2(GraphicsDevice.Viewport.Width / 2 - 150, GraphicsDevice.Viewport.Height / 2 - 80), 
                    Color.Yellow * promptPulse, 2);
            }
            else if (nearPromptExit)
            {
                float promptPulse = 0.8f + (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * 5) * 0.2f;
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "PRESS E TO CLIMB TO SURFACE", 
                    new Vector2(GraphicsDevice.Viewport.Width / 2 - 190, GraphicsDevice.Viewport.Height / 2 - 80), 
                    Color.Yellow * promptPulse, 2);
            }

            // --- Lava Tube Cave Screen-Space Overlay ---
            if (_inTunnel)
            {
                // Dark vignette
                if (_vignetteTexture != null)
                {
                    _spriteBatch.Draw(_vignetteTexture, GraphicsDevice.Viewport.Bounds, new Color(0, 0, 0) * 0.7f);
                }
                // Label
                string tunnelLabel = "LAVA TUBE CAVE  -  RARE MATERIALS AHEAD!";
                float labelPulse = 0.8f + (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * 3) * 0.2f;
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, tunnelLabel,
                    new Vector2(GraphicsDevice.Viewport.Width / 2 - 280, GraphicsDevice.Viewport.Height - 80),
                    new Color((int)(180 * labelPulse), (int)(180 * labelPulse), (int)(180 * labelPulse)), 2);
            }

            // Draw Menus
            if (_showWorkbenchMenu)
            {
                // Background (Centered)
                int wbWidth = 600;
                int wbHeight = 400;
                Rectangle menuRect = new Rectangle((GraphicsDevice.Viewport.Width - wbWidth) / 2, (GraphicsDevice.Viewport.Height - wbHeight) / 2, wbWidth, wbHeight);
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
                int menuWidth = Math.Min(GraphicsDevice.Viewport.Width - 40, 600);
                int menuHeight = Math.Min(GraphicsDevice.Viewport.Height - 40, 200 + (_crops.Count * 80) + 120);
                Rectangle menuRect = new Rectangle((GraphicsDevice.Viewport.Width - menuWidth) / 2, (GraphicsDevice.Viewport.Height - menuHeight) / 2, menuWidth, menuHeight);
                _spriteBatch.Draw(_pixelTexture, menuRect, Color.DarkGreen);
                
                // Close Button
                _spriteBatch.Draw(_pixelTexture, new Rectangle(menuRect.Right - 40, menuRect.Top + 10, 30, 30), Color.Red);
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "X", new Vector2(menuRect.Right - 32, menuRect.Top + 18), Color.White, 2);

                // Title
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "GREENHOUSE", new Vector2(menuRect.X + 20, menuRect.Top + 20), Color.White, 4);

                // Dynamic Crop Buttons
                int cropIndex = 0;
                int startYOffset = 100;
                int buttonSpacing = 80;

                foreach (var crop in _crops)
                {
                    bool canAfford = true;
                    foreach (var cost in crop.Costs)
                    {
                        if (_player.Inventory.CountItem(cost.Key) < cost.Value)
                        {
                            canAfford = false;
                            break;
                        }
                    }

                    Color plantColor = canAfford ? crop.GetFallbackColor() : Color.Gray;
                    Rectangle plantBtnRect = new Rectangle(menuRect.X + 50, menuRect.Top + startYOffset + (cropIndex * buttonSpacing), 300, 60);
                    
                    _spriteBatch.Draw(_pixelTexture, plantBtnRect, plantColor);
                    
                    // Determine text color for contrast 
                    Color textColor = Color.Black; 
                    if (!canAfford) textColor = Color.DarkGray; // Dim if cannot afford
                    
                    PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, $"PLANT {crop.Name.ToUpper()}", new Vector2(plantBtnRect.X + 10, plantBtnRect.Y + 20), textColor, 2);
                    
                    // Draw Costs
                    int offsetX = 0;
                    foreach (var cost in crop.Costs)
                    {
                        string costTextureKey = cost.Key.ToLower();
                        if (_textures.ContainsKey(costTextureKey))
                        {
                            _spriteBatch.Draw(_textures[costTextureKey], new Rectangle(plantBtnRect.Right + 10 + offsetX, plantBtnRect.Y, 40, 40), Color.White);
                            Color costColor = _player.Inventory.CountItem(cost.Key) >= cost.Value ? Color.White : Color.Red;
                            PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, $"x{cost.Value}", new Vector2(plantBtnRect.Right + 60 + offsetX, plantBtnRect.Y + 10), costColor, 2);
                            offsetX += 100; // Space for next item icon + text
                        }
                    }

                    cropIndex++;
                }

                // Harvest Button
                int harvestYOffset = startYOffset + (_crops.Count * buttonSpacing) + 20;
                Color harvestColor = (_interactedStructure != null && _interactedStructure.IsReadyToHarvest) ? Color.Green : Color.Gray;
                Rectangle harvestBtnRect = new Rectangle(menuRect.X + 50, menuRect.Top + harvestYOffset, 300, 60);
                _spriteBatch.Draw(_pixelTexture, harvestBtnRect, harvestColor);
                
                string harvestText = _interactedStructure != null && _interactedStructure.ReadyCount > 0 ? $"HARVEST ({_interactedStructure.ReadyCount})" : "HARVEST";
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, harvestText, new Vector2(harvestBtnRect.X + 10, harvestBtnRect.Y + 20), Color.Black, 2);

                // Growth Timer and Planted Count
                if (_interactedStructure != null)
                {
                    int textYOffset = harvestYOffset + 80;
                    if (_interactedStructure.IsGrowing)
                    {
                        string timerText = "GROWING: " + ((int)(_interactedStructure.MaxGrowthTimer - _interactedStructure.GrowthTimer)).ToString() + "S";
                        PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, timerText, new Vector2(menuRect.X + 50, menuRect.Top + textYOffset), Color.White, 2);
                    }
                    if (_interactedStructure.PlantedCount > 0)
                    {
                         PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, $"PLANTED: {_interactedStructure.PlantedCount}", new Vector2(menuRect.X + 50, menuRect.Top + textYOffset + 40), Color.Cyan, 2);
                    }
                }
            }

            // Draw Build Menu
            if (_buildModeState == BuildModeState.Menu || _buildModeState == BuildModeState.Editing)
            {
                // Draw Menu Background (Left Side Vertical)
                int menuWidth = 60;
                int menuHeight = 9 * 50 + 10 * 5; // 9 slots
                int menuX = 10;
                int menuY = (GraphicsDevice.Viewport.Height - menuHeight) / 2;
                
                _spriteBatch.Draw(_pixelTexture, new Rectangle(menuX, menuY, menuWidth, menuHeight), Color.DarkGray);

                // Draw Slots
                for (int i = 0; i < 9; i++)
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
                    else if (i == 2) // Reactor
                    {
                        // Cost: 50 Rock, 30 Crystal
                        canBuild = _player.Inventory.CountItem("Rock") >= 50 && _player.Inventory.CountItem("Crystal") >= 30;
                        costText = "50 R, 30 C";
                        if (_textures.ContainsKey("reactor")) _spriteBatch.Draw(_textures["reactor"], slotRect, canBuild ? Color.White : Color.Gray * 0.5f);
                    }
                    else if (i == 3) // HAB
                    {
                        // Cost: 60 Rock, 20 Crystal
                        canBuild = _player.Inventory.CountItem("Rock") >= 60 && _player.Inventory.CountItem("Crystal") >= 20;
                        costText = "60 R, 20 C";
                        if (_textures.ContainsKey("hab")) _spriteBatch.Draw(_textures["hab"], slotRect, canBuild ? Color.White : Color.Gray * 0.5f);
                    }
                    else if (i == 4) // Bionic Tech
                    {
                        // Cost: 40 Rock, 50 Crystal
                        canBuild = _player.Inventory.CountItem("Rock") >= 40 && _player.Inventory.CountItem("Crystal") >= 50;
                        costText = "40 R, 50 C";
                        if (_textures.ContainsKey("bionic_tech")) _spriteBatch.Draw(_textures["bionic_tech"], slotRect, canBuild ? Color.White : Color.Gray * 0.5f);
                    }
                    else if (i == 5) // Machinery
                    {
                        // Cost: 80 Rock, 40 Crystal
                        canBuild = _player.Inventory.CountItem("Rock") >= 80 && _player.Inventory.CountItem("Crystal") >= 40;
                        costText = "80 R, 40 C";
                        if (_textures.ContainsKey("machinery")) _spriteBatch.Draw(_textures["machinery"], slotRect, canBuild ? Color.White : Color.Gray * 0.5f);
                    }
                    else if (i == 6) // Radar
                    {
                        // Cost: 40 Rock, 40 Crystal
                        canBuild = _player.Inventory.CountItem("Rock") >= 40 && _player.Inventory.CountItem("Crystal") >= 40;
                        costText = "40 R, 40 C";
                        if (_textures.ContainsKey("radar")) _spriteBatch.Draw(_textures["radar"], slotRect, canBuild ? Color.White : Color.Gray * 0.5f);
                    }
                    else if (i == 7) // WormHole
                    {
                        // Cost: 100 Rock, 100 Crystal
                        canBuild = _player.Inventory.CountItem("Rock") >= 100 && _player.Inventory.CountItem("Crystal") >= 100;
                        costText = "100 R, 100 C";
                        if (_textures.ContainsKey("wormhole")) _spriteBatch.Draw(_textures["wormhole"], slotRect, canBuild ? Color.White : Color.Gray * 0.5f);
                    }
                    else if (i == 8) // Edit Button
                    {
                        _spriteBatch.Draw(_pixelTexture, slotRect, _buildModeState == BuildModeState.Editing ? Color.Green : Color.Blue);
                        string btnText = _buildModeState == BuildModeState.Editing ? "OK" : "EDIT"; // Short text for vertical
                        PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, btnText, new Vector2(slotRect.X + 5, slotRect.Y + 20), Color.White, 1.5f);
                        continue; // Skip cost logic for button
                    }

                    // Draw Cost Text (Hover or Always? Let's do always for now to be clear)
                    // Draw to the right of the slot
                    Color textColor = canBuild ? Color.White : Color.Red;
                    PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, costText, new Vector2(slotRect.Right + 10, slotRect.Y + 15), textColor, 1.5f);
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
            // Draw Spaceship Menu
            if (_showSpaceshipMenu)
            {
                int menuWidth = 350;
                int menuHeight = 350;
                int menuX = (GraphicsDevice.Viewport.Width - menuWidth) / 2;
                int menuY = (GraphicsDevice.Viewport.Height - menuHeight) / 2;
                
                // Background
                _spriteBatch.Draw(_pixelTexture, new Rectangle(menuX, menuY, menuWidth, menuHeight), Color.DarkGray);
                _spriteBatch.Draw(_pixelTexture, new Rectangle(menuX + 5, menuY + 5, menuWidth - 10, menuHeight - 10), Color.Black);
                
                // Title
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "SPACESHIP", new Vector2(menuX + 110, menuY + 20), Color.White, 2);
                
                // Exit Button
                Rectangle exitBtn = new Rectangle(menuX + 300, menuY + 10, 40, 40);
                _spriteBatch.Draw(_pixelTexture, exitBtn, Color.Red);
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "X", new Vector2(exitBtn.X + 12, exitBtn.Y + 10), Color.White, 2);

                // Refill Button
                bool canAffordRefill = _player.Inventory.CountItem("Rock") >= 2;
                Color refillBtnColor = canAffordRefill ? Color.Green : Color.Gray;
                Rectangle refillBtn = new Rectangle(menuX + 50, menuY + 60, 250, 40);
                
                _spriteBatch.Draw(_pixelTexture, refillBtn, refillBtnColor);
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "REFILL OXYJIN", new Vector2(refillBtn.X + 40, refillBtn.Y + 10), Color.Black, 2);
                Color refillCostColor = canAffordRefill ? Color.White : Color.Red;
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "COST: 2 ROCKS", new Vector2(menuX + 100, menuY + 105), refillCostColor, 2);
                
                // Upgrade Section
                if (_interactedStructure != null)
                {
                    int stage = _interactedStructure.RepairStage;
                    
                    if (stage >= 4)
                    {
                        // Fully repaired!
                        PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "SHIP FULLY RESTORED!", new Vector2(menuX + 50, menuY + 150), Color.Cyan, 2);
                        PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "READY FOR LAUNCH", new Vector2(menuX + 70, menuY + 180), Color.LimeGreen, 2);
                    }
                    else
                    {
                        // Get costs for current stage
                        var costs = GetSpaceshipUpgradeCosts(stage);
                        string[] stageNames = { "INITIAL REPAIR", "HULL REBUILD", "ENGINE RESTORE", "FULL RESTORATION" };
                        string stageName = stage < stageNames.Length ? stageNames[stage] : "UPGRADE";
                        
                        // Check if player has anything to contribute
                        bool hasAnythingToGive = false;
                        if (costs != null)
                        {
                            foreach (var cost in costs)
                            {
                                int contributed = _interactedStructure.GetContributed(cost.Key);
                                int have = _player.Inventory.CountItem(cost.Key);
                                if (have > 0 && contributed < cost.Value) { hasAnythingToGive = true; break; }
                            }
                        }
                        
                        Rectangle upgradeBtn = new Rectangle(menuX + 50, menuY + 140, 250, 40);
                        _spriteBatch.Draw(_pixelTexture, upgradeBtn, hasAnythingToGive ? Color.Green : Color.Gray);
                        PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "CONTRIBUTE", new Vector2(upgradeBtn.X + 50, upgradeBtn.Y + 10), Color.Black, 2);
                        
                        // Stage name
                        PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, stageName, new Vector2(menuX + 80, menuY + 130), Color.White, 1);
                        
                        // Draw costs with contributed/needed and progress bars
                        int costY = menuY + 195;
                        if (costs != null)
                        {
                            foreach (var cost in costs)
                            {
                                int contributed = _interactedStructure.GetContributed(cost.Key);
                                bool fulfilled = contributed >= cost.Value;
                                Color costColor = fulfilled ? Color.LimeGreen : Color.Orange;
                                string costStr = $"{cost.Key.ToUpper()}: {contributed}/{cost.Value}";
                                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, costStr, new Vector2(menuX + 60, costY -10), costColor, 2);
                                
                                // Progress bar
                                int barWidth = 220;
                                int barHeight = 6;
                                float progress = Math.Min(1f, (float)contributed / cost.Value);
                                _spriteBatch.Draw(_pixelTexture, new Rectangle(menuX + 60, costY + 20, barWidth, barHeight), Color.DarkGray);
                                _spriteBatch.Draw(_pixelTexture, new Rectangle(menuX + 60, costY + 20, (int)(barWidth * progress), barHeight), fulfilled ? Color.LimeGreen : Color.Orange);
                                
                                costY += 35;
                            }
                        }
                        
                        // Stage indicator
                        string progressStr = $"STAGE {stage}/4";
                        PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, progressStr, new Vector2(menuX + 120, menuY + menuHeight - 40), Color.Yellow, 2);
                    }
                }
            }
            // Draw Contribution Popup (on top of spaceship menu)
            if (_showContributePopup && _interactedStructure != null)
            {
                int popW = 380;
                int popH = 60 + _contributeMaterialKeys.Count * 65 + 60;
                int popX = (GraphicsDevice.Viewport.Width - popW) / 2;
                int popY = (GraphicsDevice.Viewport.Height - popH) / 2;
                
                // Background
                _spriteBatch.Draw(_pixelTexture, new Rectangle(popX - 2, popY - 2, popW + 4, popH + 4), Color.White);
                _spriteBatch.Draw(_pixelTexture, new Rectangle(popX, popY, popW, popH), new Color(20, 20, 30));
                
                // Title
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "CONTRIBUTE MATERIALS", new Vector2(popX + 40, popY + 15), Color.Cyan, 2);
                
                var upgradeCosts = GetSpaceshipUpgradeCosts(_interactedStructure.RepairStage);
                
                for (int i = 0; i < _contributeMaterialKeys.Count; i++)
                {
                    string mat = _contributeMaterialKeys[i];
                    int labelY = popY + 55 + i * 65;
                    int controlY = labelY + 18;
                    
                    int have = _player.Inventory.CountItem(mat);
                    int alreadyContributed = _interactedStructure.GetContributed(mat);
                    int needed = (upgradeCosts != null && upgradeCosts.ContainsKey(mat)) ? upgradeCosts[mat] : 0;
                    int stillNeeded = needed - alreadyContributed;
                    
                    // Material name + info on same line
                    PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, mat.ToUpper(), new Vector2(popX + 30, labelY - 10), Color.White, 2);
                    string infoStr = $"HAVE {have}  NEED {Math.Max(stillNeeded, 0)}";
                    PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, infoStr, new Vector2(popX + 180, labelY + 3), Color.Gray, 1);
                    
                    // Minus button
                    Rectangle minusBtn = new Rectangle(popX + 30, controlY, 35, 30);
                    _spriteBatch.Draw(_pixelTexture, minusBtn, Color.DarkRed);
                    PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "-", new Vector2(minusBtn.X + 12, minusBtn.Y + 6), Color.White, 2);
                    
                    // Number input field
                    Rectangle numField = new Rectangle(popX + 75, controlY, 225, 30);
                    bool isFocused = (_contributeFocusedField == i);
                    _spriteBatch.Draw(_pixelTexture, numField, isFocused ? new Color(60, 60, 80) : new Color(40, 40, 50));
                    // Border for focused field
                    if (isFocused)
                    {
                        _spriteBatch.Draw(_pixelTexture, new Rectangle(numField.X - 1, numField.Y - 1, numField.Width + 2, 1), Color.Cyan);
                        _spriteBatch.Draw(_pixelTexture, new Rectangle(numField.X - 1, numField.Y + numField.Height, numField.Width + 2, 1), Color.Cyan);
                        _spriteBatch.Draw(_pixelTexture, new Rectangle(numField.X - 1, numField.Y, 1, numField.Height), Color.Cyan);
                        _spriteBatch.Draw(_pixelTexture, new Rectangle(numField.X + numField.Width, numField.Y, 1, numField.Height), Color.Cyan);
                    }
                    
                    string displayNum = isFocused ? _contributeTypingBuffer : _contributeAmounts[mat].ToString();
                    PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, displayNum, new Vector2(numField.X + 10, numField.Y + 6), Color.White, 2);
                    
                    // Plus button
                    Rectangle plusBtn = new Rectangle(popX + 310, controlY, 35, 30);
                    _spriteBatch.Draw(_pixelTexture, plusBtn, Color.DarkGreen);
                    PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "+", new Vector2(plusBtn.X + 10, plusBtn.Y + 6), Color.White, 2);
                }
                
                // Confirm button
                Rectangle confirmBtn = new Rectangle(popX + 30, popY + popH - 55, 140, 40);
                _spriteBatch.Draw(_pixelTexture, confirmBtn, Color.Green);
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "CONFIRM", new Vector2(confirmBtn.X + 20, confirmBtn.Y + 10), Color.Black, 2);
                
                // Cancel button
                Rectangle cancelBtn = new Rectangle(popX + 210, popY + popH - 55, 140, 40);
                _spriteBatch.Draw(_pixelTexture, cancelBtn, Color.DarkRed);
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "CANCEL", new Vector2(cancelBtn.X + 25, cancelBtn.Y + 10), Color.White, 2);
            }

            // Draw Loot Menu
            if (_showLootMenu && _interactedBackpack != null)
            {
                // Draw Backpack Inventory
                _interactedBackpack.Storage.Draw(_spriteBatch, _pixelTexture, _textures, new Vector2(GraphicsDevice.Viewport.Width / 2 - 200, GraphicsDevice.Viewport.Height / 2 - 150));
                
                // Draw Title
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "LOOT BACKPACK", new Vector2(GraphicsDevice.Viewport.Width / 2 - 100, GraphicsDevice.Viewport.Height / 2 - 200), Color.White, 2);
                
                // Draw "COLLECT ALL" Button
                Rectangle collectAllBtn = new Rectangle(GraphicsDevice.Viewport.Width / 2 + 220, GraphicsDevice.Viewport.Height / 2 - 150, 150, 40);
                
                // Check if player has space for all items by simulating the transfer
                bool canCollectAll = true;
                Inventory tempInv = _player.Inventory.Clone();
                
                for (int y = 0; y < _interactedBackpack.Storage.Rows && canCollectAll; y++)
                {
                    for (int x = 0; x < _interactedBackpack.Storage.Cols && canCollectAll; x++)
                    {
                        string item = _interactedBackpack.Storage.GetItem(x, y);
                        if (item != null)
                        {
                            int count = _interactedBackpack.Storage.GetItemCount(x, y);
                            for (int i = 0; i < count; i++)
                            {
                                if (!tempInv.AddItem(item))
                                {
                                    canCollectAll = false;
                                    break;
                                }
                            }
                        }
                    }
                }
                Color btnColor = canCollectAll ? Color.Green : Color.Gray;
                Color textColor = canCollectAll ? Color.White : Color.DarkGray;
                
                _spriteBatch.Draw(_pixelTexture, collectAllBtn, btnColor);
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "COLLECT ALL", new Vector2(collectAllBtn.X + 10, collectAllBtn.Y + 10), textColor, 2);
            }

            // Draw Backpack Context Menu
            if (_showBackpackContextMenu)
            {
                int menuHeight = _backpackContextMenuItemCount > 1 ? 50 : 25;
                _spriteBatch.Draw(_pixelTexture, new Rectangle((int)_backpackContextMenuPos.X, (int)_backpackContextMenuPos.Y, 100, menuHeight), Color.White);
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "LOOT", new Vector2(_backpackContextMenuPos.X + 5, _backpackContextMenuPos.Y + 5), Color.Black, 2);
                
                if (_backpackContextMenuItemCount > 1)
                {
                    PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "LOOT ALL", new Vector2(_backpackContextMenuPos.X + 5, _backpackContextMenuPos.Y + 30), Color.Black, 2);
                }
            }

            // Draw Inventory Context Menu
            if (_showInventoryContextMenu)
            {
                bool isFood = _crops.Exists(c => c.Id == _contextMenuItem);
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
                _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height), Color.Black * 0.8f);
                
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
                        causeMessage = "YOU WERE KILLED BY AN ALIEN - THANKS SCHMOLLIE...";
                        break;
                }
                
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, deathMessage, new Vector2(GraphicsDevice.Viewport.Width / 2 - 200, GraphicsDevice.Viewport.Height / 2 - 150), Color.Red, 6);
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, causeMessage, new Vector2(GraphicsDevice.Viewport.Width / 2 - 300, GraphicsDevice.Viewport.Height / 2 - 50), Color.White, 3);
                
                // Respawn button
                Rectangle respawnBtn = new Rectangle(GraphicsDevice.Viewport.Width / 2 - 150, GraphicsDevice.Viewport.Height / 2 + 50, 300, 60);
                _spriteBatch.Draw(_pixelTexture, respawnBtn, Color.Green);
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, "RESPAWN", new Vector2(respawnBtn.X + 50, respawnBtn.Y + 20), Color.Black, 3);
            }
            
            // Inventory Full Warning
            if (_inventoryFullTimer > 0)
            {
                _inventoryFullTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                string msg = "INVENTORY FULL";
                Vector2 msgPos = new Vector2(GraphicsDevice.Viewport.Width / 2 - 100, GraphicsDevice.Viewport.Height / 2);
                
                // Pulse effect
                float scale = 3f + (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * 10) * 0.2f;
                // Shadow
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, msg, msgPos + new Vector2(2,2), Color.Black, (int)scale);
                // Text
                PixelTextRenderer.DrawText(_spriteBatch, _pixelTexture, msg, msgPos, Color.Red, (int)scale);
            }

            _spriteBatch.End();
            }

            // Draw Minimap (UI Layer)
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            if (_showMinimap)
            {
                int scale = _minimapSize == 1 ? MinimapScaleSmall : MinimapScaleLarge;
                int mapRange = 30; // Show 60x60 tiles (30 in each direction)
                int mapWidth = mapRange * 2 * scale;
                int mapHeight = mapRange * 2 * scale;
                int mapX = GraphicsDevice.Viewport.Width - mapWidth - 20;
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

                // Draw Entities on Minimap
                foreach (var entity in _entityManager.GetEntities())
                {
                    if (entity is Backpack)
                    {
                        // Draw Skull (Clamp to edge if out of range)
                        int entityTileX = (int)Math.Floor(entity.Position.X / TileMap.TileSize);
                        int entityTileY = (int)Math.Floor(entity.Position.Y / TileMap.TileSize);
                        
                        int relX = entityTileX - playerTileX;
                        int relY = entityTileY - playerTileY;
                        
                        // Check if out of range
                        bool outOfRange = Math.Abs(relX) >= mapRange || Math.Abs(relY) >= mapRange;
                        
                        // Clamp relative coordinates to [-mapRange, mapRange]
                        int clampedRelX = Math.Clamp(relX, -mapRange, mapRange - 1);
                        int clampedRelY = Math.Clamp(relY, -mapRange, mapRange - 1);
                        
                        int minimapX = mapX + (clampedRelX + mapRange) * scale;
                        int minimapY = mapY + (clampedRelY + mapRange) * scale;
                        
                        // Draw larger skull (3x scale) for visibility
                        int skullSize = scale * 3;
                        int skullX = minimapX - scale; // Center it
                        int skullY = minimapY - scale;
                        
                        if (_textures.ContainsKey("skull_crossbones"))
                        {
                            // Draw Red background to make it pop
                            _spriteBatch.Draw(_pixelTexture, new Rectangle(skullX, skullY, skullSize, skullSize), Color.Red);
                            _spriteBatch.Draw(_textures["skull_crossbones"], new Rectangle(skullX, skullY, skullSize, skullSize), Color.White);
                        }
                        else
                        {
                            _spriteBatch.Draw(_pixelTexture, new Rectangle(skullX, skullY, skullSize, skullSize), Color.Magenta);
                        }
                    }
                    else if (entity is Structure s && s.Type != "Rock" && s.Type != "Crystal")
                    {
                        // Draw colored dots for major structures
                        int entityTileX = (int)Math.Floor(entity.Position.X / TileMap.TileSize);
                        int entityTileY = (int)Math.Floor(entity.Position.Y / TileMap.TileSize);

                        int relX = entityTileX - playerTileX;
                        int relY = entityTileY - playerTileY;

                        // Check if in range to draw
                        if (Math.Abs(relX) < mapRange && Math.Abs(relY) < mapRange)
                        {
                            int minimapX = mapX + (relX + mapRange) * scale;
                            int minimapY = mapY + (relY + mapRange) * scale;

                            Color structColor = Color.Yellow; // Default

                            if (s.Type == "Spaceship") structColor = Color.Cyan;
                            else if (s.Type == "Greenhouse") structColor = Color.LimeGreen;
                            else if (s.Type == "Workbench") structColor = Color.Orange;

                            int iconSize = scale * 2;
                            int iconX = minimapX - (scale / 2);
                            int iconY = minimapY - (scale / 2);

                            _spriteBatch.Draw(_pixelTexture, new Rectangle(iconX, iconY, iconSize, iconSize), structColor);
                        }
                    }
                }

                // Draw Tunnel Entrances on Minimap (orange dots)
                foreach (var entrance in _tileMap.TunnelEntrances)
                {
                    int entTileX = (int)Math.Floor(entrance.X / TileMap.TileSize);
                    int entTileY = (int)Math.Floor(entrance.Y / TileMap.TileSize);

                    if (_tileMap.IsExplored(entTileX, entTileY))
                    {
                        int relX = entTileX - playerTileX;
                        int relY = entTileY - playerTileY;

                        if (Math.Abs(relX) < mapRange && Math.Abs(relY) < mapRange)
                        {
                            int mmX = mapX + (relX + mapRange) * scale;
                            int mmY = mapY + (relY + mapRange) * scale;
                            int iconSz = scale * 2;
                            _spriteBatch.Draw(_pixelTexture, new Rectangle(mmX - scale / 2, mmY - scale / 2, iconSz, iconSz), new Color(220, 70, 0));
                        }
                    }
                }

                // Draw Player (center of minimap)
                int playerScreenX = mapX + mapRange * scale;
                int playerScreenY = mapY + mapRange * scale;
                _spriteBatch.Draw(_pixelTexture, new Rectangle(playerScreenX, playerScreenY, scale, scale), Color.Red);
            }
            // Draw Save Button (Floppy)
            if (_floppyIcon != null)
            {
                Rectangle saveBtnRect = new Rectangle(GraphicsDevice.Viewport.Width - 50, GraphicsDevice.Viewport.Height - 50, 40, 40);
                
                if (_saveAnimationTimer > 0)
                {
                    // Pulse Effect
                    float scale = 1.0f + (float)Math.Sin(_saveAnimationTimer * Math.PI * 4) * 0.2f;
                    int size = (int)(40 * scale);
                    int offset = (size - 40) / 2;
                    Rectangle animRect = new Rectangle(saveBtnRect.X - offset, saveBtnRect.Y - offset, size, size);
                    _spriteBatch.Draw(_floppyIcon, animRect, Color.LimeGreen);
                }
                else
                {
                    _spriteBatch.Draw(_floppyIcon, saveBtnRect, Color.White);
                }
            }
            _spriteBatch.End();
            
            base.Draw(gameTime);
        }
        private void DrawLine(SpriteBatch spriteBatch, Texture2D texture, Vector2 start, Vector2 end, Color color, int thickness)
        {
            Vector2 edge = end - start;
            float angle = (float)Math.Atan2(edge.Y, edge.X);
            spriteBatch.Draw(texture, new Rectangle((int)start.X, (int)start.Y, (int)edge.Length(), thickness), null, color, angle, new Vector2(0, 0.5f), SpriteEffects.None, 0);
        }
        private void SaveGame()
        {
            SaveData data = new SaveData();
            
            // Player Data
            data.PlayerX = _player.Position.X;
            data.PlayerY = _player.Position.Y;
            data.Oxygen = _player.Oxygen;
            data.Hunger = _player.Hunger;
            data.BackpackLevel = _player.Inventory.UpgradeLevel;
            data.SuitLevel = _player.SuitLevel;
            
            // Inventory
            for (int y = 0; y < _player.Inventory.Rows; y++)
            {
                for (int x = 0; x < _player.Inventory.Cols; x++)
                {
                    string item = _player.Inventory.GetItem(x, y);
                    if (item != null)
                    {
                        data.Inventory.Add(new SaveData.InventoryItemData
                        {
                            Name = item,
                            Count = _player.Inventory.GetItemCount(x, y),
                            X = x,
                            Y = y
                        });
                    }
                }
            }
            
            // Structures
            foreach (var entity in _entityManager.GetEntities())
            {
                if (entity is Structure s)
                {
                    data.Structures.Add(new SaveData.StructureData
                    {
                        Type = s.Type,
                        X = s.Position.X,
                        Y = s.Position.Y,
                        RepairStage = s.RepairStage,
                        ContributedMaterials = s.ContributedMaterials.Count > 0 ? new Dictionary<string, int>(s.ContributedMaterials) : null
                    });
                }
            }
            
            try
            {
                data.Explored = _tileMap.GetExploredChunks();

                string jsonString = JsonSerializer.Serialize(data);
                File.WriteAllText(SaveFileName, jsonString);
                System.Console.WriteLine("Game Saved!");
                _saveExists = true;
                _saveAnimationTimer = 0.5f; // Trigger animation
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Save Failed: {ex.Message}");
            }
        }

        private void LoadGame()
        {
            if (!File.Exists(SaveFileName)) return;

            try
            {
                string jsonString = File.ReadAllText(SaveFileName);
                SaveData data = JsonSerializer.Deserialize<SaveData>(jsonString);
                
                // Clear existing
                // Clear existing entities
                _entityManager.Clear();
                
                // Update existing player instead of creating new one (preserves any references)
                _player.Position = new Vector2(data.PlayerX, data.PlayerY);
                // Safety check: if position is 0,0 (invalid save?), move to spawn
                if (_player.Position == Vector2.Zero)
                {
                    _player.Position = new Vector2(400, 304);
                }
                
                _player.Oxygen = data.Oxygen;
                _player.Hunger = data.Hunger;
                _player.SuitLevel = data.SuitLevel > 0 ? data.SuitLevel : 1;
                _player.Inventory.Clear();
                
                // Snap Camera
                _camera.Position = _player.Position;
                
                // Reset Intro/Menu state
                _spaceshipPosition = new Vector2(400, 200); // Set to landed position just in case
                _introTimer = 0f;

                // Restore Backpack Size
                while (_player.Inventory.UpgradeLevel < data.BackpackLevel)
                {
                    _player.Inventory.Upgrade();
                }

                // Restore Inventory to exact positions
                if (data.Inventory != null)
                {
                    foreach (var item in data.Inventory)
                    {
                        // Ensure it fits in bounds just in case
                        if (item.X >= 0 && item.X < _player.Inventory.Cols && item.Y >= 0 && item.Y < _player.Inventory.Rows)
                        {
                             _player.Inventory.SetItem(item.X, item.Y, item.Name, item.Count);
                        }
                    }
                }
                
                // Restore Structures
                if (data.Structures != null)
                {
                    foreach (var sData in data.Structures)
                    {
                        // Create local copy to modify
                        var currentData = sData;

                        // Repair logic for corrupted save (missing Type)
                        if (string.IsNullOrEmpty(currentData.Type))
                        {
                            // Detect Spaceship by location (approx 400, 200)
                            if (Math.Abs(currentData.X - 400) < 10 && Math.Abs(currentData.Y - 200) < 10)
                            {
                                currentData.Type = "Spaceship";
                                currentData.RepairStage = 0; // Ensure it has a valid stage
                            }
                            else
                            {
                                // Unknown corrupted entity, skip it
                                continue;
                            }
                        }

                        Structure s = new Structure(new Vector2(currentData.X, currentData.Y), currentData.Type, 80, 80);
                        
                        // Set specific sizes
                        if (currentData.Type == "Workbench") { s.Width = 40; s.Height = 40; }
                        else if (currentData.Type == "HAB") { s.Width = 80; s.Height = 80; }
                        else if (currentData.Type == "Machinery") { s.Width = 160; s.Height = 160; }
                        else if (currentData.Type == "Spaceship") { s.Width = 384; s.Height = 168; } // Ensure Size
                        
                        s.RepairStage = currentData.RepairStage;
                        if (currentData.ContributedMaterials != null)
                        {
                            foreach (var kvp in currentData.ContributedMaterials)
                            {
                                s.ContributeMaterial(kvp.Key, kvp.Value);
                            }
                        }
                        _entityManager.AddEntity(s);
                    }
                }
                
                // Unstuck Logic: Check if player is inside the Spaceship
                Rectangle shipRect = new Rectangle(400, 200, 384, 168);
                Rectangle playerRect = new Rectangle((int)_player.Position.X, (int)_player.Position.Y, 32, 64); // Approx player size
                
                if (shipRect.Intersects(playerRect))
                {
                    // Move player down to safe spot below the ship
                    _player.Position = new Vector2(400 + 192, 200 + 180);
                    _camera.Position = _player.Position;
                }
                
                // Add Player back to entity manager? 
                // The main Update loop handles _player separately, but _entityManager holds other things.
                // Wait, _player is NOT in _entityManager usually in this code base?
                // Lines 120+ in Update: _player.Update(...) is called explicitly.
                // Lines 247 in LoadContent: _player is created.
                // So _player is separate. Safe.
                
                // Restore Exploration
                if (data.Explored != null)
                {
                    _tileMap.LoadExploredChunks(data.Explored);
                }

                _currentGameState = GameState.Playing;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Load Failed: {ex.Message}");
            }
        }
    }
}
