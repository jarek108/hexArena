using UnityEngine;
using System.Collections.Generic;
using HexGame.Units;

namespace HexGame
{
    [System.Serializable]
    public struct PotentialHit
    {
        public Unit target;
        public float min;
        public float max;
        public int drawIndex;
        public float damageMultiplier;
        public string logInfo;

        public PotentialHit(Unit target, float min, float max, int drawIndex, float damageMultiplier = 1f, string logInfo = "")
        {
            this.target = target;
            this.min = min;
            this.max = max;
            this.drawIndex = drawIndex;
            this.damageMultiplier = damageMultiplier;
            this.logInfo = logInfo;
        }
    }

    public struct MoveVerification
    {
        public bool isValid;
        public string reason;

        public static MoveVerification Success() => new MoveVerification { isValid = true, reason = "" };
        public static MoveVerification Failure(string reason) => new MoveVerification { isValid = false, reason = reason };
    }

    public abstract class Ruleset : ScriptableObject 
    {
        public string unitSchema;

        [Header("Flow Controls")]
        public bool ignoreAPs = false;
        public bool ignoreFatigue = false;
        public bool ignoreMoveOrder = false;

        [HideInInspector] public HexData currentSearchTarget;
        [HideInInspector] public List<HexData> currentSearchTargets = new List<HexData>();

        public virtual void OnStartPathfinding(HexData target, Unit unit)
        {
            currentSearchTarget = target;
            currentSearchTargets.Clear();
            currentSearchTargets.Add(target);
        }

        public virtual void OnStartPathfinding(IEnumerable<HexData> targets, Unit unit)
        {
            Debug.Log("pathing");
            currentSearchTargets = new List<HexData>(targets);
            currentSearchTarget = currentSearchTargets.Count > 0 ? currentSearchTargets[0] : null;
        }

        /// <summary>
        /// Returns the tactical weight of a move for pathfinding. 
        /// Should ONLY return Infinity for physical blockers, not resource constraints.
        /// </summary>
        public abstract float GetPathfindingMoveCost(Unit unit, HexData fromHex, HexData toHex);

        /// <summary>
        /// Attempts to move a unit from one hex to another. 
        /// Validates preconditions (AP, Occupation) and executes interruption logic (ZoC attacks, Traps).
        /// Returns Success if the move proceeds, or Failure if blocked/interrupted.
        /// </summary>
        public abstract MoveVerification TryMoveStep(Unit unit, HexData fromHex, HexData toHex);

        /// <summary>
        /// Executes the side effects of a move (deducting resources, updating grid footprints).
        /// </summary>
        public abstract void PerformMove(Unit unit, HexData fromHex, HexData toHex);

        public abstract void OnAttacked(Unit attacker, Unit target);
        public abstract void OnHit(Unit attacker, Unit target, float damage);
        public abstract void OnDie(Unit unit);

        public abstract void ExecutePath(Unit unit, List<HexData> path, Hex targetHex, System.Action onComplete = null);

        public virtual void OnUnitSelected(Unit unit) { }
        public virtual void OnUnitDeselected(Unit unit) { }

        // Turn Flow Hooks
        public virtual void OnRoundStart(IEnumerable<Unit> allUnits) { }
        public virtual void OnTurnStart(Unit unit) { }
        public virtual void OnTurnEnd(Unit unit) { }
        public virtual int GetTurnPriority(Unit unit) { return 0; }

        // Turn Flow Interface for GameMaster
        public virtual int RoundNumber => 0;
        public virtual Unit ActiveUnit => null;
        public virtual List<Unit> TurnQueue => new List<Unit>();

        public virtual void StartCombat() { }
        public virtual void AdvanceTurn() { }
        public virtual void WaitTurn() { }
        public virtual void StopCombat() { }

        public virtual List<PotentialHit> GetPotentialHits(Unit attacker, Unit target, HexData fromHex = null) 
        { 
            return new List<PotentialHit>(); 
        }

        public virtual void OnFinishPathfinding(Unit unit, List<HexData> path, bool success) { }
        public virtual void OnClearPathfindingVisuals() { }

        public virtual int GetMoveStopIndex(Unit unit, List<HexData> path)
        {
            return path != null ? path.Count : 0;
        }

        /// <summary>
        /// Returns a list of hexes from which the attacker can strike the target.
        /// By default returns the target's own hex (direct targeting).
        /// </summary>
        public virtual List<HexData> GetValidAttackPositions(Unit attacker, Unit target)
        {
            var results = new List<HexData>();
            if (target != null && target.CurrentHex != null)
            {
                results.Add(target.CurrentHex.Data);
            }
            return results;
        }
    }
}