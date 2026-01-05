using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HexGame.UI;
using UnityEngine.UI;

namespace HexGame.Tests
{
    public class IconManagerTests
    {
        private GameObject iconManagerGO;
        private IconManager iconManager;
        private GameObject iconPrefab;

        [SetUp]
        public void SetUp()
        {
            iconManagerGO = new GameObject("IconManager");
            iconManager = iconManagerGO.AddComponent<IconManager>();
            
            iconPrefab = new GameObject("IconPrefab");
            iconPrefab.AddComponent<RectTransform>();
            
            // Create child for IconImage as expected by IconManager logic
            GameObject iconImageChild = new GameObject("IconImage");
            iconImageChild.transform.SetParent(iconPrefab.transform);
            iconImageChild.AddComponent<RectTransform>();
            iconImageChild.AddComponent<Image>();

            GameObject shortcutTextChild = new GameObject("ShortcutText");
            shortcutTextChild.transform.SetParent(iconPrefab.transform);
            shortcutTextChild.AddComponent<RectTransform>();
            
            iconManager.iconPrefab = iconPrefab;
        }

        [TearDown]
        public void TearDown()
        {
            if (iconManagerGO != null) Object.DestroyImmediate(iconManagerGO);
            if (iconPrefab != null) Object.DestroyImmediate(iconPrefab);
        }

        [Test]
        public void RefreshUI_Creates_GameObjects_For_Icons()
        {
            // Arrange
            iconManager.icons.Add(new IconData { iconName = "Test1" });
            iconManager.icons.Add(new IconData { iconName = "Test2" });

            // Act
            iconManager.RefreshUI();

            // Assert
            Assert.AreEqual(2, iconManager.transform.childCount, "Should have 2 child objects.");
            Assert.AreEqual("Icon_Test1", iconManager.transform.GetChild(0).name);
            Assert.AreEqual("Icon_Test2", iconManager.transform.GetChild(1).name);
        }

        [Test]
        public void ClearUIImmediate_Removes_All_Children()
        {
            // Arrange
            new GameObject("Child1").transform.SetParent(iconManager.transform);
            new GameObject("Child2").transform.SetParent(iconManager.transform);
            Assert.AreEqual(2, iconManager.transform.childCount);

            // Act
            iconManager.ClearUIImmediate();

            // Assert
            Assert.AreEqual(0, iconManager.transform.childCount, "All children should be removed.");
        }

        [Test]
        public void RefreshUI_WithoutClearing_CreatesDuplicates()
        {
            // Arrange
            iconManager.icons.Add(new IconData { iconName = "Test1" });
            
            // Act
            iconManager.RefreshUI();
            iconManager.RefreshUI(); // Calling it again

            // Assert
            // Currently, IconManager.RefreshUI only clears children if Application.isPlaying is true.
            // In EditMode (tests), it should currently duplicate.
            Assert.AreNotEqual(1, iconManager.transform.childCount, "Expected duplication because RefreshUI doesn't clear in EditMode.");
        }

        [Test]
        public void RefreshUI_WithClear_DoesNotCreateDuplicates()
        {
            // Arrange
            iconManager.icons.Add(new IconData { iconName = "Test1" });
            
            // Act
            iconManager.ClearUIImmediate();
            iconManager.RefreshUI();
            iconManager.ClearUIImmediate();
            iconManager.RefreshUI();

            // Assert
            Assert.AreEqual(1, iconManager.transform.childCount, "Should only have 1 child object after manual clearing.");
        }

        [Test]
        public void RefreshUI_Assigns_Sprites_To_Images()
        {
            // Arrange
            Sprite testSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
            iconManager.icons.Add(new IconData { iconName = "SpriteTest", iconSprite = testSprite });

            // Act
            iconManager.RefreshUI();

            // Assert
            GameObject child = iconManager.transform.GetChild(0).gameObject;
            Transform iconImageTransform = child.transform.Find("IconImage");
            Assert.IsNotNull(iconImageTransform, "IconImage child not found");
            
            Image img = iconImageTransform.GetComponent<Image>();
            Assert.IsNotNull(img, "Image component not found on IconImage child");
            Assert.AreEqual(testSprite, img.sprite);
            
            Object.DestroyImmediate(testSprite);
        }

        [Test]
        public void PopulateTools_DoesNotThrow_And_PopulatesIcons()
        {
            // Arrange
            GameObject toolManagerGO = new GameObject("ToolManager");
            var toolManager = toolManagerGO.AddComponent<HexGame.ToolManager>();
            toolManagerGO.AddComponent<HexGame.Tools.GridTool>();
            
            // Replicate Editor-like state: manager might be in a prefab or have nulls
            iconManager.icons.Clear();
            
            // Act & Assert
            Assert.DoesNotThrow(() => iconManager.PopulateTools(), "PopulateTools should not throw errors.");
            
            Assert.Greater(iconManager.icons.Count, 0, "Icons list should be populated with at least one tool.");
            Object.DestroyImmediate(toolManagerGO);
        }

        [Test]
        public void RefreshUI_Handles_PartialPrefab_Robustly()
        {
            // Arrange
            // Create a prefab that is MISSING some components expected by logic
            GameObject badPrefab = new GameObject("BadPrefab");
            // No IconImage, No ShortcutText
            iconManager.iconPrefab = badPrefab;
            iconManager.icons.Add(new IconData { iconName = "Test" });

            // Act & Assert
            Assert.DoesNotThrow(() => iconManager.RefreshUI(), "RefreshUI should handle prefabs with missing children safely.");
            
            Object.DestroyImmediate(badPrefab);
        }

        [Test]
        public void PopulateTools_AssignsIcons_ForKnownTools()
        {
            // Arrange
            GameObject toolManagerGO = new GameObject("ToolManager");
            var toolManager = toolManagerGO.AddComponent<HexGame.ToolManager>();
            
            // Add several tools
            toolManagerGO.AddComponent<HexGame.Tools.GridTool>();
            toolManagerGO.AddComponent<HexGame.Tools.ZoCTool>();
            toolManagerGO.AddComponent<HexGame.Tools.PathfindingTool>();
            
            // Set the icon folder to where we know sprites exist
            iconManager.iconFolder = "Assets/Resources/Art/ToolIcons";

            // Act
            iconManager.PopulateTools();

            // Assert
            Assert.AreEqual(3, iconManager.icons.Count, "Should have 3 icons populated.");
            
            foreach(var icon in iconManager.icons)
            {
                // We don't necessarily REQUIRE an icon sprite if it's missing from disk, 
                // but for our core tools in the repo, they should ideally be found.
                // If they are null in the test environment, we at least check that the entry was created.
                Assert.IsFalse(string.IsNullOrEmpty(icon.iconName), "Icon name should not be empty.");
                Assert.IsFalse(string.IsNullOrEmpty(icon.hotkey), "Hotkey should be assigned.");
                
                // Specific check for core tools that MUST have icons
                if(icon.iconName == "GridTool" || icon.iconName == "ZoCTool")
                {
                    Assert.IsNotNull(icon.iconSprite, $"Icon sprite for {icon.iconName} should not be null.");
                }
            }

            Object.DestroyImmediate(toolManagerGO);
        }
    }
}
