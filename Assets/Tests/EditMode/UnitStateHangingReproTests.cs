using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HexGame;
using HexGame.Tools;
using HexGame.Units;
using System.Collections.Generic;
using System.IO;

namespace HexGame.Tests
{
    public class UnitStateHangingReproTests
    {
        private GameObject managerGO;
        private GridVisualizationManager manager;
        private GameObject unitManagerGO;
        private UnitManager unitManager;
        
        [SetUp]
        public void SetUp()
        {
            managerGO = TestHelper.CreateTestManager();
            manager = managerGO.GetComponent<GridVisualizationManager>();
            Grid grid = new Grid(5, 5);
            for (int r = 0; r < 5; r++)
                for (int q = 0; q < 5; q++)
                    grid.AddHex(new HexData(q, r));
            manager.VisualizeGrid(grid);

            unitManagerGO = new GameObject("Units");
            unitManager = unitManagerGO.AddComponent<UnitManager>();
            
            // Minimal UnitSet
            UnitSet testSet = new UnitSet();
            testSet.setName = "ReproSet";
            testSet.units = new List<UnitType> { new UnitType { id = "u1", Name = "U1" } };
            unitManager.ActiveUnitSet = testSet;

            // Minimal Viz
            var vizGO = new GameObject("Viz");
            var viz = vizGO.AddComponent<SimpleUnitVisualization>();
            unitManager.unitVisualizationPrefab = viz;
        }

        [TearDown]
        public void TearDown()
        {
            if (managerGO != null) Object.DestroyImmediate(managerGO);
            if (unitManagerGO != null) Object.DestroyImmediate(unitManagerGO);
            var viz = Object.FindAnyObjectByType<UnitVisualization>();
            if (viz != null) Object.DestroyImmediate(viz.gameObject);
        }

        [Test]
        public void LoadUnits_ShouldCleanUp_OldUnitStates()
        {
            // 1. Spawn Unit
            HexData hexData = manager.Grid.GetHexAt(2, 2);
            Hex hexView = manager.GetHexView(hexData);
            unitManager.SpawnUnit("u1", 0, hexView);
            
            Unit unit = hexData.Unit;
            Assert.IsNotNull(unit);

            // 2. Add State (Simulate ZoC)
            string hangingState = "ZoC_Hanging";
            unit.AddOwnedHexState(hexData, hangingState);
            Assert.IsTrue(hexData.States.Contains(hangingState));

            // Force Instance to null to simulate race condition/destruction order
            typeof(GridVisualizationManager).GetProperty("Instance").SetValue(null, null);

            // 3. Create Dummy Layout File
            string dummyPath = Path.Combine(Application.temporaryCachePath, "dummy_layout.json");
            File.WriteAllText(dummyPath, "{\"unitSetId\":\"ReproSet\",\"units\":[]}");

            // 4. Trigger Load (which calls EraseAllUnits)
            unitManager.LoadUnits(dummyPath);
            
            // Cleanup file
            if(File.Exists(dummyPath)) File.Delete(dummyPath);

            // 5. Assert Unit is gone
            Assert.IsNull(hexData.Unit);
            
            // 5. Assert State is gone
            // If the bug exists, this assertion will FAIL.
            Assert.IsFalse(hexData.States.Contains(hangingState), "The 'ZoC_Hanging' state should be removed when unit is destroyed via LoadUnits.");
        }
    }
}