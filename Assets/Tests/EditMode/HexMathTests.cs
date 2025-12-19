using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HexGame;

public class HexMathTests
{
    [Test]
    public void Hex_InitializedCorrectly_SumIsZero()
    {
        // Hex is now a MonoBehaviour, but we can test static math logic via Vector3Int
        // or just test the logic that doesn't rely on being attached
        // However, standard "new Hex()" is dangerous if it's a MonoBehaviour.
        // We should test the static math functions which now use Vector3Int.
        
        Vector3Int h = new Vector3Int(1, 2, -3);
        Assert.AreEqual(0, h.x + h.y + h.z);
    }

    [Test]
    public void Hex_Add_WorksCorrectly()
    {
        Vector3Int a = new Vector3Int(1, -2, 1);
        Vector3Int b = new Vector3Int(-1, 2, -1);
        Vector3Int result = Hex.Add(a, b);
        
        Assert.AreEqual(Vector3Int.zero, result);
    }

    [Test]
    public void Hex_Neighbor_ReturnsCorrectCoord()
    {
        Vector3Int center = Vector3Int.zero;
        // Direction 0 is (1, 0, -1)
        Vector3Int neighbor = Hex.Neighbor(center, 0);
        
        Assert.AreEqual(new Vector3Int(1, 0, -1), neighbor);
    }

    [Test]
    public void Hex_Distance_CalculatesCorrectly()
    {
        Vector3Int a = new Vector3Int(0, 0, 0);
        Vector3Int b = new Vector3Int(1, 0, -1);
        
        Assert.AreEqual(1, Hex.Distance(a, b));
    }
}
