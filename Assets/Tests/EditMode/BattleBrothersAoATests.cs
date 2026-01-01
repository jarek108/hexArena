using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HexGame;
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
        private UnitSet unitSet;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            managerGO = TestHelper.CreateTestManager();
            manager = managerGO.GetComponent<GridVisualizationManager>();
            
            // Force instance singleton
            var instanceProp = typeof(GridVisualizationManager).GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            instanceProp.SetValue(null, manager);

            unitManagerGO = new GameObject("UnitManager");
            unitManager = unitManagerGO.AddComponent<UnitManager>();
            unitManager.activeUnitSetPath = ""; // Prevent loading real data
            
            var umInstanceProp = typeof(UnitManager).GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            umInstanceProp.SetValue(null, unitManager);

            gameMasterGO = new GameObject("GameMaster");
            gameMaster = gameMasterGO.AddComponent<GameMaster>();
            var gmInstanceProp = typeof(GameMaster).GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            gmInstanceProp.SetValue(null, gameMaster);

            grid = new Grid(10, 10);
            manager.Grid = grid;

            ruleset = ScriptableObject.CreateInstance<BattleBrothersRuleset>();
            ruleset.movement = ScriptableObject.CreateInstance<MovementModule>();
            ruleset.combat = ScriptableObject.CreateInstance<CombatModule>();
            ruleset.tactical = ScriptableObject.CreateInstance<TacticalModule>();
            gameMaster.ruleset = ruleset;

            unitSet = new UnitSet();
            unitManager.ActiveUnitSet = unitSet;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            typeof(UnitManager).GetProperty("Instance").SetValue(null, null);
            typeof(GridVisualizationManager).GetProperty("Instance").SetValue(null, null);
            typeof(GameMaster).GetProperty("Instance").SetValue(null, null);

            Object.DestroyImmediate(managerGO);
            Object.DestroyImmediate(unitManagerGO);
            Object.DestroyImmediate(gameMasterGO);
            Object.DestroyImmediate(ruleset);
            yield return null;
        }

        private Unit SetupUnit(int teamId, int mrng, int rrng)
        {
            GameObject go = new GameObject("Unit");
            Unit u = go.AddComponent<Unit>();
            
            var type = new UnitType { id = "unit_" + teamId + "_" + mrng + "_" + rrng, Name = "Unit" };
            type.Stats = new List<UnitStatValue> { 
                new UnitStatValue { id = "MAT", value = mrng > 0 ? 60 : 0 },
                new UnitStatValue { id = "RAT", value = rrng > 0 ? 60 : 0 },
                new UnitStatValue { id = "RNG", value = mrng > 0 ? mrng : rrng }
            };
            unitSet.units.Add(type);
            u.Initialize(type.id, teamId);
            return u;
        }

        private void AddNeighborsInRange(HexData center, int range)
        {
            for (int q = -range; q <= range; q++)
            {
                for (int r = Mathf.Max(-range, -q - range); r <= Mathf.Min(range, -q + range); r++)
                {
                    if (q == 0 && r == 0) continue;
                    int targetQ = center.Q + q;
                    int targetR = center.R + r;
                    if (grid.GetHexAt(targetQ, targetR) == null)
                    {
                        grid.AddHex(new HexData(targetQ, targetR));
                    }
                }
            }
        }

        [Test]
        public void AoA_MeleeUnit_ShowsCorrectRange()
        {
            HexData h = new HexData(0, 0);
            grid.AddHex(h);
            AddNeighborsInRange(h, 1);

            Unit u = SetupUnit(0, 1, 0);
            u.SetHex(manager.GetHexView(h));

            ruleset.tactical.ShowAoA(u, h, ruleset.movement.maxElevationDelta);
            
            int aoaCount = 0;
            foreach (var hex in grid.GetAllHexes())
            {
                if (hex.States.Contains($"AoA{u.teamId}_{u.Id}")) aoaCount++;
            }
            
            Assert.AreEqual(6, aoaCount, "Melee unit with range 1 should have 6 AoA states.");
        }

        [Test]
        public void AoA_RangedUnit_ShowsCorrectRange()
        {
            HexData h = new HexData(0, 0);
            grid.AddHex(h);
            AddNeighborsInRange(h, 3);

            Unit u = SetupUnit(0, 0, 3);
            u.SetHex(manager.GetHexView(h));

            ruleset.tactical.ShowAoA(u, h, ruleset.movement.maxElevationDelta);
            
            int aoaCount = 0;
            foreach (var hex in grid.GetAllHexes())
            {
                if (hex.States.Contains($"AoA{u.teamId}_{u.Id}")) aoaCount++;
            }
            
            Assert.AreEqual(36, aoaCount, "Ranged unit with range 3 should have 36 AoA states.");
        }

        [Test]
        public void AoA_MeleeUnit_RespectsElevation()
        {
            HexData h = new HexData(0, 0);
            grid.AddHex(h);
            AddNeighborsInRange(h, 1);

            // Set one neighbor to very high elevation
            var neighbors = grid.GetNeighbors(h);
            neighbors[0].Elevation = 5.0f;
            ruleset.movement.maxElevationDelta = 1.0f;

            Unit u = SetupUnit(0, 1, 0);
            u.SetHex(manager.GetHexView(h));

            ruleset.tactical.ShowAoA(u, h, ruleset.movement.maxElevationDelta);
            
            int aoaCount = 0;
            foreach (var hex in grid.GetAllHexes())
            {
                if (hex.States.Contains($"AoA{u.teamId}_{u.Id}")) aoaCount++;
            }
            
            Assert.AreEqual(5, aoaCount, "Melee unit should only see 5 neighbors within elevation limit.");
        }

        [Test]
        public void AoA_RangedUnit_IgnoresElevation()
        {
            HexData h = new HexData(0, 0);
            grid.AddHex(h);
            AddNeighborsInRange(h, 1);

            var neighbors = grid.GetNeighbors(h);
            neighbors[0].Elevation = 5.0f;
            ruleset.movement.maxElevationDelta = 1.0f;

            Unit u = SetupUnit(0, 0, 1);
            u.SetHex(manager.GetHexView(h));

            ruleset.tactical.ShowAoA(u, h, ruleset.movement.maxElevationDelta);
            
            int aoaCount = 0;
            foreach (var hex in grid.GetAllHexes())
            {
                if (hex.States.Contains($"AoA{u.teamId}_{u.Id}")) aoaCount++;
            }
            
            Assert.AreEqual(6, aoaCount, "Ranged unit should ignore elevation for AoA visualization.");
        }
    }
}