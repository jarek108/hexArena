using NUnit.Framework;
using UnityEngine;
using HexGame;

[TestFixture]
public class UnitPlacementTests
{
    private GameObject unitManagerGO;
    private UnitManager unitManager;
    private GameObject hexGO;
    private Hex hex;
    private GameObject unitGO;
    private Unit unit;

    [SetUp]
    public void SetUp()
    {
        unitManagerGO = new GameObject("UnitManager");
        unitManager = unitManagerGO.AddComponent<UnitManager>();
        unitManager.activeUnitSetPath = ""; // Prevent loading real data

        hexGO = new GameObject("TestHex");
        hex = hexGO.AddComponent<Hex>();
        
        // Correctly initialize with Data
        HexData data = new HexData(0, 0);
        hex.AssignData(data);

        // Create a dummy UnitSet for initialization
        var testSet = ScriptableObject.CreateInstance<HexGame.Units.UnitSet>();
        testSet.units.Add(new HexGame.Units.UnitType { Name = "Test" });
        unitManager.ActiveUnitSet = testSet;

        unitGO = new GameObject("TestUnit");
        unit = unitGO.AddComponent<Unit>();
        unit.Initialize(0, 0);
    }

    [TearDown]
    public void TearDown()
    {
        if (unitManager != null && unitManager.ActiveUnitSet != null) Object.DestroyImmediate(unitManager.ActiveUnitSet);
        Object.DestroyImmediate(unitManagerGO);
        Object.DestroyImmediate(hexGO);
        Object.DestroyImmediate(unitGO);
    }

    [Test]
    public void Unit_SetHex_OccupiesHex()
    {
        unit.SetHex(hex);

        Assert.AreEqual(hex, unit.CurrentHex);
        Assert.AreEqual(unit, hex.Unit);
    }

    [Test]
    public void Unit_SetHex_UpdatesPosition()
    {
        hex.transform.position = new Vector3(10, 5, 10);
        unit.SetHex(hex);

        Assert.AreEqual(hex.transform.position, unit.transform.position);
    }

    [Test]
    public void Unit_SetHex_ClearsPreviousHex()
    {
        GameObject hex2GO = new GameObject("TestHex2");
        Hex hex2 = hex2GO.AddComponent<Hex>();
        HexData data2 = new HexData(1, 0);
        hex2.AssignData(data2);
        
        unit.SetHex(hex);
        Assert.AreEqual(unit, hex.Unit);

        unit.SetHex(hex2);
        
        Assert.IsNull(hex.Unit);
        Assert.AreEqual(hex2, unit.CurrentHex);
        Assert.AreEqual(unit, hex2.Unit);

        Object.DestroyImmediate(hex2GO);
    }

    [Test]
    public void Unit_Repositioned_On_Hex_Elevation_Change()
    {
        // Arrange
        unit.SetHex(hex);
        float offset = 0.5f;
        var viz = unitGO.AddComponent<HexGame.Units.SimpleUnitVisualization>();
        viz.yOffset = offset;
        unit.Initialize(0, 0); // Re-init to pick up visualization

        // Act
        float newElevation = 5f;
        hex.Elevation = newElevation;

        // Assert
        Vector3 expectedPos = hex.transform.position;
        expectedPos.y += offset;
        Assert.AreEqual(expectedPos, unit.transform.position, "Unit should move to new elevation + offset.");
    }
}
