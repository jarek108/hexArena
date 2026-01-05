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
        public void ScrollUp_WithCtrl_IncreasesBrushSize()
        {
            // Arrange
            brushTool.sizeControlKey = BrushTool.ModifierKey.Ctrl;
            brushTool.maxBrushSize = 10;
            brushTool.brushSize = 1;
            var mouse = InputSystem.AddDevice<Mouse>();
            var keyboard = InputSystem.AddDevice<Keyboard>();

            // Act
            Press(keyboard.ctrlKey);
            Set(mouse.scroll, new Vector2(0, 1));
            brushTool.HandleInput(null);

            // Assert
            Assert.AreEqual(2, brushTool.brushSize);
        }

        [Test]
        public void ScrollUp_WithoutRequiredCtrl_DoesNotChangeSize()
        {
            // Arrange
            brushTool.sizeControlKey = BrushTool.ModifierKey.Ctrl;
            brushTool.brushSize = 1;
            var mouse = InputSystem.AddDevice<Mouse>();
            var keyboard = InputSystem.AddDevice<Keyboard>();

            // Act
            // Ctrl NOT pressed
            Set(mouse.scroll, new Vector2(0, 1));
            brushTool.HandleInput(null);

            // Assert
            Assert.AreEqual(1, brushTool.brushSize);
        }

        [Test]
        public void ScrollDown_WithCtrl_DecreasesBrushSize()
        {
            // Arrange
            brushTool.sizeControlKey = BrushTool.ModifierKey.Ctrl;
            brushTool.brushSize = 3;
            var mouse = InputSystem.AddDevice<Mouse>();
            var keyboard = InputSystem.AddDevice<Keyboard>();

            // Act
            Press(keyboard.ctrlKey);
            Set(mouse.scroll, new Vector2(0, -1));
            brushTool.HandleInput(null);

            // Assert
            Assert.AreEqual(2, brushTool.brushSize);
        }

        [Test]
        public void BrushSize_IsClamped_BetweenOneAndMax()
        {
            // Arrange
            brushTool.sizeControlKey = BrushTool.ModifierKey.None; // No modifier for simplicity
            brushTool.maxBrushSize = 6;
            brushTool.brushSize = 6;
            var mouse = InputSystem.AddDevice<Mouse>();

            // Act: Scroll Up at Max
            Set(mouse.scroll, new Vector2(0, 1));
            brushTool.HandleInput(null);
            Assert.AreEqual(6, brushTool.brushSize);

            // Act: Scroll Down at Min
            brushTool.brushSize = 1;
            Set(mouse.scroll, new Vector2(0, -1));
            brushTool.HandleInput(null);
            Assert.AreEqual(1, brushTool.brushSize);
        }
    }
}
