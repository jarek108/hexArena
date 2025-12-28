using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using HexGame;
using Grid = HexGame.Grid;

namespace HexGame.Tests
{
    [TestFixture]
    public class PathfinderTests
    {
        private Grid grid;

        [SetUp]
        public void SetUp()
        {
            grid = new Grid(10, 10);
            for (int q = 0; q < 10; q++)
            {
                for (int r = 0; r < 10; r++)
                {
                    grid.AddHex(new HexData(q, r));
                }
            }
        }

        [Test]
        public void FindPath_DirectNeighbors_ReturnsTwoHexPath()
        {
            HexData start = grid.GetHexAt(5, 5);
            HexData target = grid.GetHexAt(5, 6);

            PathResult result = Pathfinder.FindPath(grid, null, start, target);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(2, result.Path.Count);
            Assert.AreEqual(start, result.Path[0]);
            Assert.AreEqual(target, result.Path[1]);
            Assert.AreEqual(1f, result.TotalCost);
        }

        [Test]
        public void FindPath_LongDistance_ReturnsCorrectSequence()
        {
            HexData start = grid.GetHexAt(0, 0);
            HexData target = grid.GetHexAt(3, 0); // Straight line along Q

            PathResult result = Pathfinder.FindPath(grid, null, start, target);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(4, result.Path.Count); // 0,0 -> 1,0 -> 2,0 -> 3,0
            Assert.AreEqual(3f, result.TotalCost);
        }

        [Test]
        public void FindPath_StartEqualsTarget_ReturnsSingleHex()
        {
            HexData start = grid.GetHexAt(5, 5);
            PathResult result = Pathfinder.FindPath(grid, null, start, start);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(1, result.Path.Count);
            Assert.AreEqual(0f, result.TotalCost);
        }

        [Test]
        public void FindPath_DisconnectedGrid_ReturnsFailure()
        {
            Grid smallGrid = new Grid(2, 2);
            HexData start = new HexData(0, 0);
            HexData target = new HexData(5, 5); // Target not in grid
            smallGrid.AddHex(start);

            PathResult result = Pathfinder.FindPath(smallGrid, null, start, target);

            Assert.IsFalse(result.Success);
        }

        [Test]
        public void FindPath_HighElevationGap_ReturnsFailure()
        {
            // Setup: 0,0 (0 elev) -> 1,0 (5 elev) -> 2,0 (0 elev)
            grid.GetHexAt(0, 0).Elevation = 0f;
            grid.GetHexAt(1, 0).Elevation = 5f;
            grid.GetHexAt(2, 0).Elevation = 0f;

            // Try to cross the 'wall' at 1,0 with default max change of 1
            PathResult result = Pathfinder.FindPath(grid, null, grid.GetHexAt(0, 0), grid.GetHexAt(2, 0));

            // It should fail or find a much longer way around if available. 
            // In a 10x10 grid, it will find a way around unless we block the whole column.
            
            // To make a definitive test, block the path
            for (int r = 0; r < 10; r++) grid.GetHexAt(1, r).Elevation = 5f;

            result = Pathfinder.FindPath(grid, null, grid.GetHexAt(0, 5), grid.GetHexAt(2, 5));
            Assert.IsFalse(result.Success, "Should not be able to cross a wall of 5 elevation with max change of 1.");
        }
    }
}
