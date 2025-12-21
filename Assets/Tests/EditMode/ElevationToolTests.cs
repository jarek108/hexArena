using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HexGame;
using HexGame.Tools;
using System.Collections.Generic;

namespace HexGame.Tests
{
    public class ElevationToolTests
    {
        private GameObject managerGO;
        private HexGridManager manager;
        private GridCreator creator;
        private ElevationTool elevationTool;
        private Hex testHex;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            managerGO = TestHelper.CreateTestManager();
            manager = managerGO.GetComponent<HexGridManager>();
            creator = managerGO.GetComponent<GridCreator>();
            elevationTool = managerGO.AddComponent<ElevationTool>();
            
            creator.GenerateGrid();
            yield return null;
            
            testHex = manager.GetHexView(manager.Grid.GetHexAt(0, 0));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(managerGO);
        }

        [UnityTest]
        public IEnumerator ElevationTool_LeftClick_IncreasesElevation()
        {
            elevationTool.OnActivate();
            float initialElevation = testHex.Elevation;

            var method = elevationTool.GetType().GetMethod("ChangeElevation", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(elevationTool, new object[] { testHex, 1f });

            yield return null;
            Assert.AreEqual(initialElevation + 1f, testHex.Elevation, "Elevation should have increased by 1.");
        }

        [UnityTest]
        public IEnumerator ElevationTool_RightClick_DecreasesElevation()
        {
            elevationTool.OnActivate();
            testHex.Elevation = 5f;
            float initialElevation = testHex.Elevation;

            var method = elevationTool.GetType().GetMethod("ChangeElevation", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(elevationTool, new object[] { testHex, -1f });

            yield return null;
            Assert.AreEqual(initialElevation - 1f, testHex.Elevation, "Elevation should have decreased by 1.");
        }

        [UnityTest]
        public IEnumerator ElevationTool_RespectsClamping()
        {
            elevationTool.OnActivate();
            testHex.Elevation = 0f;

            var method = elevationTool.GetType().GetMethod("ChangeElevation", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            method.Invoke(elevationTool, new object[] { testHex, -1f });
            yield return null;
            Assert.AreEqual(0f, testHex.Elevation, "Elevation should be clamped at 0.");

            testHex.Elevation = 10f;
            method.Invoke(elevationTool, new object[] { testHex, 1f });
            yield return null;
            Assert.AreEqual(10f, testHex.Elevation, "Elevation should be clamped at 10.");
        }
    }
}
