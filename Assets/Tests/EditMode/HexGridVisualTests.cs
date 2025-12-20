using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HexGame;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class HexGridVisualTests
{
    private GameObject managerGO;
    private HexGridManager manager;
    private GridCreator creator;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        managerGO = new GameObject("HexGridManager");
        manager = managerGO.AddComponent<HexGridManager>();
        creator = managerGO.AddComponent<GridCreator>();
        
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (Application.isPlaying)
        {
            Object.Destroy(managerGO);
        }
        else
        {
            Object.DestroyImmediate(managerGO);
        }
        yield return null;
    }

    [UnityTest]
    public IEnumerator HexGridManager_GeneratesGrid_SpawnsObjects()
    {
        // Manually trigger generation after setup
        creator.GenerateGrid();
        yield return null;

        Assert.Greater(manager.transform.childCount, 0, "Grid should spawn child objects");
        
        // Check first hex position
        Transform firstHex = manager.transform.GetChild(0);
        Assert.IsNotNull(firstHex);
        
        // Verify components
        Assert.IsNotNull(firstHex.GetComponent<Hex>(), "Hex component missing");
        Assert.IsNotNull(firstHex.GetComponent<MeshFilter>(), "MeshFilter component missing");
        Assert.IsNotNull(firstHex.GetComponent<MeshRenderer>(), "MeshRenderer component missing");
        Assert.IsNotNull(firstHex.GetComponent<MeshCollider>(), "MeshCollider component missing");
    }

    [UnityTest]
    public IEnumerator HexGridManager_RegeneratesAndClearsGrid_Correctly()
    {
        SerializedObject so = new SerializedObject(creator);

        // --- First Generation (5x5) ---
        so.FindProperty("gridWidth").intValue = 5;
        so.FindProperty("gridHeight").intValue = 5;
        so.ApplyModifiedProperties();
        creator.GenerateGrid();
        yield return null;
        Assert.AreEqual(25, manager.transform.childCount, "First grid generation should spawn 25 hexes.");

        // --- Second Generation (3x3) ---
        so.FindProperty("gridWidth").intValue = 3;
        so.FindProperty("gridHeight").intValue = 3;
        so.ApplyModifiedProperties();
        creator.GenerateGrid();
        yield return null;
        Assert.AreEqual(9, manager.transform.childCount, "Second grid generation should spawn 9 hexes.");

        // --- Clear Grid ---
        manager.ClearGrid();
        yield return null;
        Assert.AreEqual(0, manager.transform.childCount, "ClearGrid should remove all hexes.");
    }
}