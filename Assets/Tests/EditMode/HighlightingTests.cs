using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HexGame;
using HexGame.Tools;
using HexGame.Tests;
using System.Collections.Generic;

public class HighlightingTests
{
    private GameObject managerGO;
    private HexGridManager manager;
    private GridCreator creator;
    private SelectionTool selectionTool;
    private ToolManager toolManager;
    private HexStateVisualizer visualizer;
    private SelectionManager selectionManager;
    private Hex testHex;

    private HexGridManager.RimSettings highlightVisuals = new HexGridManager.RimSettings { color = Color.yellow, width = 0.2f, pulsation = 5f };
    private HexGridManager.RimSettings selectionVisuals = new HexGridManager.RimSettings { color = Color.red, width = 0.2f, pulsation = 2f };
    private HexGridManager.RimSettings defaultVisuals = new HexGridManager.RimSettings { color = Color.black, width = 0f, pulsation = 0f };

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        managerGO = TestHelper.CreateTestManager();
        manager = managerGO.GetComponent<HexGridManager>();
        creator = managerGO.GetComponent<GridCreator>();
        toolManager = managerGO.GetComponent<ToolManager>();
        selectionTool = managerGO.GetComponent<SelectionTool>();
        visualizer = managerGO.GetComponent<HexStateVisualizer>();
        selectionManager = managerGO.GetComponent<SelectionManager>();

        // Setup visualizer with standard settings for tests
        visualizer.stateSettings = new List<HexStateVisualizer.StateSetting>
        {
            new HexStateVisualizer.StateSetting { state = HexState.Default, priority = 0, visuals = defaultVisuals },
            new HexStateVisualizer.StateSetting { state = HexState.Hovered, priority = 10, visuals = highlightVisuals },
            new HexStateVisualizer.StateSetting { state = HexState.Selected, priority = 20, visuals = selectionVisuals }
        };
        
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
        Hex hexA = manager.GetHexView(manager.Grid.GetHexAt(1, 1));
        Hex hexB = manager.GetHexView(manager.Grid.GetHexAt(2, 2));
        selectionTool.SelectHex(hexA);
        yield return null;

        MaterialPropertyBlock blockA = new MaterialPropertyBlock();
        hexA.GetComponent<Renderer>().GetPropertyBlock(blockA, 0);
        Assert.AreEqual(selectionVisuals.color, blockA.GetColor("_RimColor"), "Pre-condition: Hex A should be visually selected.");
        
        selectionManager.ManualUpdate(hexB);
        yield return null;
        
        hexA.GetComponent<Renderer>().GetPropertyBlock(blockA, 0);
        Assert.AreEqual(selectionVisuals.color, blockA.GetColor("_RimColor"), "Hex A should remain visually selected after hovering off it.");
    }
    
    [UnityTest]
    public IEnumerator Transition_SelectToDeselect_UpdatesProperties()
    {
        selectionTool.SelectHex(testHex);
        yield return null;
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        testHex.GetComponent<Renderer>().GetPropertyBlock(block, 0);
        Assert.Greater(block.GetFloat("_RimWidth"), 0f);

        selectionTool.DeselectHex(testHex);
        yield return null;
        testHex.GetComponent<Renderer>().GetPropertyBlock(block, 0);
        Assert.AreEqual(0, block.GetFloat("_RimWidth"));
    }

    [UnityTest]
    public IEnumerator HighlightHex_ActivatesRim()
    {
        selectionManager.ManualUpdate(testHex);
        yield return null;
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        testHex.GetComponent<Renderer>().GetPropertyBlock(block, 0);
        Color rimColor = block.GetColor("_RimColor");
        float rimWidth = block.GetFloat("_RimWidth");
        float rimPulsationSpeed = block.GetFloat("_RimPulsationSpeed");
        
        Assert.AreEqual(highlightVisuals.color, rimColor, "Highlight should set RimColor to Yellow.");
        Assert.Greater(rimWidth, 0f, "Highlight should set a positive RimWidth.");
        Assert.Greater(rimPulsationSpeed, 0f, "Highlight should set a positive RimPulsationSpeed.");
    }

    [UnityTest]
    public IEnumerator SelectHex_ActivatesRim_WithDifferentSettings()
    {
        selectionTool.SelectHex(testHex);
        yield return null;
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        testHex.GetComponent<Renderer>().GetPropertyBlock(block, 0);
        Color rimColor = block.GetColor("_RimColor");
        float rimWidth = block.GetFloat("_RimWidth");
        float rimPulsationSpeed = block.GetFloat("_RimPulsationSpeed");
        
        Assert.AreEqual(selectionVisuals.color, rimColor, "Selection should set RimColor to Red.");
        Assert.Greater(rimWidth, 0f, "Selection should set a positive RimWidth.");
        Assert.AreEqual(selectionVisuals.pulsation, rimPulsationSpeed, "Selection should set RimPulsationSpeed to expected value.");
    }

    [UnityTest]
    public IEnumerator ResetHex_DeactivatesRim()
    {
        selectionManager.ManualUpdate(testHex);
        yield return null;
        selectionManager.ManualUpdate(null); // Simulate hovering off
        yield return null;
        
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        testHex.GetComponent<Renderer>().GetPropertyBlock(block, 0);
        float rimWidth = block.GetFloat("_RimWidth");
        Color rimColor = block.GetColor("_RimColor");
        
        Assert.AreEqual(0, rimWidth, "Reset should set RimWidth to a zero value (hiding it).");
        Assert.AreEqual(defaultVisuals.color, rimColor, "Reset should set RimColor to Black (default).");
    }

    [UnityTest]
    public IEnumerator Transition_HighlightToSelect_UpdatesProperties()
    {
        selectionManager.ManualUpdate(testHex);
        yield return null;
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        testHex.GetComponent<Renderer>().GetPropertyBlock(block, 0);
        Assert.AreEqual(highlightVisuals.color, block.GetColor("_RimColor"));
        
        selectionTool.SelectHex(testHex);
        yield return null;
        testHex.GetComponent<Renderer>().GetPropertyBlock(block, 0);
        Assert.AreEqual(selectionVisuals.color, block.GetColor("_RimColor"));
    }

    [UnityTest]
    public IEnumerator SwitchHighlight_ResetsOldHex()
    {
        Hex hexA = manager.GetHexView(manager.Grid.GetHexAt(0, 0));
        Hex hexB = manager.GetHexView(manager.Grid.GetHexAt(1, 0));
        
        selectionManager.ManualUpdate(hexA);
        yield return null;
        selectionManager.ManualUpdate(hexB);
        yield return null;
        
        MaterialPropertyBlock blockA = new MaterialPropertyBlock();
        hexA.GetComponent<Renderer>().GetPropertyBlock(blockA, 0);
        MaterialPropertyBlock blockB = new MaterialPropertyBlock();
        hexB.GetComponent<Renderer>().GetPropertyBlock(blockB, 0);
        
        Assert.AreEqual(0, blockA.GetFloat("_RimWidth"), "Old Hex A should be reset.");
        Assert.AreEqual(highlightVisuals.color, blockB.GetColor("_RimColor"), "New Hex B should be highlighted.");
    }

    [UnityTest]
    public IEnumerator SwitchSelection_ResetsOldHex()
    {
        Hex hexA = manager.GetHexView(manager.Grid.GetHexAt(0, 0));
        Hex hexB = manager.GetHexView(manager.Grid.GetHexAt(1, 0));
        
        selectionTool.SelectHex(hexA);
        yield return null;
        selectionTool.SelectHex(hexB);
        yield return null;
        
        MaterialPropertyBlock blockA = new MaterialPropertyBlock();
        hexA.GetComponent<Renderer>().GetPropertyBlock(blockA, 0);
        MaterialPropertyBlock blockB = new MaterialPropertyBlock();
        hexB.GetComponent<Renderer>().GetPropertyBlock(blockB, 0);
        
        Assert.AreEqual(0, blockA.GetFloat("_RimWidth"), "Old Hex A should be reset.");
        Assert.AreEqual(selectionVisuals.color, blockB.GetColor("_RimColor"), "New Hex B should be selected.");
    }

    [UnityTest]
    public IEnumerator Highlight_OnSelectedHex_Ignored()
    {
        selectionTool.SelectHex(testHex);
        yield return null;
        selectionManager.ManualUpdate(testHex);
        yield return null;
        
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        testHex.GetComponent<Renderer>().GetPropertyBlock(block, 0);
        Assert.AreEqual(selectionVisuals.color, block.GetColor("_RimColor"), "Highlighting a Selected hex should be ignored (remain Red).");
    }

    [UnityTest]
    public IEnumerator HighlightHex_SetsPulsationSpeed()
    {
        selectionManager.ManualUpdate(testHex);
        yield return null;
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        testHex.GetComponent<Renderer>().GetPropertyBlock(block, 0);
        float rimPulsationSpeed = block.GetFloat("_RimPulsationSpeed");
        Assert.AreEqual(highlightVisuals.pulsation, rimPulsationSpeed, "Highlight should set RimPulsationSpeed to expected value.");
    }

    [UnityTest]
    public IEnumerator SelectHex_SetsPulsationSpeed()
    {
        selectionTool.SelectHex(testHex);
        yield return null;
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        testHex.GetComponent<Renderer>().GetPropertyBlock(block, 0);
        float rimPulsationSpeed = block.GetFloat("_RimPulsationSpeed");
        Assert.AreEqual(selectionVisuals.pulsation, rimPulsationSpeed, "Selection should set RimPulsationSpeed to expected value.");
    }

    [UnityTest]
    public IEnumerator ResetHex_SetsPulsationSpeed()
    {
        selectionManager.ManualUpdate(testHex);
        yield return null;
        selectionManager.ManualUpdate(null);
        yield return null;
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        testHex.GetComponent<Renderer>().GetPropertyBlock(block, 0);
        float rimPulsationSpeed = block.GetFloat("_RimPulsationSpeed");
        Assert.AreEqual(defaultVisuals.pulsation, rimPulsationSpeed, "Reset should set RimPulsationSpeed to Default value.");
    }
    
    [UnityTest]
    public IEnumerator UpdateSettings_ImmediatelyReflectedInActiveHighlight()
    {
        selectionManager.ManualUpdate(testHex);
        yield return null;
        
        for (int i = 0; i < visualizer.stateSettings.Count; i++)
        {
            if (visualizer.stateSettings[i].state == HexState.Hovered)
            {
                var s = visualizer.stateSettings[i];
                s.visuals.color = Color.cyan;
                visualizer.stateSettings[i] = s;
                break;
            }
        }
        
        visualizer.SendMessage("OnValidate");
        yield return null;
        
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        testHex.GetComponent<Renderer>().GetPropertyBlock(block, 0);
        Assert.AreEqual(Color.cyan, block.GetColor("_RimColor"), "Changing visualizer settings should update active highlights.");
    }

    [UnityTest]
    public IEnumerator UpdateSettings_ImmediatelyReflectedInActiveSelection()
    {
        selectionTool.SelectHex(testHex);
        yield return null;

        for (int i = 0; i < visualizer.stateSettings.Count; i++)
        {
            if (visualizer.stateSettings[i].state == HexState.Selected)
            {
                var s = visualizer.stateSettings[i];
                s.visuals.color = Color.blue;
                visualizer.stateSettings[i] = s;
                break;
            }
        }
        
        visualizer.SendMessage("OnValidate");
        yield return null;
        
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        testHex.GetComponent<Renderer>().GetPropertyBlock(block, 0);
        Assert.AreEqual(Color.blue, block.GetColor("_RimColor"), "Changing visualizer settings should update active selections.");
    }
    
    [UnityTest]
    public IEnumerator UpdateSettings_ImmediatelyReflectedInDefaultHex()
    {
        selectionManager.ManualUpdate(null);
        yield return null;

        Color newDefaultColor = Color.magenta;
        
        for (int i = 0; i < visualizer.stateSettings.Count; i++)
        {
            if (visualizer.stateSettings[i].state == HexState.Default)
            {
                var s = visualizer.stateSettings[i];
                s.visuals.color = newDefaultColor;
                visualizer.stateSettings[i] = s;
                break;
            }
        }

        visualizer.SendMessage("OnValidate");
        yield return null;
        
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        testHex.GetComponent<Renderer>().GetPropertyBlock(block, 0);
        Assert.AreEqual(newDefaultColor, block.GetColor("_RimColor"), "Neutral hexes should reflect Default setting changes.");
    }
}