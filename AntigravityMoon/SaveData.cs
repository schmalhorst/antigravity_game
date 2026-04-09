using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace AntigravityMoon
{
    [Serializable]
    public class SaveData
    {
        public float PlayerX { get; set; }
        public float PlayerY { get; set; }
        public float Oxygen { get; set; }
        public float Hunger { get; set; }
        public int BackpackLevel { get; set; }
        public int SuitLevel { get; set; }
        public List<InventoryItemData> Inventory { get; set; } = new List<InventoryItemData>();
        public List<StructureData> Structures { get; set; } = new List<StructureData>();
        public List<ExploredChunkData> Explored { get; set; } = new List<ExploredChunkData>();

        public struct ExploredChunkData
        {
            public int X { get; set; }
            public int Y { get; set; }
            public List<bool> Tiles { get; set; }
        }

        public struct InventoryItemData
        {
            public string Name { get; set; }
            public int Count { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
        }

        public struct StructureData
        {
            public string Type { get; set; }
            public float X { get; set; }
            public float Y { get; set; }
            public int RepairStage { get; set; }
            public Dictionary<string, int> ContributedMaterials { get; set; }
        }
    }
}
