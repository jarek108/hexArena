using NUnit.Framework;
using UnityEngine;
using HexGame.Tools;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace HexGame.Tests
{
    public class BrushToolTests : InputTestFixture
    {
        private class MockBrushTool : BrushTool
        {
            public int GetBrushSize() => brushSize;
            public void SetBrushSize(int size) => brushSize = size;
            public void SetMaxBrushSize(int size) => maxBrushSize = size;
        }

        private GameObject toolGO;
        private MockBrushTool brushTool;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            toolGO = new GameObject("BrushTool");
            brushTool = toolGO.AddComponent<MockBrushTool>();
            brushTool.IsEnabled = true;
        }

        [TearDown]
        public override void TearDown()
        {
            Object.DestroyImmediate(toolGO);
            base.TearDown();
        }

        [Test]
        public void ScrollUp_IncreasesBrushSize()
        {
            // Arrange
            brushTool.SetMaxBrushSize(10);
            brushTool.SetBrushSize(1);
            var mouse = InputSystem.AddDevice<Mouse>();

            // Act
            Set(mouse.scroll, new Vector2(0, 1));
            brushTool.HandleInput(null);

            // Assert
            Assert.AreEqual(2, brushTool.GetBrushSize());
        }

        [Test]
        public void ScrollDown_DecreasesBrushSize()
        {
            // Arrange
            brushTool.SetBrushSize(3);
            var mouse = InputSystem.AddDevice<Mouse>();

            // Act
            Set(mouse.scroll, new Vector2(0, -1));
            brushTool.HandleInput(null);

            // Assert
            Assert.AreEqual(2, brushTool.GetBrushSize());
        }

        [Test]
        public void BrushSize_IsClamped_BetweenOneAndMax()
        {
            // Arrange
            brushTool.SetMaxBrushSize(6);
            brushTool.SetBrushSize(6);
            var mouse = InputSystem.AddDevice<Mouse>();

            // Act: Scroll Up at Max
            Set(mouse.scroll, new Vector2(0, 1));
            brushTool.HandleInput(null);
            Assert.AreEqual(6, brushTool.GetBrushSize());

            // Act: Scroll Down at Min
            brushTool.SetBrushSize(1);
            Set(mouse.scroll, new Vector2(0, -1));
            brushTool.HandleInput(null);
            Assert.AreEqual(1, brushTool.GetBrushSize());
        }
    }
}
