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
        private HexGridManager manager;
        private HexStateVisualizer visualizer;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            managerGO = TestHelper.CreateTestManager();
            manager = managerGO.GetComponent<HexGridManager>();
            visualizer = managerGO.GetComponent<HexStateVisualizer>();
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
            var visualizerType = typeof(HexStateVisualizer);
            var widthField = visualizerType.GetField("gridWidth", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            if (widthField == null) Assert.Fail("Could not find field 'gridWidth' on HexStateVisualizer");
            widthField.SetValue(visualizer, 0.1f);

            var showGridField = visualizerType.GetField("showGrid", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            if (showGridField == null) Assert.Fail("Could not find field 'showGrid' on HexStateVisualizer");

            // 1. Grid On (showGrid = true) -> Rim width should be restored (0.1f)
            showGridField.SetValue(visualizer, true);
            visualizer.SendMessage("OnValidate");
            yield return null;
            Assert.AreEqual(0.1f, visualizer.GetDefaultRimSettings().width, "Rim width should be 0.1f when showGrid is true.");

            // 2. Grid Off (showGrid = false) -> Rim width should be 0
            showGridField.SetValue(visualizer, false);
            visualizer.SendMessage("OnValidate");
            yield return null;
            Assert.AreEqual(0f, visualizer.GetDefaultRimSettings().width, "Rim width should be 0 when showGrid is false.");
        }
    }
}
