using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace AntigravityMoon
{
    public static class PixelTextRenderer
    {
        private static Dictionary<char, bool[,]> _charMap;

        public static void Init()
        {
            _charMap = new Dictionary<char, bool[,]>();

            // 3x5 Font Definitions
            // A
            _charMap['A'] = new bool[,] {
                { false, true, false },
                { true, false, true },
                { true, true, true },
                { true, false, true },
                { true, false, true }
            };
            // B
            _charMap['B'] = new bool[,] {
                { true, true, false },
                { true, false, true },
                { true, true, false },
                { true, false, true },
                { true, true, false }
            };
            // C
            _charMap['C'] = new bool[,] {
                { false, true, true },
                { true, false, false },
                { true, false, false },
                { true, false, false },
                { false, true, true }
            };
            // D
            _charMap['D'] = new bool[,] {
                { true, true, false },
                { true, false, true },
                { true, false, true },
                { true, false, true },
                { true, true, false }
            };
            // E
            _charMap['E'] = new bool[,] {
                { true, true, true },
                { true, false, false },
                { true, true, false },
                { true, false, false },
                { true, true, true }
            };
            // F
            _charMap['F'] = new bool[,] {
                { true, true, true },
                { true, false, false },
                { true, true, false },
                { true, false, false },
                { true, false, false }
            };
            // G
            _charMap['G'] = new bool[,] {
                { false, true, true },
                { true, false, false },
                { true, false, true },
                { true, false, true },
                { false, true, true }
            };
            // H
            _charMap['H'] = new bool[,] {
                { true, false, true },
                { true, false, true },
                { true, true, true },
                { true, false, true },
                { true, false, true }
            };
            // I
            _charMap['I'] = new bool[,] {
                { true, true, true },
                { false, true, false },
                { false, true, false },
                { false, true, false },
                { true, true, true }
            };
            // J
            _charMap['J'] = new bool[,] {
                { false, false, true },
                { false, false, true },
                { false, false, true },
                { true, false, true },
                { false, true, false }
            };
            // K
            _charMap['K'] = new bool[,] {
                { true, false, true },
                { true, false, true },
                { true, true, false },
                { true, false, true },
                { true, false, true }
            };
            // L
            _charMap['L'] = new bool[,] {
                { true, false, false },
                { true, false, false },
                { true, false, false },
                { true, false, false },
                { true, true, true }
            };
            // M
            _charMap['M'] = new bool[,] {
                { true, false, true },
                { true, true, true },
                { true, false, true },
                { true, false, true },
                { true, false, true }
            };
            // N
            _charMap['N'] = new bool[,] {
                { true, true, false },
                { true, false, true },
                { true, false, true },
                { true, false, true },
                { true, false, true }
            };
            // O
            _charMap['O'] = new bool[,] {
                { false, true, false },
                { true, false, true },
                { true, false, true },
                { true, false, true },
                { false, true, false }
            };
            // P
            _charMap['P'] = new bool[,] {
                { true, true, false },
                { true, false, true },
                { true, true, true },
                { true, false, false },
                { true, false, false }
            };
            // Q
            _charMap['Q'] = new bool[,] {
                { false, true, false },
                { true, false, true },
                { true, false, true },
                { true, true, true },
                { false, false, true }
            };
            // R
            _charMap['R'] = new bool[,] {
                { true, true, false },
                { true, false, true },
                { true, true, false },
                { true, false, true },
                { true, false, true }
            };
            // S
            _charMap['S'] = new bool[,] {
                { false, true, true },
                { true, false, false },
                { false, true, false },
                { false, false, true },
                { true, true, false }
            };
            // T
            _charMap['T'] = new bool[,] {
                { true, true, true },
                { false, true, false },
                { false, true, false },
                { false, true, false },
                { false, true, false }
            };
            // U
            _charMap['U'] = new bool[,] {
                { true, false, true },
                { true, false, true },
                { true, false, true },
                { true, false, true },
                { false, true, false }
            };
            // V
            _charMap['V'] = new bool[,] {
                { true, false, true },
                { true, false, true },
                { true, false, true },
                { true, false, true },
                { false, true, false }
            };
            // W
            _charMap['W'] = new bool[,] {
                { true, false, true },
                { true, false, true },
                { true, false, true },
                { true, true, true },
                { true, false, true }
            };
            // X
            _charMap['X'] = new bool[,] {
                { true, false, true },
                { true, false, true },
                { false, true, false },
                { true, false, true },
                { true, false, true }
            };
            // <
            _charMap['<'] = new bool[,] {
                { false, false, true },
                { false, true, false },
                { true, false, false },
                { false, true, false },
                { false, false, true }
            };
            // Y
            _charMap['Y'] = new bool[,] {
                { true, false, true },
                { true, false, true },
                { false, true, false },
                { false, true, false },
                { false, true, false }
            };
            // Z
            _charMap['Z'] = new bool[,] {
                { true, true, true },
                { false, false, true },
                { false, true, false },
                { true, false, false },
                { true, true, true }
            };
            // 0
            _charMap['0'] = new bool[,] {
                { false, true, false },
                { true, false, true },
                { true, false, true },
                { true, false, true },
                { false, true, false }
            };
            // 1
            _charMap['1'] = new bool[,] {
                { false, true, false },
                { true, true, false },
                { false, true, false },
                { false, true, false },
                { true, true, true }
            };
            // 2
            _charMap['2'] = new bool[,] {
                { false, true, false },
                { true, false, true },
                { false, false, true },
                { false, true, false },
                { true, true, true }
            };
            // 3
            _charMap['3'] = new bool[,] {
                { true, true, false },
                { false, false, true },
                { false, true, false },
                { false, false, true },
                { true, true, false }
            };
            // 4
            _charMap['4'] = new bool[,] {
                { true, false, true },
                { true, false, true },
                { true, true, true },
                { false, false, true },
                { false, false, true }
            };
            // 5
            _charMap['5'] = new bool[,] {
                { true, true, true },
                { true, false, false },
                { true, true, false },
                { false, false, true },
                { true, true, false }
            };
            // 6
            _charMap['6'] = new bool[,] {
                { false, true, false },
                { true, false, false },
                { true, true, false },
                { true, false, true },
                { false, true, false }
            };
            // 7
            _charMap['7'] = new bool[,] {
                { true, true, true },
                { false, false, true },
                { false, true, false },
                { false, true, false },
                { false, true, false }
            };
            // 8
            _charMap['8'] = new bool[,] {
                { false, true, false },
                { true, false, true },
                { false, true, false },
                { true, false, true },
                { false, true, false }
            };
            // 9
            _charMap['9'] = new bool[,] {
                { false, true, false },
                { true, false, true },
                { false, true, true },
                { false, false, true },
                { false, true, false }
            };
            // Space
            _charMap[' '] = new bool[,] {
                { false, false, false },
                { false, false, false },
                { false, false, false },
                { false, false, false },
                { false, false, false }
            };
            // :
            _charMap[':'] = new bool[,] {
                { false, false, false },
                { false, true, false },
                { false, false, false },
                { false, true, false },
                { false, false, false }
            };
            // %
            _charMap['%'] = new bool[,] {
                { true, false, true },
                { false, false, true },
                { false, true, false },
                { false, false, true },
                { true, false, true }
            };
            // >
            _charMap['>'] = new bool[,] {
                { true, false, false },
                { false, true, false },
                { false, false, true },
                { false, true, false },
                { true, false, false }
            };
            // .
            _charMap['.'] = new bool[,] {
                { false, false, false },
                { false, false, false },
                { false, false, false },
                { false, false, false },
                { false, true, false }
            };
            // -
            _charMap['-'] = new bool[,] {
                { false, false, false },
                { false, false, false },
                { true, true, true },
                { false, false, false },
                { false, false, false }
            };
            // ,
            _charMap[','] = new bool[,] {
                { false, false, false },
                { false, false, false },
                { false, false, false },
                { false, false, false },
                { false, true, false } // Same as period for 3x5 simplicity, or could be bottom-left
            };
        }

        public static void DrawText(SpriteBatch spriteBatch, Texture2D texture, string text, Vector2 position, Color color, int scale = 1)
        {
            if (_charMap == null) Init();

            int xOffset = 0;
            text = text.ToUpper();

            foreach (char c in text)
            {
                if (_charMap.ContainsKey(c))
                {
                    bool[,] pixels = _charMap[c];
                    for (int y = 0; y < 5; y++)
                    {
                        for (int x = 0; x < 3; x++)
                        {
                            if (pixels[y, x])
                            {
                                spriteBatch.Draw(texture, new Rectangle((int)position.X + xOffset + x * scale, (int)position.Y + y * scale, scale, scale), color);
                            }
                        }
                    }
                    xOffset += 4 * scale; // 3 width + 1 spacing
                }
                else
                {
                    xOffset += 4 * scale;
                }
            }
        }
    }
}
