using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HexGame;

public class HighlightingTests
{
    private GameObject managerGO;
    private HexGridManager manager;
    private GridCreator creator;
    private SelectionTool selectionTool;
    private Hex testHex;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        managerGO = new GameObject("HexGridManager");
        manager = managerGO.AddComponent<HexGridManager>();
        creator = managerGO.AddComponent<GridCreator>();
        selectionTool = managerGO.AddComponent<SelectionTool>();
        
        creator.Initialize(manager);
        selectionTool.Initialize(manager);
        
        yield return null;
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
        MaterialPropertyBlock blockA = new MaterialPropertyBlock();
        hexA.GetComponent<Renderer>().GetPropertyBlock(blockA, 0);
        Assert.AreEqual(selectionTool.selectionRimSettings.color, blockA.GetColor("_RimColor"), "Pre-condition: Hex A should be visually selected.");
        selectionTool.ResetHex(hexA);
        selectionTool.HighlightHex(hexB);
        yield return null;
        hexA.GetComponent<Renderer>().GetPropertyBlock(blockA, 0);
        Assert.AreEqual(selectionTool.selectionRimSettings.color, blockA.GetColor("_RimColor"), "Hex A should remain visually selected after hovering off it.");
    }
    
    [UnityTest]
    public IEnumerator Transition_SelectToDeselect_UpdatesProperties()
    {
        // 1. Select
        selectionTool.SelectHex(testHex);
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        testHex.GetComponent<Renderer>().GetPropertyBlock(block, 0);
        Assert.Greater(block.GetFloat("_RimWidth"), 0f);

        // 2. Deselect
        selectionTool.DeselectHex(testHex);
        testHex.GetComponent<Renderer>().GetPropertyBlock(block, 0);
        Assert.Less(block.GetFloat("_RimWidth"), 0f);

        yield return null;
    }

    [UnityTest]
    public IEnumerator HighlightHex_ActivatesRim()
    {
        selectionTool.HighlightHex(testHex);
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        testHex.GetComponent<Renderer>().GetPropertyBlock(block, 0);
        Color rimColor = block.GetColor("_RimColor");
        float rimWidth = block.GetFloat("_RimWidth");
        float rimPulsationSpeed = block.GetFloat("_RimPulsationSpeed");
        Assert.AreEqual(selectionTool.highlightRimSettings.color, rimColor, "Highlight should set RimColor to Yellow.");
        Assert.Greater(rimWidth, 0f, "Highlight should set a positive RimWidth.");
        Assert.Greater(rimPulsationSpeed, 0f, "Highlight should set a positive RimPulsationSpeed.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator SelectHex_ActivatesRim_WithDifferentSettings()
    {
        selectionTool.SelectHex(testHex);
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        testHex.GetComponent<Renderer>().GetPropertyBlock(block, 0);
        Color rimColor = block.GetColor("_RimColor");
        float rimWidth = block.GetFloat("_RimWidth");
        float rimPulsationSpeed = block.GetFloat("_RimPulsationSpeed");
        Assert.AreEqual(selectionTool.selectionRimSettings.color, rimColor, "Selection should set RimColor to Red.");
        Assert.Greater(rimWidth, 0f, "Selection should set a positive RimWidth.");
        Assert.AreEqual(selectionTool.selectionRimSettings.pulsation, rimPulsationSpeed, "Selection should set RimPulsationSpeed to expected value.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator ResetHex_DeactivatesRim()
    {
        selectionTool.HighlightHex(testHex);
        selectionTool.ResetHex(testHex);
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        testHex.GetComponent<Renderer>().GetPropertyBlock(block, 0);
        float rimWidth = block.GetFloat("_RimWidth");
        Color rimColor = block.GetColor("_RimColor");
        Assert.Less(rimWidth, 0f, "Reset should set RimWidth to a negative value (hiding it).");
        Assert.AreEqual(manager.defaultRimSettings.color, rimColor, "Reset should set RimColor to Black (default).");
        yield return null;
    }

    [UnityTest]
    public IEnumerator Transition_HighlightToSelect_UpdatesProperties()
    {
        selectionTool.HighlightHex(testHex);
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        testHex.GetComponent<Renderer>().GetPropertyBlock(block, 0);
        Assert.AreEqual(selectionTool.highlightRimSettings.color, block.GetColor("_RimColor"));
        selectionTool.SelectHex(testHex);
        testHex.GetComponent<Renderer>().GetPropertyBlock(block, 0);
        Assert.AreEqual(selectionTool.selectionRimSettings.color, block.GetColor("_RimColor"));
        yield return null;
    }

    [UnityTest]
    public IEnumerator SwitchHighlight_ResetsOldHex()
    {
        HexData dataA = manager.Grid.GetHexAt(0, 0);
        HexData dataB = manager.Grid.GetHexAt(1, 0);
        Hex hexA = manager.GetHexView(dataA);
        Hex hexB = manager.GetHexView(dataB);
        selectionTool.HighlightHex(hexA);
        selectionTool.HighlightHex(hexB);
        MaterialPropertyBlock blockA = new MaterialPropertyBlock();
        hexA.GetComponent<Renderer>().GetPropertyBlock(blockA, 0);
        MaterialPropertyBlock blockB = new MaterialPropertyBlock();
        hexB.GetComponent<Renderer>().GetPropertyBlock(blockB, 0);
        Assert.Less(blockA.GetFloat("_RimWidth"), 0f, "Old Hex A should be reset.");
        Assert.AreEqual(selectionTool.highlightRimSettings.color, blockB.GetColor("_RimColor"), "New Hex B should be highlighted.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator SwitchSelection_ResetsOldHex()
    {
        HexData dataA = manager.Grid.GetHexAt(0, 0);
        HexData dataB = manager.Grid.GetHexAt(1, 0);
        Hex hexA = manager.GetHexView(dataA);
        Hex hexB = manager.GetHexView(dataB);
        selectionTool.SelectHex(hexA);
        selectionTool.SelectHex(hexB);
        MaterialPropertyBlock blockA = new MaterialPropertyBlock();
        hexA.GetComponent<Renderer>().GetPropertyBlock(blockA, 0);
        MaterialPropertyBlock blockB = new MaterialPropertyBlock();
        hexB.GetComponent<Renderer>().GetPropertyBlock(blockB, 0);
        Assert.Less(blockA.GetFloat("_RimWidth"), 0f, "Old Hex A should be reset.");
        Assert.AreEqual(selectionTool.selectionRimSettings.color, blockB.GetColor("_RimColor"), "New Hex B should be selected.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator Highlight_OnSelectedHex_Ignored()
    {
        selectionTool.SelectHex(testHex);
        selectionTool.HighlightHex(testHex);
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        testHex.GetComponent<Renderer>().GetPropertyBlock(block, 0);
        Assert.AreEqual(selectionTool.selectionRimSettings.color, block.GetColor("_RimColor"), "Highlighting a Selected hex should be ignored (remain Red).");
        yield return null;
    }

    [UnityTest]
    public IEnumerator HighlightHex_SetsPulsationSpeed()
    {
        selectionTool.HighlightHex(testHex);
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        testHex.GetComponent<Renderer>().GetPropertyBlock(block, 0);
        float rimPulsationSpeed = block.GetFloat("_RimPulsationSpeed");
        Assert.AreEqual(selectionTool.highlightRimSettings.pulsation, rimPulsationSpeed, "Highlight should set RimPulsationSpeed to the value in highlightRimSettings.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator SelectHex_SetsPulsationSpeed()
    {
        selectionTool.SelectHex(testHex);
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        testHex.GetComponent<Renderer>().GetPropertyBlock(block, 0);
        float rimPulsationSpeed = block.GetFloat("_RimPulsationSpeed");
        Assert.AreEqual(selectionTool.selectionRimSettings.pulsation, rimPulsationSpeed, "Selection should set RimPulsationSpeed to the value in selectionRimSettings.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator ResetHex_SetsPulsationSpeed()
    {
        selectionTool.HighlightHex(testHex);
        selectionTool.ResetHex(testHex);
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        testHex.GetComponent<Renderer>().GetPropertyBlock(block, 0);
        float rimPulsationSpeed = block.GetFloat("_RimPulsationSpeed");
        Assert.AreEqual(manager.defaultRimSettings.pulsation, rimPulsationSpeed, "Reset should set RimPulsationSpeed to the value in defaultRimSettings.");
        yield return null;
    }
    
    [UnityTest]
    public IEnumerator UpdateSettings_ImmediatelyReflectedInActiveHighlight()
    {
        selectionTool.HighlightHex(testHex);
        Color newHighlightColor = Color.cyan;
        selectionTool.highlightRimSettings.color = newHighlightColor;
        selectionTool.RefreshVisuals();
        yield return null;
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        testHex.GetComponent<Renderer>().GetPropertyBlock(block, 0);
        Color actualColor = block.GetColor("_RimColor");
        Assert.AreEqual(newHighlightColor, actualColor, "Changing manager highlight settings should immediately update active highlights.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator UpdateSettings_ImmediatelyReflectedInActiveSelection()
    {
        selectionTool.SelectHex(testHex);
        Color newSelectionColor = Color.blue;
        selectionTool.selectionRimSettings.color = newSelectionColor;
        selectionTool.RefreshVisuals();
        yield return null;
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        testHex.GetComponent<Renderer>().GetPropertyBlock(block, 0);
        Color actualColor = block.GetColor("_RimColor");
        Assert.AreEqual(newSelectionColor, actualColor, "Changing manager selection settings should immediately update active selections.");
        yield return null;
    }
    
    [UnityTest]
    public IEnumerator UpdateSettings_ImmediatelyReflectedInDefaultHex()
    {
        selectionTool.ResetHex(testHex);
        Color newDefaultColor = Color.magenta;
        manager.defaultRimSettings.color = newDefaultColor;
        manager.RefreshVisuals();
        yield return null;
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        testHex.GetComponent<Renderer>().GetPropertyBlock(block, 0);
        Assert.AreEqual(newDefaultColor, block.GetColor("_RimColor"), "Changing manager default settings should immediately update neutral hexes.");
        yield return null;
    }
}