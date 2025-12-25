using UnityEditor;
using UnityEngine;
using HexGame.UI;
using HexGame;
using UnityEngine.Events;

namespace HexGame.Editor
{
    public static class AddUnitToolIcon
    {
        [MenuItem("HexGame/Internal/Add Unit Tool Icon")]
        public static void AddIcon()
        {
            IconManager iconManager = Object.FindFirstObjectByType<IconManager>();
            ToolManager toolManager = Object.FindFirstObjectByType<ToolManager>();

            if (iconManager == null || toolManager == null)
            {
                Debug.LogError("IconManager or ToolManager not found in scene.");
                return;
            }

            // Check if already exists
            if (iconManager.icons.Exists(i => i.iconName == "Units"))
            {
                Debug.Log("Unit icon already exists.");
                return;
            }

            IconData unitIcon = new IconData();
            unitIcon.iconName = "Units";
            unitIcon.hotkey = "U";
            unitIcon.iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Icons/Icon_Select.png");
            
            // Set up onClick event
            UnityEditor.Events.UnityEventTools.AddStringPersistentListener(
                unitIcon.onClick, 
                new UnityAction<string>(toolManager.SelectToolByName), 
                "UnitPlacementTool"
            );

            iconManager.icons.Add(unitIcon);
            EditorUtility.SetDirty(iconManager);
            
            Debug.Log("Unit tool icon added to IconManager.");
        }
    }
}
