using UnityEngine;
using System.Collections.Generic;

namespace HexGame
{
    public abstract class Ruleset : ScriptableObject
    {
        [HideInInspector] public HexData currentSearchTarget;

        public virtual void OnStartPathfinding(HexData target, Unit unit)
        {
            currentSearchTarget = target;
        }

        public abstract float GetMoveCost(Unit unit, HexData fromHex, HexData toHex);
        public abstract bool OnEntry(Unit unit, HexData hex);
        public abstract bool OnDeparture(Unit unit, HexData hex);

        public abstract void OnAttack(Unit attacker, Unit target);
        public abstract void OnBeingAttacked(Unit attacker, Unit target);

        public abstract void ExecutePath(Unit unit, List<HexData> path, Hex targetHex);

        public virtual void OnUnitSelected(Unit unit) { }
        public virtual void OnUnitDeselected(Unit unit) { }

        public virtual void OnFinishPathfinding(Unit unit, List<HexData> path, bool success) { }
        public virtual void OnClearPathfindingVisuals() { }

        public virtual int GetMoveStopIndex(Unit unit, List<HexData> path)
        {
            return path != null ? path.Count : 0;
        }
    }
}
