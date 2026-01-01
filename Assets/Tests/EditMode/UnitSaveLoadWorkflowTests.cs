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
    public class UnitSaveLoadWorkflowTests
    {
        private GameObject managerGO;
        private GridVisualizationManager manager;
        private GameObject unitManagerGO;
        private UnitManager unitManager;
        
        private string tempSetPath;
        private string tempLayoutPath;
        private string tempSchemaPath; // UnitSet needs a schema usually, or embedded definitions

        [SetUp]
        public void SetUp()
        {
            // 1. Setup Grid
            managerGO = TestHelper.CreateTestManager();
            manager = managerGO.GetComponent<GridVisualizationManager>();
            Grid grid = new Grid(5, 5);
            for (int r = 0; r < 5; r++)
                for (int q = 0; q < 5; q++)
                    grid.AddHex(new HexData(q, r));
            manager.VisualizeGrid(grid);

            // 2. Setup UnitManager
            unitManagerGO = new GameObject("Units");
            unitManager = unitManagerGO.AddComponent<UnitManager>();
            
            // 3. Create a temporary UnitSet on disk
            // We need a valid path under Assets/Data/Sets for ResolveSetById to find it
            string setsDir = Path.Combine("Assets", "Data", "Sets");
            if (!Directory.Exists(setsDir)) Directory.CreateDirectory(setsDir);
            
            UnitSet diskSet = new UnitSet();
            diskSet.setName = "TempDiskSet";
            diskSet.units = new List<UnitType> 
            { 
                new UnitType { id = "diskUnit1", Name = "Disk Unit 1" } 
            };
            
            tempSetPath = Path.Combine(setsDir, "temp_test_set.json");
            File.WriteAllText(tempSetPath, diskSet.ToJson());

            // 4. Setup Paths
            tempLayoutPath = Path.Combine(Application.temporaryCachePath, "test_layout.json");
            
            // 5. Setup Viz Prefab
            GameObject testVizGO = new GameObject("TestViz");
            var viz = testVizGO.AddComponent<SimpleUnitVisualization>();
            unitManager.unitVisualizationPrefab = viz;
        }

        [TearDown]
        public void TearDown()
        {
            if (managerGO != null) Object.DestroyImmediate(managerGO);
            if (unitManagerGO != null) Object.DestroyImmediate(unitManagerGO);
            
            // Clean up files
            if (File.Exists(tempSetPath)) File.Delete(tempSetPath);
            if (File.Exists(tempLayoutPath)) File.Delete(tempLayoutPath);
            
            // Clean up Viz Prefab
            var viz = Object.FindAnyObjectByType<UnitVisualization>();
            if (viz != null && viz.gameObject.name == "TestViz") Object.DestroyImmediate(viz.gameObject);
        }

        [Test]
        public void FullCycle_SaveEraseLoad_RestoresUnitsAndSet()
        {
            // 1. Load the set into the manager (simulating user selecting it)
            unitManager.activeUnitSetPath = tempSetPath;
            unitManager.LoadActiveSet();
            
            Assert.IsNotNull(unitManager.ActiveUnitSet, "Active set should be loaded.");
            Assert.AreEqual("TempDiskSet", unitManager.ActiveUnitSet.setName);

            // 2. Spawn a unit
            HexData hexData = manager.Grid.GetHexAt(2, 2);
            Hex hexView = manager.GetHexView(hexData);
            unitManager.SpawnUnit("diskUnit1", 0, hexView);

            Assert.IsNotNull(hexData.Unit, "Unit should be spawned.");
            Assert.AreEqual("diskUnit1", hexData.Unit.UnitTypeId);

            // 3. Save Layout
            unitManager.SaveUnits(tempLayoutPath);
            Assert.IsTrue(File.Exists(tempLayoutPath), "Layout file should be created.");

            // 4. ERASE EVERYTHING
            unitManager.EraseAllUnits();
            Assert.IsNull(hexData.Unit, "Unit should be gone.");
            
            // Reset Manager's Active Set to simulate a fresh state or lost context
            unitManager.activeUnitSetPath = "";
            unitManager.ActiveUnitSet = null;

            // 5. Load Layout
            // This should trigger ResolveSetById -> Find "TempDiskSet" in Assets/Data/Sets -> Load it -> Spawn units
            unitManager.LoadUnits(tempLayoutPath);

            // 6. Verify
            Assert.IsNotNull(unitManager.ActiveUnitSet, "Manager should have resolved and loaded the set from the layout ID.");
            Assert.AreEqual("TempDiskSet", unitManager.ActiveUnitSet.setName);
            
            // Check Unit
            Assert.IsNotNull(hexData.Unit, "Unit should be respawned.");
            Assert.AreEqual("diskUnit1", hexData.Unit.UnitTypeId);
            Assert.AreEqual(2, hexData.Unit.CurrentHex.Q);
            Assert.AreEqual(2, hexData.Unit.CurrentHex.R);
        }
    }
}