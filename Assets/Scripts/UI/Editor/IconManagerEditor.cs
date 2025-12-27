using UnityEditor;
using UnityEngine;
using HexGame.UI;
using HexGame.Tools;
using System.Linq;
using System.Collections.Generic;

namespace HexGame.Editor
{
    [CustomEditor(typeof(IconManager))]
    public class IconManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            IconManager manager = (IconManager)target;
            SerializedProperty iconsProp = serializedObject.FindProperty("icons");

            serializedObject.Update();

            // Top Buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Populate Tools"))
            {
                PopulateTools(manager);
            }
            if (GUILayout.Button("Add Icon"))
            {
                iconsProp.InsertArrayElementAtIndex(iconsProp.arraySize);
            }
            if (GUILayout.Button("Clear Icons"))
            {
                manager.icons.Clear();
                manager.ClearUIImmediate();
                EditorUtility.SetDirty(manager);
            }
            GUILayout.EndHorizontal();
            
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("iconPrefab"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("iconFolder"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("iconSize"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("backgroundBorderSize"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("backgroundColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("spacing"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("padding"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Hotkey Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hotkeyColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hotkeySize"));
            
            EditorGUILayout.LabelField("Selection Visuals", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("activeIconColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("inactiveIconColor"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Icons", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            for (int i = 0; i < iconsProp.arraySize; i++)
            {
                SerializedProperty element = iconsProp.GetArrayElementAtIndex(i);
                SerializedProperty nameProp = element.FindPropertyRelative("iconName");
                SerializedProperty spriteProp = element.FindPropertyRelative("iconSprite");
                SerializedProperty hotkeyProp = element.FindPropertyRelative("hotkey");
                SerializedProperty onClickProp = element.FindPropertyRelative("onClick");

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(nameProp, GUIContent.none, GUILayout.Width(80));
                EditorGUILayout.PropertyField(spriteProp, GUIContent.none, GUILayout.Width(100));
                EditorGUILayout.LabelField("Key:", GUILayout.Width(30));
                EditorGUILayout.PropertyField(hotkeyProp, GUIContent.none, GUILayout.Width(40));
                
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    iconsProp.DeleteArrayElementAtIndex(i);
                    // Breaking here is crucial as the array size has changed
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break; 
                }
                EditorGUILayout.EndHorizontal();

                // Draw the UnityEvent on a new line
                EditorGUILayout.PropertyField(onClickProp);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }

            if (serializedObject.ApplyModifiedProperties() || EditorGUI.EndChangeCheck())
            {
                if (!Application.isPlaying)
                {
                    manager.ClearUIImmediate();
                    manager.RefreshUI();
                    EditorUtility.SetDirty(manager);
                }
            }
        }

        private void PopulateTools(IconManager iconManager)
        {
            ToolManager toolManager = FindFirstObjectByType<ToolManager>();
            if (toolManager == null)
            {
                // Try to find it on the same object or scene
                toolManager = iconManager.GetComponent<ToolManager>();
                if (toolManager == null)
                {
                    return;
                }
            }

            // Clear existing icons
            iconManager.icons.Clear();

            // Get all ITool components from the ToolManager's GameObject
            var tools = toolManager.GetComponents<ITool>();

            // Track used hotkeys
            HashSet<string> usedHotkeys = new HashSet<string>();

            bool dirty = true; // Always dirty after clear

            foreach (var tool in tools)
            {
                string toolTypeName = tool.GetType().Name;
                string cleanName = toolTypeName.Replace("Tool", "");

                // Create new IconData
                IconData newData = new IconData();
                newData.iconName = toolTypeName;
                newData.onClick = new UnityEngine.Events.UnityEvent();
                
                // Assign Sprite
                newData.iconSprite = FindSpriteForTool(cleanName, iconManager.iconFolder);

                // Assign Hotkey
                string hotkey = AssignHotkey(toolTypeName, usedHotkeys);
                if (hotkey != null)
                {
                    newData.hotkey = hotkey;
                    usedHotkeys.Add(hotkey);
                }

                // Setup Event
                UnityEditor.Events.UnityEventTools.AddStringPersistentListener(
                    newData.onClick, 
                    toolManager.SelectToolByName, 
                    toolTypeName
                );

                iconManager.icons.Add(newData);
            }

            if (dirty)
            {
                EditorUtility.SetDirty(iconManager);
                iconManager.ClearUIImmediate(); // Clear existing GameObjects first
                iconManager.RefreshUI(); // Rebuild from the new list
            }
        }

        private Sprite FindSpriteForTool(string name, string iconFolder)
        {
            string[] searchNames = { $"Icon_{name}", "Icon_Select" };
            if (name == "Pathfinding") searchNames = new[] { "Icon_Pathfinding", "Icon_Select" };

            foreach (var searchName in searchNames)
            {
                string[] guids = AssetDatabase.FindAssets($"{searchName} t:Sprite", new[] { iconFolder });
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    return AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }
            }
            return null;
        }

        private string AssignHotkey(string name, HashSet<string> used)
        {
            string upperName = name.ToUpper();
            
            // Try letters from the name
            foreach (char c in upperName)
            {
                string key = c.ToString();
                if (char.IsLetter(c) && !used.Contains(key))
                {
                    return key;
                }
            }

            // Fallback: Try A-Z
            string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            foreach (char c in alphabet)
            {
                string key = c.ToString();
                if (!used.Contains(key))
                {
                    return key;
                }
            }

            return null;
        }
    }
}
