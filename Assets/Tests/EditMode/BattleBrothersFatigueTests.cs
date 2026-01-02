using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using HexGame;
using HexGame.Units;

namespace HexGame.Tests
{
    public class BattleBrothersFatigueTests
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
            unitManager.activeUnitSetPath = "";
            typeof(UnitManager).GetProperty("Instance").SetValue(null, unitManager);

            gameMasterGO = new GameObject("GameMaster");
            gameMaster = gameMasterGO.AddComponent<GameMaster>();
            var instanceProp = typeof(GameMaster).GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            instanceProp.SetValue(null, gameMaster);

            grid = new Grid(10, 10);
            manager.Grid = grid;

            ruleset = ScriptableObject.CreateInstance<BattleBrothersRuleset>();
            ruleset.movement = ScriptableObject.CreateInstance<MovementModule>();
            ruleset.movement.plainsCost = 2; // Cost of 2
            gameMaster.ruleset = ruleset;

            unitSet = new UnitSet();
            unitManager.ActiveUnitSet = unitSet;
            var type = new UnitType { id = "unit", Name = "Unit" };
            // Define Max Fatigue as 100 in the prototype
            type.Stats = new List<UnitStatValue> { 
                new UnitStatValue { id = "FAT", value = 100 },
                new UnitStatValue { id = "AP", value = 9 }
            };
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

        private Hex SetupHex(int q, int r)
        {
            GameObject go = new GameObject($"Hex_{q}_{r}");
            Hex h = go.AddComponent<Hex>();
            HexData data = new HexData(q, r);
            data.TerrainType = TerrainType.Plains;
            h.AssignData(data);
            grid.AddHex(data);
            return h;
        }

        [Test]
        public void TryMoveStep_WithFreshUnit_ShouldNotFailFatigue()
        {
            Hex h1 = SetupHex(0, 0);
            Hex h2 = SetupHex(1, 0);
            
            GameObject uGO = new GameObject("Unit");
            Unit u = uGO.AddComponent<Unit>();
            u.Initialize("unit", 0);
            u.SetHex(h1);

            // Verify current behavior (bug): Initializing copies FAT=100 to Current Stats.
            // But we want to Assert that MoveVerification SUCCEEDS, which means 
            // the logic should treat current fatigue as 0 (or at least low enough).
            
            MoveVerification result = ruleset.TryMoveStep(u, h1.Data, h2.Data);
            
            Assert.IsTrue(result.isValid, $"Move should be valid but failed with: {result.reason}");
            
            Object.DestroyImmediate(uGO);
            
            Object.DestroyImmediate(uGO);
        }
    }
}
