using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HexGame;
using HexGame.Tools;
using HexGame.Units;

namespace HexGame.Tests
{
    public class BattleBrothersAoATests
    {
        private GameObject managerGO;
        private GridVisualizationManager manager;
        private GameObject unitManagerGO;
        private UnitManager unitManager;
        private GameObject gameMasterGO;
        private GameMaster gameMaster;
        private BattleBrothersRuleset ruleset;
        private Grid grid;
        private Unit unit;
        private UnitSet unitSet;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            managerGO = TestHelper.CreateTestManager();
            manager = managerGO.GetComponent<GridVisualizationManager>();
            typeof(GridVisualizationManager).GetProperty("Instance").SetValue(null, manager);

            unitManagerGO = new GameObject("UnitManager");
            unitManager = unitManagerGO.AddComponent<UnitManager>();
            unitManager.activeUnitSetPath = ""; // Prevent loading real data
            typeof(UnitManager).GetProperty("Instance").SetValue(null, unitManager);

            gameMasterGO = new GameObject("GameMaster");
            gameMaster = gameMasterGO.AddComponent<GameMaster>();
            ruleset = ScriptableObject.CreateInstance<BattleBrothersRuleset>();
            ruleset.movement = ScriptableObject.CreateInstance<MovementModule>();
            ruleset.combat = ScriptableObject.CreateInstance<CombatModule>();
            ruleset.tactical = ScriptableObject.CreateInstance<TacticalModule>();
            gameMaster.ruleset = ruleset;

            grid = new Grid(10, 10);
            manager.Grid = grid;

            unitSet = ScriptableObject.CreateInstance<UnitSet>();
            unitManager.ActiveUnitSet = unitSet;

            GameObject unitGO = new GameObject("Unit");
            unit = unitGO.AddComponent<Unit>();
            unitGO.AddComponent<SimpleUnitVisualization>();

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            typeof(UnitManager).GetProperty("Instance").SetValue(null, null);
            typeof(GridVisualizationManager).GetProperty("Instance").SetValue(null, null);
            Object.DestroyImmediate(managerGO);
            Object.DestroyImmediate(unitManagerGO);
            Object.DestroyImmediate(gameMasterGO);
            Object.DestroyImmediate(ruleset);
            Object.DestroyImmediate(unitSet);
            yield return null;
        }

        private void SetupUnit(int teamId, int mrng, int rrng)
        {
            var type = new UnitType { Name = "Test" };
            type.Stats = new List<UnitStatValue>
            {
                new UnitStatValue { id = "MAT", value = mrng > 0 ? 50 : 0 },
                new UnitStatValue { id = "RAT", value = rrng > 0 ? 50 : 0 },
                new UnitStatValue { id = "RNG", value = Mathf.Max(mrng, rrng) }
            };
            unitSet.units = new List<UnitType> { type };
            unitManager.ActiveUnitSet = unitSet;
            unit.Initialize(0, teamId);
            unit.Stats["CAP"] = 100;
            unit.Stats["MFAT"] = 100;
            unit.Stats["CFAT"] = 0;
        }

        private HexData CreateHex(int q, int r, float elevation = 0)
        {
            HexData data = new HexData(q, r);
            data.Elevation = elevation;
            grid.AddHex(data);
            
            GameObject go = new GameObject($"Hex_{q}_{r}");
            go.transform.SetParent(manager.transform);
            Hex hexView = go.AddComponent<Hex>();
            hexView.AssignData(data);
            
            return data;
        }

        [UnityTest]
        public IEnumerator OnFinishPathfinding_AddsAoAState()
        {
            SetupUnit(0, 1, 0); // Team 0, Melee range 1
            HexData start = CreateHex(0, 0);
            HexData stop = CreateHex(1, 0);
            HexData target = CreateHex(2, 0);

            List<HexData> path = new List<HexData> { start, stop };
            
            ruleset.OnFinishPathfinding(unit, path, true);
            
            Assert.IsTrue(target.States.Contains($"AoA0_{unit.Id}"), "Neighbor hex should have AoA state");
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator OnClearPathfindingVisuals_RemovesAoAState()
        {
            SetupUnit(0, 1, 0);
            HexData start = CreateHex(0, 0);
            HexData stop = CreateHex(1, 0);
            HexData target = CreateHex(1, -1); // neighbor of stop

            List<HexData> path = new List<HexData> { start, stop };
            ruleset.OnFinishPathfinding(unit, path, true);
            Assert.IsTrue(target.States.Contains($"AoA0_{unit.Id}"));

            ruleset.OnClearPathfindingVisuals();
            
            Assert.IsFalse(target.States.Contains($"AoA0_{unit.Id}"), "AoA state should be removed after clearing");
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator MeleeAoA_RespectsElevation()
        {
            SetupUnit(0, 1, 0);
            ruleset.movement.maxElevationDelta = 1.0f;
            
            HexData stop = CreateHex(0, 0, 0);
            HexData low = CreateHex(1, 0, 0);
            HexData high = CreateHex(1, -1, 2.0f); // Guaranteed neighbor of (0,0)

            List<HexData> path = new List<HexData> { stop };
            ruleset.OnFinishPathfinding(unit, path, true);

            Assert.IsTrue(low.States.Contains($"AoA0_{unit.Id}"), "Same level hex should be in AoA");
            Assert.IsFalse(high.States.Contains($"AoA0_{unit.Id}"), "Too high hex should NOT be in AoA");
            
            yield return null;
        }
    }
}
