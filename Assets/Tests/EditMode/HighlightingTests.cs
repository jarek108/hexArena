using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HexGame;
using HexGame.Tools;
using HexGame.Tests;
using System.Collections.Generic;

namespace HexGame.Tests
{
    public class HighlightingTests
    {
        private GameObject managerGO;
        private GridVisualizationManager manager;
        private GridCreator creator;
        private PathfindingTool pathfindingTool;
        private ToolManager toolManager;
        private Hex testHex;

        private GridVisualizationManager.RimSettings highlightVisuals = new GridVisualizationManager.RimSettings { color = Color.yellow, width = 0.2f, pulsation = 5f };
        private GridVisualizationManager.RimSettings selectionVisuals = new GridVisualizationManager.RimSettings { color = Color.red, width = 0.2f, pulsation = 2f };
        private GridVisualizationManager.RimSettings defaultVisuals = new GridVisualizationManager.RimSettings { color = Color.black, width = 0f, pulsation = 0f };

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            managerGO = TestHelper.CreateTestManager();
            manager = managerGO.GetComponent<GridVisualizationManager>();
            // Force this instance to be the global instance for the test
            typeof(GridVisualizationManager).GetProperty("Instance").SetValue(null, manager);
            
            creator = managerGO.GetComponent<GridCreator>();
            toolManager = managerGO.GetComponent<ToolManager>();
            pathfindingTool = managerGO.GetComponent<PathfindingTool>();

            var caster = managerGO.AddComponent<HexRaycaster>();
            toolManager.SetActiveTool(pathfindingTool);

            manager.stateSettings = new List<GridVisualizationManager.StateSetting>
            {
                new GridVisualizationManager.StateSetting { state = HexState.Default, priority = 0, visuals = defaultVisuals },
                new GridVisualizationManager.StateSetting { state = HexState.Hovered, priority = 10, visuals = highlightVisuals },
                new GridVisualizationManager.StateSetting { state = HexState.Selected, priority = 20, visuals = selectionVisuals },
                new GridVisualizationManager.StateSetting { state = HexState.Target, priority = 30, visuals = new GridVisualizationManager.RimSettings { color = Color.blue, width = 0.15f } }
            };
            
            manager.RefreshVisuals(); // Apply settings to all hexes
            
            creator.GenerateGrid();
            yield return null;
            HexData data = manager.Grid.GetHexAt(0, 0);
            testHex = manager.GetHexView(data);
        }
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Object.DestroyImmediate(managerGO);
        yield return null;
    }

    [UnityTest]
    public IEnumerator HoveringOffSelectedHex_DoesNotClearSelectionVisuals()
    {
        pathfindingTool.SetSource(testHex);
        yield return null;
        Assert.AreEqual(selectionVisuals.color, manager.GetHexRimColor(testHex), "Pre-condition: Hex should be visually selected.");

        toolManager.ManualUpdate(null); // Hover off
        yield return null;
        Assert.AreEqual(selectionVisuals.color, manager.GetHexRimColor(testHex), "Selection should remain Red when hovering off.");
    }
    
    [UnityTest]
    public IEnumerator Transition_SelectToDeselect_UpdatesProperties()
    {
        pathfindingTool.SetSource(testHex);
        yield return null;
        Assert.Greater(manager.GetHexRimWidth(testHex), 0f);

        pathfindingTool.SetSource(testHex); // Toggle selection off
        yield return null;
        manager.RefreshVisuals(testHex); // Force refresh to update visual cache
        Assert.AreEqual(0f, manager.GetHexRimWidth(testHex));
    }

    [UnityTest]
    public IEnumerator HighlightHex_ActivatesRim()
    {
        toolManager.ManualUpdate(testHex);
        yield return null;
        
        Assert.AreEqual(highlightVisuals.color, manager.GetHexRimColor(testHex), "Highlight should set RimColor to Yellow.");
        Assert.Greater(manager.GetHexRimWidth(testHex), 0f, "Highlight should set a positive RimWidth.");
        Assert.Greater(manager.GetHexRimPulsation(testHex), 0f, "Highlight should set a positive RimPulsationSpeed.");
    }

    [UnityTest]
    public IEnumerator SelectHex_ActivatesRim_WithDifferentSettings()
    {
        pathfindingTool.SetSource(testHex);
        yield return null;
        
        Assert.AreEqual(selectionVisuals.color, manager.GetHexRimColor(testHex), "Selection should set RimColor to Red.");
        Assert.Greater(manager.GetHexRimWidth(testHex), 0f, "Selection should set a positive RimWidth.");
        Assert.AreEqual(selectionVisuals.pulsation, manager.GetHexRimPulsation(testHex), "Selection should set RimPulsationSpeed to expected value.");
    }

    [UnityTest]
    public IEnumerator ResetHex_DeactivatesRim()
    {
        toolManager.ManualUpdate(testHex);
        yield return null;
        toolManager.ManualUpdate(null); // Simulate hovering off
        yield return null;
        
        Assert.AreEqual(0, manager.GetHexRimWidth(testHex), "Reset should set RimWidth to a zero value (hiding it).");
        Assert.AreEqual(defaultVisuals.color, manager.GetHexRimColor(testHex), "Reset should set RimColor to Black (default).");
    }

    [UnityTest]
    public IEnumerator Transition_HighlightToSelect_UpdatesProperties()
    {
                toolManager.ManualUpdate(testHex);
                yield return null;
                Assert.AreEqual(highlightVisuals.color, manager.GetHexRimColor(testHex));
        
                pathfindingTool.SetSource(testHex);
                yield return null;
                Assert.AreEqual(selectionVisuals.color, manager.GetHexRimColor(testHex));    }

    [UnityTest]
    public IEnumerator SwitchHighlight_ResetsOldHex()
    {
        Hex hexA = manager.GetHexView(manager.Grid.GetHexAt(0, 0));
        Hex hexB = manager.GetHexView(manager.Grid.GetHexAt(1, 0));
        
        toolManager.ManualUpdate(hexA);
        yield return null;
        toolManager.ManualUpdate(hexB);
        yield return null;
        Assert.AreEqual(defaultVisuals.color, manager.GetHexRimColor(hexA), "Old Hex A should be reset.");
        Assert.AreEqual(highlightVisuals.color, manager.GetHexRimColor(hexB), "New Hex B should be highlighted.");
    }

    [UnityTest]
    public IEnumerator SwitchSelection_ResetsOldHex()
    {
        Hex hexA = manager.GetHexView(manager.Grid.GetHexAt(0, 0));
        Hex hexB = manager.GetHexView(manager.Grid.GetHexAt(1, 0));
        
        pathfindingTool.SetSource(hexA);
        yield return null;
        pathfindingTool.SetSource(hexB);
        yield return null;
        Assert.AreEqual(defaultVisuals.color, manager.GetHexRimColor(hexA), "Old Hex A should be reset.");
        Assert.AreEqual(selectionVisuals.color, manager.GetHexRimColor(hexB), "New Hex B should be selected.");
    }

    [UnityTest]
    public IEnumerator Highlight_OnSelectedHex_Ignored()
    {
        pathfindingTool.SetSource(testHex);
        yield return null;
        Assert.AreEqual(selectionVisuals.color, manager.GetHexRimColor(testHex));

        toolManager.ManualUpdate(testHex); // Hover while selected
        yield return null;
        Assert.AreEqual(selectionVisuals.color, manager.GetHexRimColor(testHex), "Highlighting a Selected hex should be ignored (remain Red).");
    }

    [UnityTest]
    public IEnumerator HighlightHex_SetsPulsationSpeed()
    {
        toolManager.ManualUpdate(testHex);
        yield return null;
        Assert.AreEqual(highlightVisuals.pulsation, manager.GetHexRimPulsation(testHex), "Highlight should set RimPulsationSpeed to expected value.");
    }

    [UnityTest]
    public IEnumerator SelectHex_SetsPulsationSpeed()
    {
        pathfindingTool.SetSource(testHex);
        yield return null;
        Assert.AreEqual(selectionVisuals.pulsation, manager.GetHexRimPulsation(testHex), "Selection should set RimPulsationSpeed to expected value.");
    }

    [UnityTest]
    public IEnumerator ResetHex_SetsPulsationSpeed()
    {
        toolManager.ManualUpdate(testHex);
        yield return null;
        toolManager.ManualUpdate(null);
        yield return null;
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        testHex.GetComponent<Renderer>().GetPropertyBlock(block, 0);
        float rimPulsationSpeed = block.GetFloat("_RimPulsationSpeed");
        Assert.AreEqual(defaultVisuals.pulsation, rimPulsationSpeed, "Reset should set RimPulsationSpeed to Default value.");
    }
    
    [UnityTest]
    public IEnumerator UpdateSettings_ImmediatelyReflectedInActiveHighlight()
    {
        toolManager.ManualUpdate(testHex);
        yield return null;
        
        for (int i = 0; i < manager.stateSettings.Count; i++)
        {
            if (manager.stateSettings[i].state == HexState.Hovered)
            {
                var s = manager.stateSettings[i];
                s.visuals.color = Color.cyan;
                manager.stateSettings[i] = s;
                break;
            }
        }
        
        manager.SendMessage("OnValidate");
        yield return null;
        Assert.AreEqual(Color.cyan, manager.GetHexRimColor(testHex), "Changing visualizer settings should update active highlights.");
    }

    [UnityTest]
    public IEnumerator UpdateSettings_ImmediatelyReflectedInActiveSelection()
    {
        pathfindingTool.SetSource(testHex);
        yield return null;

        for (int i = 0; i < manager.stateSettings.Count; i++)
        {
            if (manager.stateSettings[i].state == HexState.Selected)
            {
                var s = manager.stateSettings[i];
                s.visuals.color = Color.blue;
                manager.stateSettings[i] = s;
                break;
            }
        }
        
        manager.SendMessage("OnValidate");
        yield return null;
        Assert.AreEqual(Color.blue, manager.GetHexRimColor(testHex), "Changing visualizer settings should update active selections.");
    }
    
    [UnityTest]
    public IEnumerator UpdateSettings_ImmediatelyReflectedInDefaultHex()
    {
        toolManager.ManualUpdate(null);
        yield return null;

        Color newDefaultColor = Color.magenta;
        
        for (int i = 0; i < manager.stateSettings.Count; i++)
        {
            if (manager.stateSettings[i].state == HexState.Default)
            {
                var s = manager.stateSettings[i];
                s.visuals.color = newDefaultColor;
                manager.stateSettings[i] = s;
                break;
            }
        }

        manager.SendMessage("OnValidate");
        yield return null;
        Assert.AreEqual(newDefaultColor, manager.GetHexRimColor(testHex), "Neutral hexes should reflect Default setting changes.");
    }
}
}