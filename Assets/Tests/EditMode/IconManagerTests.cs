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
            iconPrefab.AddComponent<Image>();
            
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
        public void RefreshUI_Assigns_Sprites_To_Images()
        {
            // Arrange
            Sprite testSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
            iconManager.icons.Add(new IconData { iconName = "SpriteTest", iconSprite = testSprite });

            // Act
            iconManager.RefreshUI();

            // Assert
            GameObject child = iconManager.transform.GetChild(0).gameObject;
            Image img = child.GetComponent<Image>();
            Assert.IsNotNull(img);
            Assert.AreEqual(testSprite, img.sprite);
            
            Object.DestroyImmediate(testSprite);
        }
    }
}
