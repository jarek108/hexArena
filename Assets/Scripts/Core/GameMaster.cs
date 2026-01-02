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

        // Delegating turn flow properties to the current ruleset
        public int roundNumber => ruleset != null ? ruleset.RoundNumber : 0;
        public Unit activeUnit => ruleset != null ? ruleset.ActiveUnit : null;
        public List<Unit> TurnQueue => ruleset != null ? ruleset.TurnQueue : new List<Unit>();

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
            ruleset?.StartCombat();
        }

        public void AdvanceTurn()
        {
            ruleset?.AdvanceTurn();
        }

        public void WaitCurrentTurn()
        {
            ruleset?.WaitTurn();
        }

        public void EndCurrentTurn()
        {
            AdvanceTurn();
        }

        public void EndCombat()
        {
            ruleset?.StopCombat();
        }
    }
}
