using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AntigravityMoon
{
    public class Inventory
    {
        public int Rows { get; private set; } = 1;
        public int Cols { get; private set; } = 4;
        public const int MaxStack = 10;
        public int UpgradeLevel { get; private set; } = 0;

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

        public bool IsEmpty()
        {
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Cols; x++)
                {
                    if (_items[x, y].ItemName != null) return false;
                }
            }
            return true;
        }

        public bool Upgrade()
        {
            if (UpgradeLevel == 0)
            {
                // Upgrade to 8 cols, 1 row
                UpgradeLevel++;
                Resize(8, 1);
                return true;
            }
            else if (UpgradeLevel == 1)
            {
                // Upgrade to 8 cols, 2 rows
                UpgradeLevel++;
                Resize(8, 2);
                return true;
            }
            return false;
        }

        private void Resize(int newCols, int newRows)
        {
            var newItems = new InventorySlot[newCols, newRows];
            
            // Copy existing items
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Cols; x++)
                {
                    if (x < newCols && y < newRows)
                    {
                        newItems[x, y] = _items[x, y];
                    }
                }
            }

            _items = newItems;
            Cols = newCols;
            Rows = newRows;
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

        public bool CanAddItem(string itemName)
        {
            // 1. Try to stack
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Cols; x++)
                {
                    if (_items[x, y].ItemName == itemName && _items[x, y].Count < MaxStack)
                    {
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

        public bool SetItem(int x, int y, string itemName, int count)
        {
            if (x < 0 || x >= Cols || y < 0 || y >= Rows) return false;
            
            _items[x, y].ItemName = itemName;
            _items[x, y].Count = count;
            return true;
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
        
        public int GetItemCount(int x, int y)
        {
            if (x >= 0 && x < Cols && y >= 0 && y < Rows)
            {
                return _items[x, y].Count;
            }
            return 0;
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

        public string GetItemAt(int index)
        {
            int x = index % Cols;
            int y = index / Cols;
            return GetItem(x, y);
        }

        public void RemoveItemAt(int index)
        {
            int x = index % Cols;
            int y = index / Cols;
            RemoveItem(x, y);
        }

        public Inventory Clone()
        {
            Inventory newInv = new Inventory();
            newInv.UpgradeLevel = this.UpgradeLevel;
            newInv.Rows = this.Rows;
            newInv.Cols = this.Cols;
            newInv._items = new InventorySlot[this.Cols, this.Rows]; // Initialize the _items array in the new inventory
            
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Cols; x++)
                {
                    newInv._items[x, y] = this._items[x, y]; // Copy each InventorySlot struct
                }
            }
            return newInv;
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

        public void MergeAndSort()
        {
            // 1. Aggregate counts
            Dictionary<string, int> totalCounts = new Dictionary<string, int>();
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Cols; x++)
                {
                     if (_items[x, y].ItemName != null)
                     {
                         string item = _items[x, y].ItemName;
                         if (!totalCounts.ContainsKey(item))
                         {
                             totalCounts[item] = 0;
                         }
                         totalCounts[item] += _items[x, y].Count;
                     }
                }
            }

            // 2. Clear Inventory
            Clear();

            // 3. Re-add items sorted alphabetically
            List<string> sortedKeys = new List<string>(totalCounts.Keys);
            sortedKeys.Sort();

            foreach (string item in sortedKeys)
            {
                 int count = totalCounts[item];
                 while (count > 0)
                 {
                      int addAmount = Math.Min(count, MaxStack);
                      
                      // Find first empty slot directly since we know we just cleared and stack logic isn't needed here (we are doing it cleanly)
                      bool placed = false;
                      for (int y = 0; y < Rows && !placed; y++)
                      {
                          for (int x = 0; x < Cols && !placed; x++)
                          {
                              if (_items[x, y].ItemName == null)
                              {
                                  _items[x, y].ItemName = item;
                                  _items[x, y].Count = addAmount;
                                  count -= addAmount;
                                  placed = true;
                              }
                          }
                      }
                      
                      if (!placed) break; // Inventory ran out of space (should be mathematically impossible given we only cleared)
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
                        // PixelTextRenderer.DrawText(spriteBatch, pixelTexture, itemName, new Vector2(rect.X + 2, rect.Y + 2), Color.Black, 1); // Removed as per new logic

                        // Draw Count if > 1
                        if (_items[x, y].Count > 1)
                        {
                            string countText = "x" + _items[x, y].Count;
                            Vector2 countPos = new Vector2(rect.X + 2, rect.Y + cellSize - 14); // Position at bottom-left
                            
                            // Draw background for count (larger)
                            int bgWidth = (countText.Length * 6) + 2;
                            spriteBatch.Draw(pixelTexture, new Rectangle((int)countPos.X - 1, (int)countPos.Y - 1, bgWidth, 10), Color.Black * 0.9f);
                            
                            // Draw count text (font size 1)
                            PixelTextRenderer.DrawText(spriteBatch, pixelTexture, countText, countPos, Color.White, 1);
                        }
                    }
                }
            }
        }
    }
}
