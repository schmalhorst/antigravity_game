using System.Text.Json.Serialization;

using System.Collections.Generic;

namespace AntigravityMoon
{
    public class CropData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Dictionary<string, int> Costs { get; set; } = new Dictionary<string, int>();
        public float GrowthTime { get; set; } = 10f;
        public string ColorHex { get; set; } = "#FFFFFF";
        public int HungerBuffReward { get; set; } = 0;
        public int OxygenBuffReward { get; set; } = 0;
        public float BuffDuration { get; set; } = 0f;
        public Microsoft.Xna.Framework.Color GetFallbackColor()
        {
            if (string.IsNullOrEmpty(ColorHex))
                return Microsoft.Xna.Framework.Color.White;

            try
            {
                var hexPattern = ColorHex.StartsWith("#") ? ColorHex.Substring(1) : ColorHex;
                if (hexPattern.Length == 6)
                {
                    byte r = byte.Parse(hexPattern.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                    byte g = byte.Parse(hexPattern.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                    byte b = byte.Parse(hexPattern.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                    return new Microsoft.Xna.Framework.Color(r, g, b);
                }
            }
            catch
            {
                // Fallback on error
            }
            return Microsoft.Xna.Framework.Color.White;
        }
    }
}
