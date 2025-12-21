using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HexGame;
using System.Linq;

namespace HexGame.Tests
{
    public class GridVisualizationStylingTests
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
        public IEnumerator Visualizer_ResolvesToHighestPriority()
        {
            manager.stateSettings = new List<GridVisualizationManager.StateSetting>
            {
                new GridVisualizationManager.StateSetting { 
                    state = HexState.Default, 
                    priority = 0, 
                    visuals = new GridVisualizationManager.RimSettings { color = Color.black, width = 0f } 
                },
                new GridVisualizationManager.StateSetting { 
                    state = HexState.Hovered, 
                    priority = 10, 
                    visuals = new GridVisualizationManager.RimSettings { color = Color.yellow, width = 0.2f } 
                },
                new GridVisualizationManager.StateSetting { 
                    state = HexState.Selected, 
                    priority = 20, 
                    visuals = new GridVisualizationManager.RimSettings { color = Color.red, width = 0.2f } 
                }
            };

            var data = new HexData(0, 0);
            var hexGO = new GameObject("Hex");
            var hex = hexGO.AddComponent<Hex>();
            var mr = hexGO.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new Material[] { manager.hexSurfaceMaterial, manager.hexMaterialSides };
            hex.AssignData(data);

            // 1. Both Selected and Hovered -> Selected wins (higher priority)
            data.AddState(HexState.Selected);
            data.AddState(HexState.Hovered);
            manager.RefreshVisuals(hex);
            Assert.AreEqual(Color.red, manager.GetHexRimColor(hex));

            yield return null;
        }

        [UnityTest]
        public IEnumerator Visualizer_FallsBack_WhenStateRemoved()
        {
            manager.stateSettings = new List<GridVisualizationManager.StateSetting>
            {
                new GridVisualizationManager.StateSetting { state = HexState.Default, priority = 0, visuals = new GridVisualizationManager.RimSettings { color = Color.black } },
                new GridVisualizationManager.StateSetting { state = HexState.Selected, priority = 20, visuals = new GridVisualizationManager.RimSettings { color = Color.red } }
            };

            var data = new HexData(0, 0);
            var hexGO = new GameObject("Hex");
            var hex = hexGO.AddComponent<Hex>();
            var mr = hexGO.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new Material[] { manager.hexSurfaceMaterial, manager.hexMaterialSides };
            hex.AssignData(data);

            data.AddState(HexState.Selected);
            manager.RefreshVisuals(hex);
            
            data.RemoveState(HexState.Selected);
            manager.RefreshVisuals(hex);

            Assert.AreEqual(Color.black, manager.GetHexRimColor(hex));

            yield return null;
        }
    }
}
