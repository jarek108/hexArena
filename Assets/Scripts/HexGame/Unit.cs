using UnityEngine;
using System.Collections.Generic;
using HexGame.Units;

namespace HexGame
{
    [ExecuteAlways]
    public class Unit : MonoBehaviour
    {
        public UnitSet unitSet;
        [SerializeField] private int unitIndex;

        public UnitType UnitType 
        {
            get 
            {
                if (unitSet != null && unitIndex >= 0 && unitIndex < unitSet.units.Count)
                    return unitSet.units[unitIndex];
                return null;
            }
        }

        public string unitName => UnitType != null ? UnitType.Name : "Unknown Unit";

        public Hex CurrentHex { get; private set; }
        
        public Dictionary<string, int> Stats = new Dictionary<string, int>();
        private UnitVisualization currentView;

        private void Start()
        {
            if (unitSet != null) Initialize(unitSet, unitIndex);
        }

        public void Initialize(UnitSet set, int index)
        {
            unitSet = set;
            unitIndex = index;
            Stats.Clear();

            UnitType type = UnitType;
            if (type != null && type.Stats != null)
            {
                foreach (var stat in type.Stats)
                {
                    Stats[stat.id] = stat.value;
                }
            }
            
            // Note: In the new flow, the Unit component is added TO the visualization instance.
            currentView = GetComponent<UnitVisualization>();
            if (currentView != null)
            {
                currentView.Initialize(this);
            }
        }

        public void SetHex(Hex hex)
        {
            if (CurrentHex != null) CurrentHex.Unit = null;
            CurrentHex = hex;
            if (CurrentHex != null)
            {
                CurrentHex.Unit = this;
                Vector3 pos = CurrentHex.transform.position;
                if (currentView != null) pos.y += currentView.yOffset;
                transform.position = pos; 
            }
        }
        
        public int GetStat(string statName, int defaultValue = 0)
        {
            if (Stats.TryGetValue(statName, out int val)) return val;
            return defaultValue;
        }
    }
}