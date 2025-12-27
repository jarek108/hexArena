using UnityEngine;
using System.Collections.Generic;
using HexGame.Units;

namespace HexGame
{
    [ExecuteAlways]
    public class Unit : MonoBehaviour
    {
        public int Id => gameObject.GetInstanceID();

        public UnitSet unitSet;
        [SerializeField] private int unitIndex;
        public int teamId;

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
        [SerializeField, HideInInspector] private int lastQ;
        [SerializeField, HideInInspector] private int lastR;
        
        public Dictionary<string, int> Stats = new Dictionary<string, int>();
        private UnitVisualization currentView;

        private void Start()
        {
            if (unitSet != null) Initialize(unitSet, unitIndex, teamId);
        }

        public void Initialize(UnitSet set, int index, int team)
        {
            unitSet = set;
            unitIndex = index;
            teamId = team;
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
            // 1. Departure from old hex
            if (CurrentHex != null)
            {
                GameMaster.Instance?.ruleset?.OnDeparture(this, CurrentHex.Data);
                // Only clear reference if we are actually changing to a new hex or null
                if (CurrentHex != hex) CurrentHex.Unit = null;
            }

            if (hex == null)
            {
                CurrentHex = null;
                return;
            }

            // 2. Entry to new hex
            CurrentHex = hex;
            CurrentHex.Unit = this;
            lastQ = hex.Q;
            lastR = hex.R;
            
            GameMaster.Instance?.ruleset?.OnEntry(this, hex.Data);
            
            UpdateVisualPosition();
        }

        public void UpdateVisualPosition()
        {
            if (CurrentHex == null) return;
            
            Vector3 pos = CurrentHex.transform.position;
            if (currentView != null) pos.y += currentView.yOffset;
            else
            {
                // Fallback: try to find view if it wasn't picked up
                currentView = GetComponent<UnitVisualization>();
                if (currentView != null) pos.y += currentView.yOffset;
            }
            transform.position = pos;
        }
        
        public int GetStat(string statName, int defaultValue = 0)
        {
            if (Stats.TryGetValue(statName, out int val)) return val;
            return defaultValue;
        }

        public UnitSaveData GetSaveData()
        {
            return new UnitSaveData
            {
                q = lastQ,
                r = lastR,
                unitIndex = this.unitIndex,
                teamId = this.teamId
            };
        }
    }

    [System.Serializable]
    public class UnitSaveData
    {
        public int q;
        public int r;
        public int unitIndex;
        public int teamId;
    }
}