using NUnit.Framework;
using UnityEngine;
using HexGame;

[TestFixture]
public class UnitPlacementTests
{
    private GameObject hexGO;
    private Hex hex;
    private GameObject unitGO;
    private Unit unit;

    [SetUp]
    public void SetUp()
    {
        hexGO = new GameObject("TestHex");
        hex = hexGO.AddComponent<Hex>();
        
        // Correctly initialize with Data
        HexData data = new HexData(0, 0);
        hex.AssignData(data);

        // Create a dummy UnitSet for initialization
        var testSet = ScriptableObject.CreateInstance<HexGame.Units.UnitSet>();
        testSet.units.Add(new HexGame.Units.UnitType { Name = "Test" });

        unitGO = new GameObject("TestUnit");
        unit = unitGO.AddComponent<Unit>();
        unit.Initialize(testSet, 0, 0);
    }

    [TearDown]
    public void TearDown()
    {
        if (unit != null && unit.unitSet != null) Object.DestroyImmediate(unit.unitSet);
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
}
