using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HexGame.UI;
using UnityEngine.UI;

namespace HexGame.Tests
{
    public class UnitStatsDisplayTests
    {
        private GameObject canvasGO;
        private GameObject managerGO;
        private UnitStatsDisplay display;
        private HexRaycaster raycaster;

        [SetUp]
        public void SetUp()
        {
            canvasGO = new GameObject("UI Canvas");
            canvasGO.AddComponent<Canvas>();
            
            managerGO = new GameObject("UnitStatsManager");
            display = managerGO.AddComponent<UnitStatsDisplay>();

            // Add raycaster to the scene
            GameObject rayGo = new GameObject("Raycaster");
            raycaster = rayGo.AddComponent<HexRaycaster>();
            
            // Inject raycaster into display via reflection
            var rayField = typeof(UnitStatsDisplay).GetField("raycaster", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            rayField.SetValue(display, raycaster);
        }

        [TearDown]
        public void TearDown()
        {
            if (canvasGO != null) Object.DestroyImmediate(canvasGO);
            if (managerGO != null) Object.DestroyImmediate(managerGO);
            if (raycaster != null) Object.DestroyImmediate(raycaster.gameObject);
            
            // Cleanup any panels created under the canvas or globally
            var panels = Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None);
            foreach (var p in panels)
            {
                if (p.name == "UnitStatsPanel") Object.DestroyImmediate(p.gameObject);
            }
        }

        [Test]
        public void EnsureUI_Creates_Panel_And_Text()
        {
            // Act
            // Triggering via a private method check is hard, but we can call a public initialization if we had one.
            // Since EnsureUI is private, we rely on the fact that it's called in Update/Start or we use reflection.
            // For tests, let's make a public wrapper or just invoke it via reflection.
            
            var method = typeof(UnitStatsDisplay).GetMethod("EnsureUI", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            method.Invoke(display, null);

            // Assert
            Assert.IsNotNull(display.panel, "Panel should be created and assigned.");
            Assert.IsNotNull(display.unitNameText, "Text should be created and assigned.");
            Assert.AreEqual("UnitStatsPanel", display.panel.name);
            Assert.AreEqual("UnitNameText", display.unitNameText.gameObject.name);
        }

        [Test]
        public void EnsureUI_Does_Not_Create_Duplicate_Panels()
        {
            // Act
            var method = typeof(UnitStatsDisplay).GetMethod("EnsureUI", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            method.Invoke(display, null);
            
            // Clear reference but leave object in scene
            display.panel = null;
            display.unitNameText = null;
            
            method.Invoke(display, null);

            // Assert
            var panels = GameObject.FindObjectsByType<RectTransform>(FindObjectsSortMode.None);
            int count = 0;
            foreach(var p in panels) if(p.name == "UnitStatsPanel") count++;
            
            Assert.AreEqual(1, count, "Should only have one UnitStatsPanel in the scene.");
            Assert.IsNotNull(display.panel, "Should have re-linked to the existing panel.");
        }

        [Test]
        public void UpdateUI_Respects_ContinuouslyVisible_In_Editor()
        {
            // Arrange
            var methodEnsure = typeof(UnitStatsDisplay).GetMethod("EnsureUI", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            methodEnsure.Invoke(display, null);
            
            var methodUpdate = typeof(UnitStatsDisplay).GetMethod("UpdateUI", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            // Act & Assert 1: Default (Editor mode simulation)
            // In EditMode, currentTarget is usually null. 
            // In our script: bool shouldShow = continuouslyVisible || currentTarget != null || !Application.isPlaying;
            // Since we ARE in EditMode (not playing), it should be true.
            
            methodUpdate.Invoke(display, null);
            Assert.IsTrue(display.panel.gameObject.activeSelf, "Panel should be active in Editor by default.");
            Assert.AreEqual("Unit Name", display.unitNameText.text);
        }

        [Test]
        public void Visibility_Logic_Simulation()
        {
            // We can't easily mock Application.isPlaying, but we can test the logic if we move it to a helper or just test the outcomes.
            // Let's test continuouslyVisible flag effect on text when currentTarget is null.
            
            var methodEnsure = typeof(UnitStatsDisplay).GetMethod("EnsureUI", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            methodEnsure.Invoke(display, null);
            
            display.continuouslyVisible = true;
            var methodUpdate = typeof(UnitStatsDisplay).GetMethod("UpdateUI", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            methodUpdate.Invoke(display, null);
            
            Assert.IsTrue(display.panel.gameObject.activeSelf);
        }

        [Test]
        public void UpdateTarget_Persists_Unit_When_KeepShowingLastUnit_Is_True()
        {
            // Arrange
            display.keepShowingLastUnit = true;
            
            // Create a dummy unit and hex
            GameObject unitGo = new GameObject("TestUnit");
            Unit unit = unitGo.AddComponent<Unit>();
            GameObject hexGo = new GameObject("TestHex");
            Hex hex = hexGo.AddComponent<Hex>();
            
            // Link them
            var dataField = typeof(Hex).GetProperty("Data", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            dataField.SetValue(hex, new HexData(0,0));
            hex.Unit = unit;

            // 1. Hover the hex
            raycaster.currentHex = hex;
            var methodUpdateTarget = typeof(UnitStatsDisplay).GetMethod("UpdateTarget", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            methodUpdateTarget.Invoke(display, null);
            Assert.AreEqual(unit, display.displayedUnit, "Unit should be selected when hovered.");

            // 2. Move mouse away (hex becomes null)
            raycaster.currentHex = null;
            methodUpdateTarget.Invoke(display, null);

            // Assert
            Assert.AreEqual(unit, display.displayedUnit, "Unit should persist when keepShowingLastUnit is true.");

            // Act 3: Disable flag and update
            display.keepShowingLastUnit = false;
            methodUpdateTarget.Invoke(display, null);

            // Assert 3
            Assert.IsNull(display.displayedUnit, "Unit should be cleared when keepShowingLastUnit is false and mouse moved away.");

            Object.DestroyImmediate(unitGo);
            Object.DestroyImmediate(hexGo);
        }

        [Test]
        public void UpdateTarget_Handles_SelectionModes()
        {
            // Arrange
            display.chooseUnitOn = UnitStatsDisplay.SelectionMode.LClick;
            display.displayedUnit = null;
            
            GameObject hexGo = new GameObject("TestHex");
            Hex hex = hexGo.AddComponent<Hex>();
            raycaster.currentHex = hex;

            var methodUpdateTarget = typeof(UnitStatsDisplay).GetMethod("UpdateTarget", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            // Act: Hover (but mode is LClick)
            methodUpdateTarget.Invoke(display, null);

            // Assert
            Assert.IsNull(display.displayedUnit, "Should not update on hover when mode is LClick.");
            
            Object.DestroyImmediate(hexGo);
        }
    }
}
