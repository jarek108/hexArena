using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace HexGame
{
    [ExecuteAlways]
    public class GameMaster : MonoBehaviour
    {
        public static GameMaster Instance { get; private set; }

        public Ruleset ruleset;

        [Header("Turn Flow")]
        public int roundNumber = 0;
        public Unit activeUnit;
        [SerializeField] private List<Unit> turnQueue = new List<Unit>();
        [SerializeField] private List<Unit> waitingQueue = new List<Unit>();
        private HashSet<int> unitsWhoWaitedThisRound = new HashSet<int>();

        public List<Unit> TurnQueue => turnQueue;
        public List<Unit> WaitingQueue => waitingQueue;

        private void OnEnable()
        {
            Instance = this;
        }

        private void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        public void StartNewRound()
        {
            roundNumber++;
            unitsWhoWaitedThisRound.Clear();
            waitingQueue.Clear();
            
            // 1. Gather all units
            // Prefer UnitManager if available to ensure we only get units in the active layout
            List<Unit> validUnits = new List<Unit>();
            if (UnitManager.Instance != null)
            {
                validUnits.AddRange(UnitManager.Instance.GetComponentsInChildren<Unit>());
            }
            else
            {
                validUnits.AddRange(Object.FindObjectsByType<Unit>(FindObjectsSortMode.None));
            }

            // Filter for only active units that aren't pending destruction
            var allUnits = validUnits.Where(u => u != null && u.gameObject.activeInHierarchy).ToList();
            
            // 2. Ruleset hook for round start (e.g. fatigue recovery)
            ruleset?.OnRoundStart(allUnits);

            // 3. Sort units by priority (Initiative)
            turnQueue = allUnits
                .OrderByDescending(u => ruleset?.GetTurnPriority(u) ?? 0)
                .ToList();

            // 4. Start the first turn
            AdvanceTurn();
        }

        public void AdvanceTurn()
        {
            // End turn hook for previous unit
            if (activeUnit != null)
            {
                ruleset?.OnTurnEnd(activeUnit);
            }

            // Cleanup queues (remove null units that might have died)
            turnQueue.RemoveAll(u => u == null);
            waitingQueue.RemoveAll(u => u == null);

            if (turnQueue.Count == 0)
            {
                if (waitingQueue.Count > 0)
                {
                    // Process waiting queue in initiative order
                    turnQueue = waitingQueue
                        .OrderByDescending(u => ruleset?.GetTurnPriority(u) ?? 0)
                        .ToList();
                    waitingQueue.Clear();
                }
                else
                {
                    StartNewRound();
                    return;
                }
            }

            // Pop next unit
            activeUnit = turnQueue[0];
            turnQueue.RemoveAt(0);

            // Start turn hook (e.g. AP restoration)
            ruleset?.OnTurnStart(activeUnit);

            Debug.Log($"[GameMaster] Round {roundNumber} - Starting turn for: {activeUnit.UnitName}");
        }

        public void WaitCurrentTurn()
        {
            if (activeUnit == null) return;

            // Can only wait once per round
            if (unitsWhoWaitedThisRound.Contains(activeUnit.Id))
            {
                Debug.Log($"[GameMaster] {activeUnit.UnitName} has already waited this round.");
                EndCurrentTurn();
                return;
            }

            unitsWhoWaitedThisRound.Add(activeUnit.Id);
            waitingQueue.Add(activeUnit);
            
            // We don't call OnTurnEnd here because the turn isn't strictly "over" yet, 
            // but we need to transition to the next unit in the current queue.
            
            activeUnit = null; // No active unit during transition
            AdvanceTurn();
        }

        public void EndCurrentTurn()
        {
            AdvanceTurn();
        }
    }
}
