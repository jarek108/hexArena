using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HexGame
{
    [Serializable]
    public class HexData
    {
        public int Q;
        public int R;
        public int S;

        public float GetMovementCost()
        {
            return 1f;
        }
        
        private float _elevation;
        public float Elevation
        {
            get => _elevation;
            set
            {
                if (!Mathf.Approximately(_elevation, value))
                {
                    _elevation = value;
                    OnElevationChanged?.Invoke();
                }
            }
        }
        
        private TerrainType _terrainType;
        public TerrainType TerrainType
        {
            get => _terrainType;
            set
            {
                if (_terrainType != value)
                {
                    _terrainType = value;
                    OnTerrainChanged?.Invoke();
                }
            }
        }
        
        
        private List<Unit> _units = new List<Unit>();
        public List<Unit> Units => _units;

        /// <summary>
        /// Compatibility property. 
        /// Get: Returns the first unit in the list or null.
        /// Set: Clears the list and adds the assigned unit (if not null).
        /// </summary>
        public Unit Unit
        {
            get => _units.Count > 0 ? _units[0] : null;
            set
            {
                _units.Clear();
                if (value != null)
                {
                    _units.Add(value);
                }
            }
        }

        public void AddUnit(Unit unit)
        {
            if (unit != null && !_units.Contains(unit))
            {
                _units.Add(unit);
            }
        }

        public void RemoveUnit(Unit unit)
        {
            if (unit != null)
            {
                _units.Remove(unit);
            }
        }

        private HashSet<string> _states;
        public HashSet<string> States 
        { 
            get
            {
                if (_states == null) _states = new HashSet<string>();
                return _states;
            }
        }

        public event Action OnStateChanged;
        public event Action OnTerrainChanged;
        public event Action OnElevationChanged;
        
        public HexData(int q, int r)
        {
            this.Q = q;
            this.R = r;
            this.S = -q - r;
        }

        public void AddState(string state)
        {
            if (States.Add(state))
            {
                OnStateChanged?.Invoke();
            }
        }

        public void RemoveState(string state)
        {
            if (States.Remove(state))
            {
                OnStateChanged?.Invoke();
            }
        }

        public void UpdateStates(IEnumerable<string> toAdd, IEnumerable<string> toRemove)
        {
            bool changed = false;

            if (toRemove != null)
            {
                foreach (var state in toRemove)
                {
                    if (States.Remove(state)) changed = true;
                }
            }

            if (toAdd != null)
            {
                foreach (var state in toAdd)
                {
                    if (States.Add(state)) changed = true;
                }
            }

            if (changed)
            {
                OnStateChanged?.Invoke();
            }
        }
    }
}