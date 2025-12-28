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
            var settings = new List<GridVisualizationManager.StateSetting>
            {
                new GridVisualizationManager.StateSetting { 
                    state = "Default", 
                    priority = 0, 
                    visuals = new GridVisualizationManager.RimSettings { color = Color.black, width = 0.1f } 
                },
                new GridVisualizationManager.StateSetting { 
                    state = "Hovered", 
                    priority = 10, 
                    visuals = new GridVisualizationManager.RimSettings { color = Color.yellow, width = 0.2f } 
                },
                new GridVisualizationManager.StateSetting { 
                    state = "Selected", 
                    priority = 20, 
                    visuals = new GridVisualizationManager.RimSettings { color = Color.red, width = 0.3f } 
                }
            };
            manager.stateSettings = settings;

            var data = new HexData(0, 0);
            var hexGO = new GameObject("Hex");
            var hex = hexGO.AddComponent<Hex>();
            var mr = hexGO.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new Material[] { manager.hexSurfaceMaterial, manager.hexMaterialSides };
            hex.AssignData(data);

            // 1. Both Selected and Hovered -> Selected wins (higher priority)
            data.AddState("Selected");
            data.AddState("Hovered");
            manager.RefreshVisuals(hex);
            Assert.AreEqual(Color.red, manager.GetHexRimColor(hex));

            yield return null;
        }

        [UnityTest]
        public IEnumerator Visualizer_FallsBack_WhenStateRemoved()
        {
            manager.stateSettings = new List<GridVisualizationManager.StateSetting>
            {
                new GridVisualizationManager.StateSetting { state = "Default", priority = 0, visuals = new GridVisualizationManager.RimSettings { color = Color.black } },
                new GridVisualizationManager.StateSetting { state = "Selected", priority = 20, visuals = new GridVisualizationManager.RimSettings { color = Color.red } }
            };

            var data = new HexData(0, 0);
            var hexGO = new GameObject("Hex");
            var hex = hexGO.AddComponent<Hex>();
            var mr = hexGO.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new Material[] { manager.hexSurfaceMaterial, manager.hexMaterialSides };
            hex.AssignData(data);

            data.AddState("Selected");
            manager.RefreshVisuals(hex);
            
            data.RemoveState("Selected");
            manager.RefreshVisuals(hex);

            Assert.AreEqual(Color.black, manager.GetHexRimColor(hex));

            yield return null;
        }

        [UnityTest]
        public IEnumerator Visualizer_ResolvesPrefixMatching()
        {
            manager.stateSettings = new List<GridVisualizationManager.StateSetting>
            {
                new GridVisualizationManager.StateSetting { 
                    state = "ZoC0", 
                    priority = 10, 
                    visuals = new GridVisualizationManager.RimSettings { color = Color.blue } 
                }
            };

            var data = new HexData(0, 0);
            var hexGO = new GameObject("Hex");
            var hex = hexGO.AddComponent<Hex>();
            var mr = hexGO.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new Material[] { manager.hexSurfaceMaterial, manager.hexMaterialSides };
            hex.AssignData(data);

            // Match prefix ZoC0_
            data.AddState("ZoC0_123");
            manager.RefreshVisuals(hex);
            Assert.AreEqual(Color.blue, manager.GetHexRimColor(hex), "Prefix matching ZoC0_ should trigger visual rule for ZoC0.");

            yield return null;
        }
    }
}
