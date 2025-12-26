using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HexGame;
using HexGame.Tools;

namespace HexGame.Tests
{
    // A mock tool for testing the ToolManager
    public class MockTool : MonoBehaviour, ITool
    {
        public bool IsActive { get; private set; }
        public bool CheckRequirements(out string reason) { reason = string.Empty; return true; }
        public void OnActivate() { IsActive = true; }
        public void OnDeactivate() { IsActive = false; }
        public void HandleInput(Hex hoveredHex) { }
    }

    public class MockToolB : MonoBehaviour, ITool
    {
        public bool IsActive { get; private set; }
        public bool CheckRequirements(out string reason) { reason = string.Empty; return true; }
        public void OnActivate() { IsActive = true; }
        public void OnDeactivate() { IsActive = false; }
        public void HandleInput(Hex hoveredHex) { }
    }

    public class MockToggleTool : MonoBehaviour, IToggleTool
    {
        public int ActivationCount { get; private set; }
        public bool CheckRequirements(out string reason) { reason = string.Empty; return true; }
        public void OnActivate() { ActivationCount++; }
        public void OnDeactivate() { }
        public void HandleInput(Hex hoveredHex) { }
    }

    public class MockRestrictedTool : MonoBehaviour, ITool
    {
        public bool ShouldAllow { get; set; } = false;
        public bool CheckRequirements(out string reason) { 
            reason = "Restriction active"; 
            return ShouldAllow; 
        }
        public void OnActivate() { }
        public void OnDeactivate() { }
        public void HandleInput(Hex hoveredHex) { }
    }

    [TestFixture]
    public class ToolsManagerTests
    {
        private ToolManager toolManager;
        private MockTool toolA;
        private MockToolB toolB;
        private MockToggleTool toggleTool;
        private MockRestrictedTool restrictedTool;
        private GameObject managerGO;

        [SetUp]
        public void SetUp()
        {
            managerGO = new GameObject("ToolManager");
            toolManager = managerGO.AddComponent<ToolManager>();
            toolA = managerGO.AddComponent<MockTool>();
            toolB = managerGO.AddComponent<MockToolB>();
            toggleTool = managerGO.AddComponent<MockToggleTool>();
            restrictedTool = managerGO.AddComponent<MockRestrictedTool>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(managerGO);
        }

        [Test]
        public void ToolManager_CanSwitchActiveTool()
        {
            toolManager.SetUpForTesting();

            // Act
            toolManager.SetActiveTool(toolA);

            // Assert
            Assert.AreEqual(toolA, toolManager.ActiveTool, "Tool A should be the active tool.");
            Assert.AreEqual(toolA.GetType().Name, toolManager.activeToolName, "activeToolName should match Tool A's class name.");

            // Act
            toolManager.SetActiveTool(toolB);

            // Assert
            Assert.AreEqual(toolB, toolManager.ActiveTool, "Tool B should be the active tool.");
            Assert.AreEqual(toolB.GetType().Name, toolManager.activeToolName, "activeToolName should match Tool B's class name.");
        }

        [Test]
        public void ToggleTool_TriggersOnce_DoesNotChangeActiveTool()
        {
            toolManager.SetUpForTesting();
            toolManager.SetActiveTool(toolA);
            Assert.AreEqual(toolA, toolManager.ActiveTool);

            // Act
            toolManager.SetActiveTool(toggleTool);

            // Assert
            Assert.AreEqual(1, toggleTool.ActivationCount, "Toggle tool should have fired once.");
            Assert.AreEqual(toolA, toolManager.ActiveTool, "Active tool should still be Tool A.");
        }

        [Test]
        public void RestrictedTool_RespectsCheckRequirements()
        {
            toolManager.SetUpForTesting();
            toolManager.SetActiveTool(toolA);
            
            // Act: Try to activate restricted tool while ShouldAllow is false
            restrictedTool.ShouldAllow = false;
            toolManager.SetActiveTool(restrictedTool);

            // Assert
            Assert.AreEqual(toolA, toolManager.ActiveTool, "Should NOT have switched to restricted tool.");

            // Act: Allow and try again
            restrictedTool.ShouldAllow = true;
            toolManager.SetActiveTool(restrictedTool);

            // Assert
            Assert.AreEqual(restrictedTool, toolManager.ActiveTool, "Should HAVE switched to restricted tool.");
        }

        [Test]
        public void SwitchingTools_Calls_OnDeactivate_OnPreviousTool()
        {
            toolManager.SetUpForTesting();
            toolManager.SetActiveTool(toolA);

            // Act
            toolManager.SetActiveTool(toolB);

            // Assert
            Assert.IsFalse(toolA.IsActive, "OnDeactivate should have been called on Tool A.");
        }

        [Test]
        public void SwitchingTools_Calls_OnActivate_OnNewTool()
        {
            toolManager.SetUpForTesting();

            // Act
            toolManager.SetActiveTool(toolB);

            // Assert
            Assert.IsTrue(toolB.IsActive, "OnActivate should have been called on Tool B.");
        }

        [Test]
        public void SelectToolByName_ActivatesCorrectTool()
        {
            toolManager.SetUpForTesting();
            toolManager.SetActiveTool(toolA);

            // Act
            toolManager.SelectToolByName("MockToolB");

            // Assert
            Assert.AreEqual(toolB, toolManager.ActiveTool, "Tool B should be active after selecting by name.");
            Assert.IsTrue(toolB.IsActive, "Tool B should be active.");
            Assert.IsFalse(toolA.IsActive, "Tool A should be deactivated.");
        }
    }
}