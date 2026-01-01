using System;
using System.Collections.Generic;

namespace HexGame.Units
{
    [Serializable]
    public class UnitStatDefinition
    {
        public string id;
        public string name;
    }

    [Serializable]
    public struct UnitStatValue
    {
        public string id;
        public int value;
    }

    [Serializable]
    public class UnitType
    {
        public string Name = "New Unit";
        // Core assets that are not numerical stats
        // (Visuals are handled by index or name reference usually, but here we might keep it simple or remove if using Manager defaults)
        
        // The dynamic stats. 
        // Changed to ID-based list for robustness against Schema reordering/renaming.
        public List<UnitStatValue> Stats = new List<UnitStatValue>();
    }
}