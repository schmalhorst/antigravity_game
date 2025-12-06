using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace AntigravityMoon
{
    public class TileMap
    {
        public const int TileSize = 48;
        public const int ChunkSize = 16; // 16x16 tiles per chunk
        
        // Event for spawning entities when chunks are generated
        public event Action<Vector2, string> OnSpawnEntity;
        
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
                    if (roll < 0.05) chunk.Tiles[x, y] = 2; // 5% Crater
                    else if (roll < 0.15) chunk.Tiles[x, y] = 1; // 10% Rock
                    else chunk.Tiles[x, y] = 0; // 85% Dust
                }
            }
            
            // Clear starting area if this is the center chunk
            if (chunkCoord.X == 0 && chunkCoord.Y == 0)
            {
                for (int x = 0; x < ChunkSize; x++)
                {
                    for (int y = 0; y < ChunkSize; y++)
                    {
                        chunk.Tiles[x, y] = 0;
                    }
                }
            }
            
                // Spawn resources in this chunk
            for (int x = 0; x < ChunkSize; x++)
            {
                for (int y = 0; y < ChunkSize; y++)
                {
                    // Don't spawn on craters
                    if (chunk.Tiles[x, y] != 2)
                    {
                        // Calculate world position
                        int worldTileX = chunkCoord.X * ChunkSize + x;
                        int worldTileY = chunkCoord.Y * ChunkSize + y;
                        Vector2 worldPos = new Vector2(worldTileX * TileSize + TileSize / 2, worldTileY * TileSize + TileSize / 2);
                        
                        double spawnRoll = chunkRandom.NextDouble();
                        
                        bool isDarkSide = chunkCoord.X > 2000; // X > 96,000 pixels

                        if (isDarkSide)
                        {
                            // Dark Side: More Crystals
                            if (spawnRoll < 0.05) OnSpawnEntity?.Invoke(worldPos, "Crystal");
                            else if (spawnRoll < 0.10) OnSpawnEntity?.Invoke(worldPos, "Rock");
                        }
                        else
                        {
                            // Normal
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
                    // Simple distance check
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

        public bool IsExplored(int x, int y)
        {
            Point chunkCoord = GetChunkCoord(x, y);
            if (!_chunks.ContainsKey(chunkCoord)) return false;
            
            Chunk chunk = _chunks[chunkCoord];
            int localX = x >= 0 ? x % ChunkSize : (ChunkSize - 1) - ((-x - 1) % ChunkSize);
            int localY = y >= 0 ? y % ChunkSize : (ChunkSize - 1) - ((-y - 1) % ChunkSize);
            
            return chunk.Explored[localX, localY];
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D texture, Rectangle cameraViewRect)
        {
            // Calculate tile range visible in camera
            int startTileX = (int)Math.Floor((float)cameraViewRect.Left / TileSize) - 1;
            int endTileX = (int)Math.Floor((float)cameraViewRect.Right / TileSize) + 1;
            int startTileY = (int)Math.Floor((float)cameraViewRect.Top / TileSize) - 1;
            int endTileY = (int)Math.Floor((float)cameraViewRect.Bottom / TileSize) + 1;

            for (int x = startTileX; x <= endTileX; x++)
            {
                for (int y = startTileY; y <= endTileY; y++)
                {
                    int tileType = GetTile(x, y);
                    bool explored = IsExplored(x, y);
                    
                    Color color = Color.White;
                    
                    bool isDarkSide = x > 32000; // Corresponds to chunk 2000 * 16

                    if (isDarkSide)
                    {
                        // Dark Side Palette
                        if (tileType == 0) color = new Color(20, 0, 40); // Dark Purple dust
                        else if (tileType == 1) color = new Color(60, 20, 80); // Lighter purple rock
                        else if (tileType == 2) color = Color.Black;
                    }
                    else
                    {
                        // Normal Palette
                        if (tileType == 0) color = Color.Gray;
                        else if (tileType == 1) color = Color.DarkGray;
                        else if (tileType == 2) color = Color.Black;
                    }

                    if (!explored)
                    {
                        color = Color.Multiply(color, 0.3f); // Darken unexplored areas
                    }

                    spriteBatch.Draw(texture, new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize), color);
                }
            }
        }
    }
}
