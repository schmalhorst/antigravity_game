using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace AntigravityMoon
{
    public class TileMap
    {
        public const int TileSize = 48;
        public int Width { get; private set; }
        public int Height { get; private set; }
        private int[,] _tiles;
        private Random _random;

        public TileMap(int width, int height)
        {
            Width = width;
            Height = height;
            _tiles = new int[width, height];
            _random = new Random();
            Generate();
        }

        private void Generate()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    // 0 = Dust, 1 = Rock (10% chance)
                    _tiles[x, y] = _random.NextDouble() < 0.1 ? 1 : 0;
                }
            }
        }

        public int GetTile(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return 1; // Out of bounds is rock
            return _tiles[x, y];
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D texture)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Color color = _tiles[x, y] == 0 ? Color.Gray : Color.DarkGray;
                    spriteBatch.Draw(texture, new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize), color);
                }
            }
        }
    }
}
