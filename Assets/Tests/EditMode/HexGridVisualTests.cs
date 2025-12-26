using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HexGame;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HexGame.Tests
{
    public class HexGridVisualTests
    {
        private GameObject managerGO;
        private GridVisualizationManager manager;
        private GridCreator creator;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            managerGO = TestHelper.CreateTestManager();
            manager = managerGO.GetComponent<GridVisualizationManager>();
            creator = managerGO.GetComponent<GridCreator>();
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(managerGO);
        }

        [UnityTest]
        public IEnumerator GridVisualizationManager_GeneratesGrid_SpawnsObjects()
        {
            HexGrid grid = new HexGrid(3, 3);
            for (int r = 0; r < 3; r++)
                for (int q = 0; q < 3; q++)
                    grid.AddHex(new HexData(q, r));

            manager.VisualizeGrid(grid);

            yield return null;

            // Container is now the manager transform itself
            Assert.AreEqual(9, manager.transform.childCount, "Grid should have 9 hex children spawned.");
        }

        [UnityTest]
        public IEnumerator GridVisualizationManager_RegeneratesAndClearsGrid_Correctly()
        {
            SerializedObject so = new SerializedObject(creator);

            // --- First Generation (5x5) ---
            so.FindProperty("gridWidth").intValue = 5;
            so.FindProperty("gridHeight").intValue = 5;
            so.ApplyModifiedProperties();
            creator.GenerateGrid();
            yield return null;
            
            Assert.AreEqual(25, manager.transform.childCount, "First grid generation should spawn 25 hexes.");

            // --- Second Generation (3x3) ---
            so.FindProperty("gridWidth").intValue = 3;
            so.FindProperty("gridHeight").intValue = 3;
            so.ApplyModifiedProperties();
            creator.GenerateGrid();
            yield return null;
            
            Assert.AreEqual(9, manager.transform.childCount, "Second grid generation should spawn 9 hexes.");

            // --- Clear Grid ---
            creator.ClearGrid();
            yield return null;
            
            Assert.AreEqual(0, manager.transform.childCount, "ClearGrid should remove all hexes from container.");
        }
    }
}
