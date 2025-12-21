using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HexGame;

namespace HexGame.Tests
{
    public class GridVisibilityTests
    {
        private GameObject managerGO;
        private GridVisualizationManager manager;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            managerGO = TestHelper.CreateTestManager();
            manager = managerGO.GetComponent<GridVisualizationManager>();
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(managerGO);
        }

        [UnityTest]
        public IEnumerator GridManager_Flag_TogglesRimWidth()
        {
            // Use reflection to set the public gridWidth field to something known
            var managerType = typeof(GridVisualizationManager);
            var widthField = managerType.GetField("gridWidth", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            if (widthField == null) Assert.Fail("Could not find field 'gridWidth' on GridVisualizationManager");
            widthField.SetValue(manager, 0.1f);

            var showGridField = managerType.GetField("showGrid", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            if (showGridField == null) Assert.Fail("Could not find field 'showGrid' on GridVisualizationManager");

            // 1. Grid On (showGrid = true) -> Rim width should be restored (0.1f)
            showGridField.SetValue(manager, true);
            manager.SendMessage("OnValidate");
            yield return null;
            Assert.AreEqual(0.1f, manager.GetDefaultRimSettings().width, "Rim width should be 0.1f when showGrid is true.");

            // 2. Grid Off (showGrid = false) -> Rim width should be 0
            showGridField.SetValue(manager, false);
            manager.SendMessage("OnValidate");
            yield return null;
            Assert.AreEqual(0f, manager.GetDefaultRimSettings().width, "Rim width should be 0 when showGrid is false.");
        }
    }
}
