using System;
using System.Collections.Generic;

namespace HexGame.Units
{
    [Serializable]
    public class UnitType
    {
        public string Name = "New Unit";
        // Core assets that are not numerical stats
        // (Visuals are handled by index or name reference usually, but here we might keep it simple or remove if using Manager defaults)
        
        // The dynamic stats. 
        // We use a parallel array/list approach where the index corresponds to the UnitManager's characteristic list.
        public List<int> Stats = new List<int>();
    }
}