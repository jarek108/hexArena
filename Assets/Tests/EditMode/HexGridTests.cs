using NUnit.Framework;
using UnityEngine;
using HexGame;

public class HexGridTests
{
    private HexGrid grid;

    [SetUp]
    public void Setup()
    {
        grid = new HexGrid(10, 10);
    }

    [Test]
    public void HexGrid_InitializesWithCorrectDimensions()
    {
        Assert.AreEqual(10, grid.Width);
        Assert.AreEqual(10, grid.Height);
    }

    [Test]
    public void HexGrid_AddAndGet_StoresData()
    {
        HexData data = new HexData(2, 3);
        grid.AddHex(data);
        
        HexData retrieved = grid.GetHexAt(2, 3);
        Assert.AreEqual(data, retrieved);
    }
    
    [Test]
    public void HexGrid_GetNeighbors_ReturnsCorrectNeighbors()
    {
        // 0,0
        HexData center = new HexData(0, 0);
        grid.AddHex(center);
        
        // Neighbor at 1, 0 (East)
        HexData neighbor = new HexData(1, 0);
        grid.AddHex(neighbor);
        
        var neighbors = grid.GetNeighbors(center);
        
        Assert.IsTrue(neighbors.Contains(neighbor));
        Assert.AreEqual(1, neighbors.Count); // Should only find the one we added
    }
}
