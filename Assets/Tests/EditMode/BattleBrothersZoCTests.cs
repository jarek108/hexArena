using System.Collections.Generic;
using HexGame;
using HexGame.Units;
using NUnit.Framework;
using UnityEngine;

namespace HexGame.Tests
{
    [TestFixture]
    public class BattleBrothersZoCTests
    {
        private GameObject managerGO;
        private GridVisualizationManager manager;
        private GameObject gameMasterGO;
        private GameMaster gameMaster;
        private BattleBrothersRuleset ruleset;
        private Grid grid;
        
        private GameObject hexStartGO;
        private Hex hexStart;
        private GameObject hexEndGO;
        private Hex hexEnd;

        private GameObject unitGO;
        private Unit unit;
        private UnitSet unitSet;

        [SetUp]
        public void SetUp()
        {
            managerGO = new GameObject("GridVisualizationManager");
            manager = managerGO.AddComponent<GridVisualizationManager>();
            
            gameMasterGO = new GameObject("GameMaster");
            gameMaster = gameMasterGO.AddComponent<GameMaster>();
            
            ruleset = ScriptableObject.CreateInstance<BattleBrothersRuleset>();
            ruleset.plainsCost = 2.0f;
            ruleset.zocPenalty = 50.0f;
            gameMaster.ruleset = ruleset;

            grid = new Grid(10, 10);
            manager.Grid = grid;

            // Start Hex (0,0)
            hexStartGO = new GameObject("HexStart");
            hexStart = hexStartGO.AddComponent<Hex>();
            HexData data1 = new HexData(0, 0);
            data1.TerrainType = TerrainType.Plains;
            hexStart.AssignData(data1);
            grid.AddHex(data1);

            // End Hex (1, -1) - Neighbor
            hexEndGO = new GameObject("HexEnd");
            hexEnd = hexEndGO.AddComponent<Hex>();
            HexData data2 = new HexData(1, -1);
            data2.TerrainType = TerrainType.Plains;
            hexEnd.AssignData(data2);
            grid.AddHex(data2);

            unitSet = ScriptableObject.CreateInstance<UnitSet>();
            unitGO = new GameObject("Unit");
            unit = unitGO.AddComponent<Unit>();
            
            var type = new UnitType { Name = "TestUnit" };
            unitSet.units = new List<UnitType> { type };
            unit.Initialize(unitSet, 0, 0); // Team 0
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(managerGO);
            Object.DestroyImmediate(gameMasterGO);
            Object.DestroyImmediate(hexStartGO);
            Object.DestroyImmediate(hexEndGO);
            Object.DestroyImmediate(unitGO);
            Object.DestroyImmediate(ruleset);
            Object.DestroyImmediate(unitSet);
        }

        [Test]
        public void GetMoveCost_NoZoC_ReturnsBaseCost()
        {
            float cost = ruleset.GetMoveCost(unit, hexStart.Data, hexEnd.Data);
            Assert.AreEqual(2.0f, cost, "Cost should be base plains cost.");
        }

        [Test]
        public void GetMoveCost_EnemyZoC_AppliesPenalty()
        {
            // Enemy Team 1 ZoC
            hexEnd.Data.AddState("ZoC_1");
            
            float cost = ruleset.GetMoveCost(unit, hexStart.Data, hexEnd.Data);
            Assert.AreEqual(52.0f, cost, "Cost should be base (2) + penalty (50).");
        }

        [Test]
        public void GetMoveCost_FriendlyZoC_NoPenalty()
        {
            // Friendly Team 0 ZoC
            hexEnd.Data.AddState("ZoC_0");

            float cost = ruleset.GetMoveCost(unit, hexStart.Data, hexEnd.Data);
            Assert.AreEqual(2.0f, cost, "Friendly ZoC should not penalize movement.");
        }

        [Test]
        public void GetMoveCost_NoUnit_IgnoresZoC()
        {
            // Enemy Team 1 ZoC
            hexEnd.Data.AddState("ZoC_1");

            // Passing null as unit (Pathfinding without unit)
            float cost = ruleset.GetMoveCost(null, hexStart.Data, hexEnd.Data);
            Assert.AreEqual(2.0f, cost, "Null unit should ignore ZoC penalty.");
        }
    }
}