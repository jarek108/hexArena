using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HexGame;
using System.Collections.Generic;
using System.Linq;

namespace HexGame.Tests
{
    [TestFixture]
    public class HexStateVisualizerTests
    {
        private GameObject managerGO;
        private HexGridManager manager;
        private GridCreator creator;
        private HexStateVisualizer visualizer;
        private Hex testHex;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            managerGO = new GameObject("HexGridManager");
            manager = managerGO.AddComponent<HexGridManager>();
            creator = managerGO.AddComponent<GridCreator>();
            visualizer = managerGO.AddComponent<HexStateVisualizer>();
            
            creator.Initialize(manager);
            
            // Setup some dummy visual settings
            visualizer.stateSettings = new List<HexStateVisualizer.StateSetting>
            {
                new HexStateVisualizer.StateSetting { 
                    state = HexState.Default, 
                    priority = 0, 
                    visuals = new HexGridManager.RimSettings { color = Color.black, width = -1f } 
                },
                new HexStateVisualizer.StateSetting { 
                    state = HexState.Hovered, 
                    priority = 10, 
                    visuals = new HexGridManager.RimSettings { color = Color.yellow, width = 0.2f } 
                },
                new HexStateVisualizer.StateSetting { 
                    state = HexState.Selected, 
                    priority = 20, 
                    visuals = new HexGridManager.RimSettings { color = Color.red, width = 0.2f } 
                }
            };

            yield return null;
            creator.GenerateGrid();
            yield return null;
            
            testHex = manager.GetHexView(manager.Grid.GetHexAt(0, 0));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.DestroyImmediate(managerGO);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Visualizer_ResolvesToHighestPriority()
        {
            // Add both Hovered and Selected
            testHex.Data.AddState(HexState.Hovered);
            testHex.Data.AddState(HexState.Selected);
            
            yield return null; // Allow visualizer to react

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            testHex.GetComponent<Renderer>().GetPropertyBlock(block, 0);
            
            Assert.AreEqual(Color.red, block.GetColor("_RimColor"), "Selected (Priority 20) should win over Hovered (Priority 10)");
        }

        [UnityTest]
        public IEnumerator Visualizer_FallsBack_WhenStateRemoved()
        {
            testHex.Data.AddState(HexState.Hovered);
            testHex.Data.AddState(HexState.Selected);
            yield return null;

            // Remove Selected
            testHex.Data.RemoveState(HexState.Selected);
            yield return null;

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            testHex.GetComponent<Renderer>().GetPropertyBlock(block, 0);
            
            Assert.AreEqual(Color.yellow, block.GetColor("_RimColor"), "Visuals should fall back to Hovered after Selected is removed");
        }

        [UnityTest]
        public IEnumerator Visualizer_RevertsToDefault_WhenEmpty()
        {
            testHex.Data.AddState(HexState.Hovered);
            yield return null;
            
            testHex.Data.RemoveState(HexState.Hovered);
            yield return null;

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            testHex.GetComponent<Renderer>().GetPropertyBlock(block, 0);
            
            Assert.AreEqual(Color.black, block.GetColor("_RimColor"), "Visuals should revert to Default settings when no other states are active");
            Assert.Less(block.GetFloat("_RimWidth"), 0f);
        }

        [UnityTest]
        public IEnumerator Visualizer_PropagatesSettingsChange_ToAllHexes()
        {
            // 1. Setup multiple hexes
            Hex hexA = manager.GetHexView(manager.Grid.GetHexAt(0, 0));
            Hex hexB = manager.GetHexView(manager.Grid.GetHexAt(1, 1));
            
            hexA.Data.AddState(HexState.Selected);
            hexB.Data.AddState(HexState.Selected);
            yield return null;

            // 2. Change Selected color in visualizer
            Color newSelectedColor = Color.blue;
            for (int i = 0; i < visualizer.stateSettings.Count; i++)
            {
                if (visualizer.stateSettings[i].state == HexState.Selected)
                {
                    var s = visualizer.stateSettings[i];
                    s.visuals.color = newSelectedColor;
                    visualizer.stateSettings[i] = s;
                    break;
                }
            }

            // 3. Trigger global refresh (simulating OnValidate)
            visualizer.SendMessage("OnValidate");
            yield return null;

            // 4. Assert both reflect new color
            MaterialPropertyBlock blockA = new MaterialPropertyBlock();
            hexA.GetComponent<Renderer>().GetPropertyBlock(blockA, 0);
            MaterialPropertyBlock blockB = new MaterialPropertyBlock();
            hexB.GetComponent<Renderer>().GetPropertyBlock(blockB, 0);

            Assert.AreEqual(newSelectedColor, blockA.GetColor("_RimColor"), "Hex A should have updated Selected color.");
            Assert.AreEqual(newSelectedColor, blockB.GetColor("_RimColor"), "Hex B should have updated Selected color.");
        }
    }
}