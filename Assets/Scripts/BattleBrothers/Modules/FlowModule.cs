using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using HexGame.Units;

namespace HexGame
{
    [CreateAssetMenu(fileName = "FlowModule", menuName = "HexGame/BattleBrothers/Modules/Flow")]
    public class FlowModule : ScriptableObject
    {
        public int roundNumber = 0;
        [HideInInspector] public Unit activeUnit;
        [SerializeField] private List<Unit> turnQueue = new List<Unit>();
        private HashSet<int> unitsWhoWaitedThisRound = new HashSet<int>();

        public List<Unit> TurnQueue 
        {
            get
            {
                var displayQueue = new List<Unit>();
                if (activeUnit != null) displayQueue.Add(activeUnit);
                displayQueue.AddRange(turnQueue);
                return displayQueue;
            }
        }

        public void StartNewRound(BattleBrothersRuleset ruleset)
        {
            if (ruleset != null)
            {
                ruleset.ignoreAPs = false;
                ruleset.ignoreFatigue = false;
                ruleset.ignoreMoveOrder = false;
            }

            roundNumber++;
            unitsWhoWaitedThisRound.Clear();
            
            List<Unit> validUnits = new List<Unit>();
            if (UnitManager.Instance != null)
            {
                validUnits.AddRange(UnitManager.Instance.GetComponentsInChildren<Unit>());
            }
            else
            {
                validUnits.AddRange(Object.FindObjectsByType<Unit>(FindObjectsSortMode.None));
            }

            var allUnits = validUnits.Where(u => u != null && u.gameObject.activeInHierarchy).ToList();
            
            ruleset?.OnRoundStart(allUnits);

            turnQueue = allUnits
                .OrderByDescending(u => ruleset?.GetTurnPriority(u) ?? 0)
                .ToList();

            AdvanceTurn(ruleset);
        }

        public void AdvanceTurn(BattleBrothersRuleset ruleset)
        {
            if (activeUnit != null)
            {
                activeUnit.RemoveOwnedHexStatesByPrefix("Active");
                ruleset?.OnTurnEnd(activeUnit);
            }

            turnQueue.RemoveAll(u => u == null);

            if (turnQueue.Count == 0)
            {
                StartNewRound(ruleset);
                return;
            }

            activeUnit = turnQueue[0];
            turnQueue.RemoveAt(0);

            ruleset?.OnTurnStart(activeUnit);

            Debug.Log($"[FlowModule] Round {roundNumber} - Starting turn for: {activeUnit.UnitName}");
        }

        public void WaitCurrentTurn(BattleBrothersRuleset ruleset)
        {
            if (activeUnit == null) return;

            if (unitsWhoWaitedThisRound.Contains(activeUnit.Id))
            {
                Debug.Log($"[FlowModule] {activeUnit.UnitName} has already waited this round.");
                AdvanceTurn(ruleset);
                return;
            }

            unitsWhoWaitedThisRound.Add(activeUnit.Id);
            turnQueue.Add(activeUnit);

            turnQueue = turnQueue
                .OrderByDescending(u => {
                    int prio = ruleset?.GetTurnPriority(u) ?? 0;
                    if (unitsWhoWaitedThisRound.Contains(u.Id)) prio -= 100;
                    return prio;
                })
                .ToList();
            
            activeUnit.RemoveOwnedHexStatesByPrefix("Active");
            activeUnit = null; 
            AdvanceTurn(ruleset);
        }

        public void EndCombat()
        {
            if (activeUnit != null)
            {
                activeUnit.RemoveOwnedHexStatesByPrefix("Active");
            }
            activeUnit = null;
            turnQueue.Clear();
            unitsWhoWaitedThisRound.Clear();
            roundNumber = 0;
            Debug.Log("[FlowModule] Combat ended.");
        }
    }
}
