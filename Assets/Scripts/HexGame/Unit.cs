using UnityEngine;
using System.Collections.Generic;
using HexGame.Units;

namespace HexGame
{
    [ExecuteAlways]
    public class Unit : MonoBehaviour
    {
        [SerializeField] private UnitType unitType;
        public UnitType UnitType => unitType;

        public Hex CurrentHex { get; private set; }
        
        public Dictionary<string, int> Stats = new Dictionary<string, int>();
        private UnitVisualization currentView;

        private void OnEnable()
        {
            var manager = FindFirstObjectByType<UnitManager>();
            if (manager != null) manager.RegisterUnit(this);
        }

        private void OnDisable()
        {
            var manager = FindFirstObjectByType<UnitManager>();
            if (manager != null) manager.UnregisterUnit(this);
        }

        private void Start()
        {
            if (unitType != null) Initialize(unitType);
        }

        public void Initialize(UnitType type)
        {
            unitType = type;
            Stats.Clear();

            var manager = FindFirstObjectByType<UnitManager>();
            if (manager == null) 
            {
                Debug.LogWarning("Unit: Initialize failed - UnitManager not found.");
                return;
            }

            // Load stats using the active set's schema
            if (unitType != null && manager.activeUnitSet != null && manager.activeUnitSet.schema != null)
            {
                var definitions = manager.activeUnitSet.schema.definitions;
                foreach (var def in definitions)
                {
                    string statName = def.id;
                    int val = 0;
                    if (unitType.Stats != null)
                    {
                        int index = unitType.Stats.FindIndex(s => s.id == statName);
                        if (index != -1) val = unitType.Stats[index].value;
                    }
                    Stats[statName] = val;
                }
            }
            
            if (currentView != null) Destroy(currentView.gameObject);

            if (manager.defaultUnitVisualization != null)
            {
                currentView = Instantiate(manager.defaultUnitVisualization, transform);
                currentView.transform.localPosition = Vector3.zero;
                currentView.transform.localRotation = Quaternion.identity;
                currentView.Initialize(this);
            }
            else
            {
                Debug.LogWarning($"Unit {name} could not find default visualization in UnitManager!");
            }
        }

        public void SetHex(Hex hex)
        {
            if (CurrentHex != null) CurrentHex.Unit = null;
            CurrentHex = hex;
            if (CurrentHex != null)
            {
                CurrentHex.Unit = this;
                transform.position = CurrentHex.transform.position; 
            }
        }
        
        public int GetStat(string statName, int defaultValue = 0)
        {
            if (Stats.TryGetValue(statName, out int val)) return val;
            return defaultValue;
        }
    }
}