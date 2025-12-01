using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AntigravityMoon
{
    public class Inventory
    {
        public const int Rows = 8;
        public const int Cols = 8;
        public const int MaxStack = 10;

        public struct InventorySlot
        {
            public string ItemName;
            public int Count;
        }

        private InventorySlot[,] _items;

        public Inventory()
        {
            _items = new InventorySlot[Cols, Rows];
        }

        public bool AddItem(string itemName)
        {
            // 1. Try to stack
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Cols; x++)
                {
                    if (_items[x, y].ItemName == itemName && _items[x, y].Count < MaxStack)
                    {
                        _items[x, y].Count++;
                        return true;
                    }
                }
            }

            // 2. Find empty slot
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Cols; x++)
                {
                    if (_items[x, y].ItemName == null)
                    {
                        _items[x, y].ItemName = itemName;
                        _items[x, y].Count = 1;
                        return true;
                    }
                }
            }
            return false; // Full
        }

        public string GetItem(int x, int y)
        {
            if (x < 0 || x >= Cols || y < 0 || y >= Rows) return null;
            return _items[x, y].ItemName;
        }

        public void RemoveItem(int x, int y)
        {
            if (x >= 0 && x < Cols && y >= 0 && y < Rows)
            {
                if (_items[x, y].ItemName != null)
                {
                    _items[x, y].Count--;
                    if (_items[x, y].Count <= 0)
                    {
                        _items[x, y].ItemName = null;
                        _items[x, y].Count = 0;
                    }
                }
            }
        }

        public int CountItem(string type)
        {
            int count = 0;
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Cols; x++)
                {
                    if (_items[x, y].ItemName == type)
                    {
                        count += _items[x, y].Count;
                    }
                }
            }
            return count;
        }

        public bool RemoveItems(string type, int count)
        {
            if (CountItem(type) < count) return false;

            int remainingToRemove = count;
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Cols; x++)
                {
                    if (_items[x, y].ItemName == type)
                    {
                        if (_items[x, y].Count >= remainingToRemove)
                        {
                            _items[x, y].Count -= remainingToRemove;
                            remainingToRemove = 0;
                            if (_items[x, y].Count == 0) _items[x, y].ItemName = null;
                            return true;
                        }
                        else
                        {
                            remainingToRemove -= _items[x, y].Count;
                            _items[x, y].Count = 0;
                            _items[x, y].ItemName = null;
                        }
                    }
                }
            }
            return true;
        }

        public void Clear()
        {
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Cols; x++)
                {
                    _items[x, y].ItemName = null;
                    _items[x, y].Count = 0;
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, Dictionary<string, Texture2D> itemTextures, Vector2 position)
        {
            int cellSize = 40;
            int padding = 5;

            // Draw background
            spriteBatch.Draw(pixelTexture, new Rectangle((int)position.X, (int)position.Y, Cols * (cellSize + padding) + padding, Rows * (cellSize + padding) + padding), Color.Black * 0.8f);

            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Cols; x++)
                {
                    Rectangle rect = new Rectangle((int)position.X + padding + x * (cellSize + padding), (int)position.Y + padding + y * (cellSize + padding), cellSize, cellSize);
                    
                    // Draw cell background
                    spriteBatch.Draw(pixelTexture, rect, Color.DarkGray);

                    // Draw item if present
                    if (_items[x, y].ItemName != null)
                    {
                        string itemName = _items[x, y].ItemName;
                        string textureKey = itemName.ToLower();
                        
                        if (itemTextures != null && itemTextures.ContainsKey(textureKey))
                        {
                             spriteBatch.Draw(itemTextures[textureKey], new Rectangle(rect.X + 5, rect.Y + 5, cellSize - 10, cellSize - 10), Color.White);
                        }
                        else
                        {
                            // Fallback color square
                            Color color = Color.White;
                            if (itemName == "Rock") color = Color.Gray;
                            else if (itemName == "Crystal") color = Color.Cyan;
                            else if (itemName == "Corn") color = Color.Yellow;
                            
                            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X + 5, rect.Y + 5, cellSize - 10, cellSize - 10), color);
                        }
                        
                        // Draw Item Name (Small)
                        PixelTextRenderer.DrawText(spriteBatch, pixelTexture, itemName, new Vector2(rect.X + 2, rect.Y + 2), Color.Black, 1);

                        // Draw Count if > 1
                        if (_items[x, y].Count > 1)
                        {
                            string countText = _items[x, y].Count.ToString();
                            Vector2 countPos = new Vector2(rect.X + cellSize - 12, rect.Y + cellSize - 8);
                            
                            // Draw background for count
                            spriteBatch.Draw(pixelTexture, new Rectangle((int)countPos.X - 1, (int)countPos.Y - 1, (countText.Length * 4) + 1, 7), Color.Black * 0.7f);
                            
                            // Draw bottom right
                            PixelTextRenderer.DrawText(spriteBatch, pixelTexture, countText, countPos, Color.White, 1);
                        }
                    }
                }
            }
        }
    }
}
