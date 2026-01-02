using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using HexGame;
using HexGame.Units;

namespace HexGame.Tests
{
    public class BattleBrothersMovementRulesTests
    {
        private GameObject managerGO;
        private GridVisualizationManager manager;
        private GameObject unitManagerGO;
        private UnitManager unitManager;
        private GameObject gameMasterGO;
        private GameMaster gameMaster;
        private BattleBrothersRuleset ruleset;
        private Grid grid;
        private UnitSet unitSet;

        [SetUp]
        public void SetUp()
        {
            managerGO = new GameObject("Manager");
            manager = managerGO.AddComponent<GridVisualizationManager>();
            typeof(GridVisualizationManager).GetProperty("Instance").SetValue(null, manager);

            unitManagerGO = new GameObject("UnitManager");
            unitManager = unitManagerGO.AddComponent<UnitManager>();
            unitManager.activeUnitSetPath = ""; // Prevent loading real data
            typeof(UnitManager).GetProperty("Instance").SetValue(null, unitManager);

            gameMasterGO = new GameObject("GameMaster");
            gameMaster = gameMasterGO.AddComponent<GameMaster>();
            var instanceProp = typeof(GameMaster).GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            instanceProp.SetValue(null, gameMaster);

            grid = new Grid(10, 10);
            manager.Grid = grid;

            ruleset = ScriptableObject.CreateInstance<BattleBrothersRuleset>();
            ruleset.movement = ScriptableObject.CreateInstance<MovementModule>();
            ruleset.combat = ScriptableObject.CreateInstance<CombatModule>();
            ruleset.tactical = ScriptableObject.CreateInstance<TacticalModule>();
            gameMaster.ruleset = ruleset;

            unitSet = new UnitSet();
            unitManager.ActiveUnitSet = unitSet;
            var type = new UnitType { id = "unit", Name = "Unit" };
            type.Stats = new List<UnitStatValue> { new UnitStatValue { id = "AP", value = 9 } };
            unitSet.units = new List<UnitType> { type };
        }

        [TearDown]
        public void TearDown()
        {
            typeof(UnitManager).GetProperty("Instance").SetValue(null, null);
            var instanceProp = typeof(GameMaster).GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            instanceProp.SetValue(null, null);

            Object.DestroyImmediate(managerGO);
            Object.DestroyImmediate(unitManagerGO);
            Object.DestroyImmediate(gameMasterGO);
            Object.DestroyImmediate(ruleset);
        }

        private Hex SetupHex(int q, int r, float elevation)
        {
            GameObject go = new GameObject($"Hex_{q}_{r}");
            Hex h = go.AddComponent<Hex>();
            HexData data = new HexData(q, r);
            data.Elevation = elevation;
            data.TerrainType = TerrainType.Plains;
            h.AssignData(data);
            grid.AddHex(data);
            return h;
        }

        [Test]
        public void GetMoveCost_ElevationDelta_AppliesPenalty()
        {
            Hex h1 = SetupHex(0, 0, 0);
            Hex h2 = SetupHex(1, 0, 1);
            GameObject uGO = new GameObject("Unit");
            Unit u = uGO.AddComponent<Unit>();
            u.Initialize("unit", 0);
            u.SetHex(h1);

            ruleset.movement.plainsCost = 2.0f;
            ruleset.movement.uphillPenalty = 10.0f;
            
            float cost = ruleset.GetPathfindingMoveCost(u, h1.Data, h2.Data);
            Assert.AreEqual(12.0f, cost);
            Object.DestroyImmediate(uGO);
        }

        [Test]
        public void GetMoveStopIndex_StopsBeforeEnemy()
        {
            Hex h1 = SetupHex(0, 0, 0);
            Hex h2 = SetupHex(1, 0, 0);
            Hex h3 = SetupHex(2, 0, 0);

            GameObject uGO = new GameObject("Unit");
            Unit u = uGO.AddComponent<Unit>();
            u.Initialize("unit", 0);
            u.SetHex(h1);

            GameObject enemyGO = new GameObject("Enemy");
            Unit enemy = enemyGO.AddComponent<Unit>();
            enemy.Initialize("unit", 1);
            enemy.SetHex(h3);

            List<HexData> path = new List<HexData> { h1.Data, h2.Data, h3.Data };
            int stopIndex = ruleset.GetMoveStopIndex(u, path);

            Assert.AreEqual(2, stopIndex, "Should stop at index 2 (h2) because h3 is occupied by enemy.");
            
            Object.DestroyImmediate(uGO);
            Object.DestroyImmediate(enemyGO);
        }

        [Test]
        public void GetMoveStopIndex_PassesThroughAlly()
        {
            Hex h1 = SetupHex(0, 0, 0);
            Hex h2 = SetupHex(1, 0, 0);
            Hex h3 = SetupHex(2, 0, 0);

            GameObject uGO = new GameObject("Unit");
            Unit u = uGO.AddComponent<Unit>();
            u.Initialize("unit", 0);
            u.SetHex(h1);

            GameObject allyGO = new GameObject("Ally");
            Unit ally = allyGO.AddComponent<Unit>();
            ally.Initialize("unit", 0);
            ally.SetHex(h2);

            List<HexData> path = new List<HexData> { h1.Data, h2.Data, h3.Data };
            int stopIndex = ruleset.GetMoveStopIndex(u, path);

            Assert.AreEqual(3, stopIndex, "Should be able to pass through ally.");
            
            Object.DestroyImmediate(uGO);
            Object.DestroyImmediate(allyGO);
        }

        [Test]
        public void GetMoveStopIndex_BudgetExceeded_TruncatesPath()
        {
            Hex h1 = SetupHex(0, 0, 0);
            Hex h2 = SetupHex(1, 0, 0);
            Hex h3 = SetupHex(2, 0, 0);

            GameObject uGO = new GameObject("Unit");
            Unit u = uGO.AddComponent<Unit>();
            u.Initialize("unit", 0);
            u.SetHex(h1);
            u.SetStat("AP", 2);

            ruleset.movement.plainsCost = 2.0f;

            List<HexData> path = new List<HexData> { h1.Data, h2.Data, h3.Data };
            int stopIndex = ruleset.GetMoveStopIndex(u, path);

            Assert.AreEqual(2, stopIndex, "Should truncate path at h2 (index 2) because budget is only 2.");
            
            Object.DestroyImmediate(uGO);
        }
    }
}
