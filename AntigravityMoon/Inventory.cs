using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AntigravityMoon
{
    public class Inventory
    {
        public int Rows { get; private set; } = 1;
        public int Cols { get; private set; } = 5;
        public int MaxStack { get; private set; } = 10;
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

        public bool IsStorageEmpty()
        {
            for (int y = 1; y < Rows; y++)
            {
                for (int x = 0; x < Cols; x++)
                {
                    if (_items[x, y].ItemName != null) return false;
                }
            }
            return true;
        }

        public bool SetUpgradeLevel(int level)
        {
            if (level == 0)
            {
                UpgradeLevel = 0;
                Resize(5, 1);
                MaxStack = 10;
                return true;
            }
            else if (level == 1)
            {
                UpgradeLevel = 1;
                Resize(5, 3); // Row 0: Quickbar (5), Rows 1-2: Space Bag (10)
                MaxStack = 50;
                return true;
            }
            else if (level == 2)
            {
                UpgradeLevel = 2;
                Resize(5, 5); // Row 0: Quickbar (5), Rows 1-4: Advanced Space Bag (20)
                MaxStack = 99;
                return true;
            }
            return false;
        }

        public bool Upgrade()
        {
            if (UpgradeLevel < 2)
            {
                return SetUpgradeLevel(UpgradeLevel + 1);
            }
            return false;
        }

        public void SwapSlots(int x1, int y1, int x2, int y2)
        {
            if (x1 < 0 || x1 >= Cols || y1 < 0 || y1 >= Rows) return;
            if (x2 < 0 || x2 >= Cols || y2 < 0 || y2 >= Rows) return;
            if (x1 == x2 && y1 == y2) return;

            // If same item, try to stack into (x2, y2)
            if (_items[x1, y1].ItemName != null && _items[x1, y1].ItemName == _items[x2, y2].ItemName)
            {
                int space = MaxStack - _items[x2, y2].Count;
                if (space > 0)
                {
                    int toMove = Math.Min(_items[x1, y1].Count, space);
                    _items[x2, y2].Count += toMove;
                    _items[x1, y1].Count -= toMove;
                    if (_items[x1, y1].Count <= 0)
                    {
                        _items[x1, y1].ItemName = null;
                        _items[x1, y1].Count = 0;
                    }
                    return;
                }
            }

            // Otherwise swap
            var temp = _items[x1, y1];
            _items[x1, y1] = _items[x2, y2];
            _items[x2, y2] = temp;
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

        public bool AddItem(string itemName, int count)
        {
            int remaining = count;
            // 1. Try to stack
            for (int y = 0; y < Rows && remaining > 0; y++)
            {
                for (int x = 0; x < Cols && remaining > 0; x++)
                {
                    if (_items[x, y].ItemName == itemName && _items[x, y].Count < MaxStack)
                    {
                        int add = Math.Min(remaining, MaxStack - _items[x, y].Count);
                        _items[x, y].Count += add;
                        remaining -= add;
                    }
                }
            }

            // 2. Find empty slot
            for (int y = 0; y < Rows && remaining > 0; y++)
            {
                for (int x = 0; x < Cols && remaining > 0; x++)
                {
                    if (_items[x, y].ItemName == null)
                    {
                        int add = Math.Min(remaining, MaxStack);
                        _items[x, y].ItemName = itemName;
                        _items[x, y].Count = add;
                        remaining -= add;
                    }
                }
            }
            return remaining == 0;
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
            newInv.MaxStack = this.MaxStack;
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
                        string textureKey = itemName.ToLower().Replace(" ", "_");
                        
                        Texture2D drawTex = null;
                        Color drawTint = Color.White;
                        
                        if (itemName == "Ice Crystal" && itemTextures.ContainsKey("crystal"))
                        {
                            drawTex = itemTextures["crystal"];
                            drawTint = Color.LightCyan;
                        }
                        else if (itemName == "Volcanic Ore" && itemTextures.ContainsKey("metal"))
                        {
                            drawTex = itemTextures["metal"];
                            drawTint = Color.Red;
                        }
                        else if (itemName == "Radioactive Slag" && itemTextures.ContainsKey("metal"))
                        {
                            drawTex = itemTextures["metal"];
                            drawTint = Color.Green;
                        }
                        else if (itemName == "Lantern" && itemTextures.ContainsKey("crystal"))
                        {
                            drawTex = itemTextures["crystal"];
                            drawTint = Color.Yellow;
                        }
                        else if (itemName == "Heat Shield" && itemTextures.ContainsKey("bionic_tech"))
                        {
                            drawTex = itemTextures["bionic_tech"];
                            drawTint = Color.Coral;
                        }
                        else if (itemName == "Rad Shield" && itemTextures.ContainsKey("bionic_tech"))
                        {
                            drawTex = itemTextures["bionic_tech"];
                            drawTint = Color.LimeGreen;
                        }
                        else if (itemName == "Pulse Laser" && itemTextures.ContainsKey("bionic_tech"))
                        {
                            drawTex = itemTextures["bionic_tech"];
                            drawTint = Color.LimeGreen;
                        }
                        else if (itemName == "Plasma Laser" && itemTextures.ContainsKey("bionic_tech"))
                        {
                            drawTex = itemTextures["bionic_tech"];
                            drawTint = Color.MediumPurple;
                        }
                        else if (itemName == "Reinforced Suit" && itemTextures.ContainsKey("astronaut"))
                        {
                            drawTex = itemTextures["astronaut"];
                            drawTint = Color.DarkOrange;
                        }
                        else if (itemName == "Radiation Suit" && itemTextures.ContainsKey("astronaut"))
                        {
                            drawTex = itemTextures["astronaut"];
                            drawTint = Color.GreenYellow;
                        }
                        else if ((itemName == "Space Bag" || itemName == "Advanced Space Bag") && itemTextures.ContainsKey("backpack"))
                        {
                            drawTex = itemTextures["backpack"];
                            drawTint = itemName == "Advanced Space Bag" ? Color.Gold : Color.White;
                        }
                        else if (itemTextures != null && itemTextures.ContainsKey(textureKey))
                        {
                            drawTex = itemTextures[textureKey];
                        }
                        
                        if (drawTex != null)
                        {
                             spriteBatch.Draw(drawTex, new Rectangle(rect.X + 5, rect.Y + 5, cellSize - 10, cellSize - 10), drawTint);
                        }
                        else
                        {
                            // Fallback color square
                            Color color = Color.White;
                            if (itemName == "Rock") color = Color.Gray;
                            else if (itemName == "Crystal") color = Color.Cyan;
                            else if (itemName == "Corn") color = Color.Yellow;
                            else if (itemName == "Ice Crystal") color = Color.LightCyan;
                            else if (itemName == "Volcanic Ore") color = Color.Red;
                            else if (itemName == "Radioactive Slag") color = Color.Green;
                            else if (itemName == "Lantern") color = Color.Yellow;
                            else if (itemName == "Heat Shield") color = Color.Coral;
                            else if (itemName == "Rad Shield") color = Color.LimeGreen;
                            else if (itemName == "Pulse Laser") color = Color.LimeGreen;
                            else if (itemName == "Plasma Laser") color = Color.MediumPurple;
                            else if (itemName == "Reinforced Suit") color = Color.DarkOrange;
                            else if (itemName == "Radiation Suit") color = Color.GreenYellow;
                            else if (itemName == "Space Bag") color = Color.Tan;
                            else if (itemName == "Advanced Space Bag") color = Color.Gold;
                            
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
