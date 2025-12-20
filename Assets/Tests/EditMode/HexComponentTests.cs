using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HexGame;

public class HexComponentTests
{
    private GameObject managerGO;
    private HexGridManager manager;
    private GridCreator creator;
    private Hex testHex;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        managerGO = new GameObject("HexGridManager");
        manager = managerGO.AddComponent<HexGridManager>();
        creator = managerGO.AddComponent<GridCreator>();
        creator.Initialize(manager);
        yield return null;
        creator.GenerateGrid();
        
        // Wait a few frames to ensure all components (especially Renderers) are initialized
        yield return null; 
        yield return null; 
        yield return null;

        // Get a hex to test
        HexData data = manager.Grid.GetHexAt(0, 0);
        Assert.IsNotNull(data, "Test hex data should not be null.");
        
        testHex = manager.GetHexView(data);
        Assert.IsNotNull(testHex, "Test hex view should not be null.");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Object.DestroyImmediate(managerGO);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Hex_Visuals_Update_On_Terrain_Change()
    {
        // Initial color for Plains (default)
        MaterialPropertyBlock initialBlock = new MaterialPropertyBlock();
        testHex.GetComponent<Renderer>().GetPropertyBlock(initialBlock, 0);
        Color initialColor = initialBlock.GetColor("_BaseColor");

        // Change terrain type to Water (assuming Water is blue in mapping)
        testHex.TerrainType = TerrainType.Water;
        yield return null; // Allow OnValidate to run and renderer to update

        // Get the new color from the hex
        MaterialPropertyBlock finalBlock = new MaterialPropertyBlock();
        testHex.GetComponent<Renderer>().GetPropertyBlock(finalBlock, 0);
        Color finalColor = finalBlock.GetColor("_BaseColor");

        // Get the expected color from the manager's mapping
        Color expectedColor = manager.GetDefaultHexColor(testHex); // This should be blue

        Assert.AreEqual(expectedColor, finalColor, "Hex color should update to new terrain type color.");
        Assert.AreNotEqual(initialColor, finalColor, "Hex color should change from initial color.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator Hex_Position_Updates_On_Elevation_Change()
    {
        // Get initial position
        Vector3 initialPosition = testHex.transform.position;
        float initialY = initialPosition.y;

        // Change elevation
        float newElevation = initialY + 5f;
        testHex.Elevation = newElevation;
        yield return null; // Allow OnValidate to run in editor

        // Get new position
        Vector3 finalPosition = testHex.transform.position;
        float finalY = finalPosition.y;

        Assert.AreEqual(newElevation, finalY, 0.001f, "Hex Y position should update to new elevation.");
        Assert.AreNotEqual(initialY, finalY, "Hex Y position should change from initial Y.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator HexGridManager_InitializesHexBaseColorCorrectly()
    {
        // Get a hex from the grid (testHex is already set up in SetUp)
        // Get its current _BaseColor from the MaterialPropertyBlock
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        testHex.GetComponent<Renderer>().GetPropertyBlock(block, 0);
        Color actualColor = block.GetColor("_BaseColor");

        // Get the expected default color for that hex's TerrainType
        Color expectedColor = manager.GetDefaultHexColor(testHex);

        // Assert that these two colors are equal
        Assert.AreEqual(expectedColor, actualColor, "Initial hex base color should match its terrain type.");
        yield return null;
    }
}