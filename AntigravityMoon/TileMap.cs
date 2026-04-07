using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace AntigravityMoon
{
    public class TileMap
    {
        public const int TileSize = 64;
        public const int ChunkSize = 16; // 16x16 tiles per chunk

        // Tile types:
        // 0 = Dust/Moon ground
        // 1 = Rock
        // 2 = Crater
        // 3 = Tunnel Entrance (door to the cave)
        // 4 = Tunnel Interior (cave floor)
        // 5 = Tunnel Wall (solid cave border)
        // 6 = Cave Exit (Rope to surface)

        // Event for spawning entities when chunks are generated
        public event Action<Vector2, string> OnSpawnEntity;

        // All known tunnel entrance world positions (populated during generation)
        public List<Vector2> TunnelEntrances { get; } = new List<Vector2>();

        // The radius around an entrance considered "inside" the tunnel
        public const float TunnelEntranceRadius = 80f;

        private class Chunk
        {
            public int[,] Tiles;
            public bool[,] Explored;

            public Chunk()
            {
                Tiles = new int[ChunkSize, ChunkSize];
                Explored = new bool[ChunkSize, ChunkSize];
            }
        }

        private Dictionary<Point, Chunk> _chunks = new Dictionary<Point, Chunk>();
        private Random _random;

        // Track the last tunnel entrance world position for spacing enforcement
        private Vector2 _lastTunnelPos = Vector2.Zero;
        private bool _firstTunnelPlaced = false;

        public TileMap()
        {
            _random = new Random();
        }

        private Point GetChunkCoord(int tileX, int tileY)
        {
            return new Point(
                tileX >= 0 ? tileX / ChunkSize : (tileX - ChunkSize + 1) / ChunkSize,
                tileY >= 0 ? tileY / ChunkSize : (tileY - ChunkSize + 1) / ChunkSize
            );
        }

        private Chunk GetOrGenerateChunk(Point chunkCoord)
        {
            if (!_chunks.ContainsKey(chunkCoord))
            {
                _chunks[chunkCoord] = GenerateChunk(chunkCoord);
            }
            return _chunks[chunkCoord];
        }

        private Chunk GenerateChunk(Point chunkCoord)
        {
            Chunk chunk = new Chunk();

            // Use chunk coordinates as seed for deterministic generation
            int seed = chunkCoord.X * 1000 + chunkCoord.Y;
            Random chunkRandom = new Random(seed);

            for (int x = 0; x < ChunkSize; x++)
            {
                for (int y = 0; y < ChunkSize; y++)
                {
                    double roll = chunkRandom.NextDouble();
                    if (roll < 0.05) chunk.Tiles[x, y] = 2;       // 5% Crater
                    else if (roll < 0.15) chunk.Tiles[x, y] = 1;  // 10% Rock
                    else chunk.Tiles[x, y] = 0;                    // 85% Dust
                }
            }

            // --- UNDERGROUND BIOME ---
            if (chunkCoord.Y < -500)
            {
                // This is an underground cave room.
                // Fill entirely with floor, border with walls.
                for (int x = 0; x < ChunkSize; x++)
                {
                    for (int y = 0; y < ChunkSize; y++)
                    {
                        if (x == 0 || x == ChunkSize - 1 || y == 0 || y == ChunkSize - 1)
                            chunk.Tiles[x, y] = 5; // Wall
                        else
                            chunk.Tiles[x, y] = 4; // Floor
                    }
                }
                
                // Add an exit rope in the middle
                chunk.Tiles[ChunkSize / 2, ChunkSize / 2] = 6;
                
                // Spawn resources
                int count = chunkRandom.Next(6, 12);
                for (int i = 0; i < count; i++)
                {
                    int rx = chunkRandom.Next(2, ChunkSize - 2);
                    int ry = chunkRandom.Next(2, ChunkSize - 2);
                    if (rx != ChunkSize / 2 || ry != ChunkSize / 2) // don't spawn exactly on rope
                    {
                        Vector2 spawnPos = new Vector2((chunkCoord.X * ChunkSize + rx) * TileSize + TileSize / 2, (chunkCoord.Y * ChunkSize + ry) * TileSize + TileSize / 2);
                        OnSpawnEntity?.Invoke(spawnPos, "Metal");
                    }
                }
                
                return chunk;
            }

            // Clear starting area if this is the center chunk
            if (chunkCoord.X == 0 && chunkCoord.Y == 0)
            {
                for (int x = 0; x < ChunkSize; x++)
                    for (int y = 0; y < ChunkSize; y++)
                        chunk.Tiles[x, y] = 0;
            }

            // --- Tunnel / Cave Placement ---
            bool isFirstTunnelChunk = (chunkCoord.X == 1 && chunkCoord.Y == 1);
            bool placeTunnel = false;

            if (isFirstTunnelChunk && !_firstTunnelPlaced)
            {
                placeTunnel = true;
                _firstTunnelPlaced = true;
            }
            else if (!isFirstTunnelChunk && chunkCoord.X != 0 && chunkCoord.Y != 0)
            {
                double tunnelRoll = chunkRandom.NextDouble();
                if (tunnelRoll < 0.001) // ~0.1% per chunk
                {
                    Vector2 candidatePos = new Vector2(chunkCoord.X * ChunkSize * TileSize, chunkCoord.Y * ChunkSize * TileSize);
                    bool farEnough = (_lastTunnelPos == Vector2.Zero) || Vector2.Distance(candidatePos, _lastTunnelPos) > 8000f;
                    bool farFromSpawn = Vector2.Distance(candidatePos, Vector2.Zero) > 3000f;

                    if (farEnough && farFromSpawn)
                    {
                        placeTunnel = true;
                    }
                }
            }

            if (placeTunnel)
            {
               // Place the entrance at a fixed local tile position within the chunk
               int ex = 8;
               int ey = 8;
               chunk.Tiles[ex, ey] = 3; // Tunnel entrance
               
               Vector2 entrancePos = new Vector2((chunkCoord.X * ChunkSize + ex) * TileSize, (chunkCoord.Y * ChunkSize + ey) * TileSize);
               TunnelEntrances.Add(entrancePos);
               _lastTunnelPos = entrancePos;
            }

            // --- Regular Resource Spawning ---
            for (int x = 0; x < ChunkSize; x++)
            {
                for (int y = 0; y < ChunkSize; y++)
                {
                    // Don't spawn on craters or tunnel entrances
                    if (chunk.Tiles[x, y] != 2 && chunk.Tiles[x, y] != 3)
                    {
                        int worldTileX = chunkCoord.X * ChunkSize + x;
                        int worldTileY = chunkCoord.Y * ChunkSize + y;
                        Vector2 worldPos = new Vector2(worldTileX * TileSize + TileSize / 2, worldTileY * TileSize + TileSize / 2);

                        double spawnRoll = chunkRandom.NextDouble();

                        bool isDarkSide = chunkCoord.X > 2000;

                        // Don't spawn items too close to start location
                        if (Vector2.Distance(worldPos, Vector2.Zero) < 800f)
                            continue;

                        if (isDarkSide)
                        {
                            if (spawnRoll < 0.05) OnSpawnEntity?.Invoke(worldPos, "Crystal");
                            else if (spawnRoll < 0.10) OnSpawnEntity?.Invoke(worldPos, "Rock");
                        }
                        else
                        {
                            if (spawnRoll < 0.02) OnSpawnEntity?.Invoke(worldPos, "Rock");
                            else if (spawnRoll < 0.03) OnSpawnEntity?.Invoke(worldPos, "Crystal");
                        }
                    }
                }
            }

            return chunk;
        }



        public void Explore(Vector2 position, float radius)
        {
            int centerTileX = (int)Math.Floor(position.X / TileSize);
            int centerTileY = (int)Math.Floor(position.Y / TileSize);
            int radiusInTiles = (int)(radius / TileSize);

            for (int x = centerTileX - radiusInTiles; x <= centerTileX + radiusInTiles; x++)
            {
                for (int y = centerTileY - radiusInTiles; y <= centerTileY + radiusInTiles; y++)
                {
                    if (Vector2.Distance(new Vector2(x * TileSize, y * TileSize), position) <= radius)
                    {
                        Point chunkCoord = GetChunkCoord(x, y);
                        Chunk chunk = GetOrGenerateChunk(chunkCoord);

                        int localX = x >= 0 ? x % ChunkSize : (ChunkSize - 1) - ((-x - 1) % ChunkSize);
                        int localY = y >= 0 ? y % ChunkSize : (ChunkSize - 1) - ((-y - 1) % ChunkSize);

                        chunk.Explored[localX, localY] = true;
                    }
                }
            }
        }

        public int GetTile(int x, int y)
        {
            Point chunkCoord = GetChunkCoord(x, y);
            Chunk chunk = GetOrGenerateChunk(chunkCoord);

            int localX = x >= 0 ? x % ChunkSize : (ChunkSize - 1) - ((-x - 1) % ChunkSize);
            int localY = y >= 0 ? y % ChunkSize : (ChunkSize - 1) - ((-y - 1) % ChunkSize);

            return chunk.Tiles[localX, localY];
        }

        /// <summary>
        /// Returns true if player is standing on Cave Floor (Tile 4) or Tunnel Entrance (Tile 3)
        /// </summary>
        public bool IsInsideTunnel(Vector2 worldPos)
        {
            int tileX = (int)Math.Floor(worldPos.X / TileSize);
            int tileY = (int)Math.Floor(worldPos.Y / TileSize);
            
            int tile = GetTile(tileX, tileY);
            return tile == 4 || tile == 3;
        }

        public bool IsExplored(int x, int y)
        {
            Point chunkCoord = GetChunkCoord(x, y);
            if (!_chunks.ContainsKey(chunkCoord)) return false;

            Chunk chunk = _chunks[chunkCoord];
            int localX = x >= 0 ? x % ChunkSize : (ChunkSize - 1) - ((-x - 1) % ChunkSize);
            int localY = y >= 0 ? y % ChunkSize : (ChunkSize - 1) - ((-y - 1) % ChunkSize);

            return chunk.Explored[localX, localY];
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D texture, Rectangle cameraViewRect, double totalSeconds = 0)
        {
            int startTileX = (int)Math.Floor((float)cameraViewRect.Left / TileSize) - 1;
            int endTileX   = (int)Math.Floor((float)cameraViewRect.Right / TileSize) + 1;
            int startTileY = (int)Math.Floor((float)cameraViewRect.Top / TileSize) - 1;
            int endTileY   = (int)Math.Floor((float)cameraViewRect.Bottom / TileSize) + 1;

            for (int x = startTileX; x <= endTileX; x++)
            {
                for (int y = startTileY; y <= endTileY; y++)
                {
                    int tileType = GetTile(x, y);
                    bool explored = IsExplored(x, y);

                    Color color;
                    bool isDarkSide = x > 32000;

                    if (tileType == 3) // Tunnel Entrance
                    {
                        color = new Color(80, 70, 70); 
                    }
                    else if (tileType == 4) // Cave Floor
                    {
                        color = new Color(50, 40, 40); // Darker floor
                    }
                    else if (tileType == 5) // Cave Wall (solid)
                    {
                        color = new Color(20, 15, 15); // Almost black walls
                    }
                    else if (tileType == 6) // Cave Exit (Rope / Hole)
                    {
                        color = new Color(130, 100, 70); // Brownish rope look
                    }
                    else if (isDarkSide)
                    {
                        if (tileType == 0) color = new Color(20, 0, 40);
                        else if (tileType == 1) color = new Color(60, 20, 80);
                        else color = Color.Black;
                    }
                    else
                    {
                        if (tileType == 0) color = Color.Gray;
                        else if (tileType == 1) color = Color.DarkGray;
                        else color = Color.Black; // Crater
                    }

                    if (!explored)
                    {
                        color = Color.Multiply(color, 0.3f);
                    }

                    spriteBatch.Draw(texture, new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize), color);
                }
            }
        }

        public List<SaveData.ExploredChunkData> GetExploredChunks()
        {
            List<SaveData.ExploredChunkData> savedChunks = new List<SaveData.ExploredChunkData>();
            foreach (var kvp in _chunks)
            {
                Chunk chunk = kvp.Value;
                bool hasExplored = false;
                List<bool> tiles = new List<bool>();

                for (int x = 0; x < ChunkSize; x++)
                {
                    for (int y = 0; y < ChunkSize; y++)
                    {
                        if (chunk.Explored[x, y]) hasExplored = true;
                        tiles.Add(chunk.Explored[x, y]);
                    }
                }

                if (hasExplored)
                {
                    savedChunks.Add(new SaveData.ExploredChunkData
                    {
                        X = kvp.Key.X,
                        Y = kvp.Key.Y,
                        Tiles = tiles
                    });
                }
            }
            return savedChunks;
        }

        public void LoadExploredChunks(List<SaveData.ExploredChunkData> savedChunks)
        {
            foreach (var data in savedChunks)
            {
                Point chunkCoord = new Point(data.X, data.Y);
                Chunk chunk = GetOrGenerateChunk(chunkCoord);

                int i = 0;
                for (int x = 0; x < ChunkSize; x++)
                {
                    for (int y = 0; y < ChunkSize; y++)
                    {
                        if (i < data.Tiles.Count)
                        {
                            chunk.Explored[x, y] = data.Tiles[i];
                            i++;
                        }
                    }
                }
            }
        }
    }
}
