using UnityEngine;
using System.Collections.Generic;

namespace HexGame.Units
{
    [ExecuteAlways]
    public class UnitManager : MonoBehaviour
    {
        public UnitSet activeUnitSet;
        public UnitVisualization defaultUnitVisualization;

        public List<Unit> units = new List<Unit>();

        private void OnEnable()
        {
            units.Clear();
            var children = GetComponentsInChildren<Unit>();
            foreach (var unit in children)
            {
                RegisterUnit(unit);
            }
        }

        public void RegisterUnit(Unit unit)
        {
            if (unit == null) return;
            if (!units.Contains(unit))
            {
                units.Add(unit);
            }
        }

        public void UnregisterUnit(Unit unit)
        {
            if (units.Contains(unit))
            {
                units.Remove(unit);
            }
        }

        public void CreateUnit()
        {
            GameObject go = new GameObject($"Unit {units.Count + 1}");
            go.transform.SetParent(this.transform);
            Unit unit = go.AddComponent<Unit>();
            RegisterUnit(unit);
        }
    }
}