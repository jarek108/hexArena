using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HexGame;
using System.Reflection;

namespace HexGame.Tests
{
    public class GridPersistenceTests
    {
        private GameObject go;
        private GridCreator creator;
        private GridVisualizationManager manager;

        [SetUp]
        public void Setup()
        {
            go = new GameObject("GridContext");
            manager = go.AddComponent<GridVisualizationManager>();
            creator = go.AddComponent<GridCreator>();
            creator.Initialize(manager);
            
            // Ensure manager has basic dependencies mock/setup
            manager.hexSurfaceMaterial = new Material(Shader.Find("Hidden/InternalErrorShader")); 
            manager.hexMaterialSides = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }

        [TearDown]
        public void Teardown()
        {
            if (go != null) Object.DestroyImmediate(go);
        }

        [Test]
        public void Grid_Persists_Through_Serialization_Cycle()
        {
            // 1. Generate initial grid
            creator.gridWidth = 5;
            creator.gridHeight = 5;
            creator.GenerateGrid();

            Assert.IsNotNull(manager.Grid, "Grid should be generated initially.");
            Assert.AreEqual(25, manager.Grid.GetAllHexes().GetEnumerator().MoveNext() ? 25 : 0, "Grid should have 25 hexes (bug in count check logic fixed below).");
            
            // Fix count check properly
            int count = 0;
            foreach(var _ in manager.Grid.GetAllHexes()) count++;
            Assert.AreEqual(25, count);

            // 2. Modify specific data to test integrity
            HexData specificHex = manager.Grid.GetHexAt(0, 0);
            specificHex.TerrainType = TerrainType.Mountains;
            specificHex.Elevation = 5;

            // 3. Trigger Serialization (Save state to internal string)
            creator.OnBeforeSerialize();

            // 4. SIMULATE DOMAIN RELOAD / DATA LOSS
            // Destroy the Grid object in memory
            manager.Grid = null;
            // Destroy the visual gameobjects
            creator.ClearGrid(); 
            
            Assert.IsNull(manager.Grid, "Grid should be null after simulated wipe.");
            Assert.AreEqual(0, go.transform.childCount, "Visuals should be gone.");

            // 5. Restore (Simulate OnEnable after reload)
            // We need to use reflection or just call the method if public. 
            // OnEnable is private, but we added logic to call RestoreGridFromState inside it.
            // We can invoke OnEnable via reflection.
            MethodInfo onEnableMethod = typeof(GridCreator).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            onEnableMethod.Invoke(creator, null);

            // 6. Assert Restoration
            Assert.IsNotNull(manager.Grid, "Grid should be restored.");
            
            int newCount = 0;
            foreach(var _ in manager.Grid.GetAllHexes()) newCount++;
            Assert.AreEqual(25, newCount, "Grid should have 25 hexes after restoration.");

            HexData restoredHex = manager.Grid.GetHexAt(0, 0);
            Assert.IsNotNull(restoredHex);
            Assert.AreEqual(TerrainType.Mountains, restoredHex.TerrainType, "Terrain type should persist.");
            Assert.AreEqual(5, restoredHex.Elevation, "Elevation should persist.");
        }

        [Test]
        public void Restoration_Clears_Old_Visuals()
        {
            // 1. Generate initial grid
            creator.gridWidth = 2;
            creator.gridHeight = 2;
            creator.GenerateGrid();
            
            // 2. Serialize
            creator.OnBeforeSerialize();

            // 3. Simulate a "dirty" reload where children might still exist but reference is lost
            // We do NOT call ClearGrid() here, mimicking a raw recompile where scene objects persist but C# RAM is reset.
            manager.Grid = null; 
            
            // Ensure children exist
            Assert.Greater(go.transform.childCount, 0);

            // 4. Restore
            MethodInfo onEnableMethod = typeof(GridCreator).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            onEnableMethod.Invoke(creator, null);

            // 5. Assert
            // If ClearGrid wasn't called, we might have double the hexes visually (4 old + 4 new = 8)
            // With ClearGrid, we should have exactly 4.
            Assert.AreEqual(4, go.transform.childCount, "Should only have 4 hex visuals, implying duplicates were cleared.");
        }
    }
}