using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace HexGame.Units.Editor
{
    public class UnitSetWindow : EditorWindow
    {
        private UnitSet selectedSet;
        private Vector2 scrollPos;

        private const string PREF_SET_PATH = "HexGame_LastUnitSetPath";

        [MenuItem("HexGame/Unit Sets")]
        public static void OpenWindow()
        {
            GetWindow<UnitSetWindow>("Unit Sets").Show();
        }

        private void OnEnable()
        {
            string setPath = EditorPrefs.GetString(PREF_SET_PATH, "");
            if (!string.IsNullOrEmpty(setPath)) selectedSet = AssetDatabase.LoadAssetAtPath<UnitSet>(setPath);
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            
            // Top Bar: Selection and Creation
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Target Unit Set:", GUILayout.Width(100));
            
            EditorGUI.BeginChangeCheck();
            selectedSet = (UnitSet)EditorGUILayout.ObjectField(selectedSet, typeof(UnitSet), false);
            if (EditorGUI.EndChangeCheck())
            {
                if (selectedSet != null) EditorPrefs.SetString(PREF_SET_PATH, AssetDatabase.GetAssetPath(selectedSet));
                else EditorPrefs.SetString(PREF_SET_PATH, "");
            }
            
            if (GUILayout.Button("New Set", GUILayout.Width(80)))
            {
                CreateNewSet();
            }
            EditorGUILayout.EndHorizontal();

            if (selectedSet != null)
            {
                // Renaming / Schema Info Section
                EditorGUILayout.BeginVertical("box");
                
                // Schema display
                if (selectedSet.schema != null)
                {
                    EditorGUILayout.LabelField($"Schema: {selectedSet.schema.name}", EditorStyles.miniLabel);
                    
                    // Name Field & Renaming Logic
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Set Name:", GUILayout.Width(70));
                    
                    EditorGUI.BeginChangeCheck();
                    string oldName = selectedSet.setName;
                    string newName = EditorGUILayout.TextField(oldName);
                    newName = SanitizeFilename(newName);
                    
                    if (EditorGUI.EndChangeCheck() && newName != oldName)
                    {
                        selectedSet.setName = newName;
                        EditorUtility.SetDirty(selectedSet);
                        
                        // Trigger Rename
                        RenameAsset(selectedSet);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndVertical();

                // Draw Editor (Handles missing schema internally)
                UnitEditorUI.DrawUnitSetEditor(selectedSet, ref scrollPos);
            }
            else
            {
                EditorGUILayout.HelpBox("Select a Unit Set to edit.", MessageType.Info);
            }
        }

        private void CreateNewSet()
        {
            string folder = "Assets/Data/Sets";
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            UnitSet newSet = CreateInstance<UnitSet>();
            
            // Inherit schema from current if available
            if (selectedSet != null && selectedSet.schema != null)
            {
                newSet.schema = selectedSet.schema;
                newSet.setName = "NewSet"; // Default name
            }
            else
            {
                 newSet.setName = "NewSet";
            }

            // Determine file name
            string baseName = "NewUnitSet";
            if (newSet.schema != null)
            {
                baseName = $"{newSet.schema.name}_{newSet.setName}";
            }
            
            string path = Path.Combine(folder, baseName + ".asset");
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            
            AssetDatabase.CreateAsset(newSet, path);
            AssetDatabase.SaveAssets();
            
            selectedSet = newSet;
            EditorPrefs.SetString(PREF_SET_PATH, AssetDatabase.GetAssetPath(selectedSet));
            EditorGUIUtility.PingObject(newSet);
        }

        private void RenameAsset(UnitSet set)
        {
            if (set == null || set.schema == null) return;
            
            string currentPath = AssetDatabase.GetAssetPath(set);
            string newName = $"{set.schema.name}_{set.setName}";
            newName = SanitizeFilename(newName);

            if (Path.GetFileNameWithoutExtension(currentPath) == newName) return;
            
            string result = AssetDatabase.RenameAsset(currentPath, newName);
            
            if (string.IsNullOrEmpty(result))
            {
                AssetDatabase.SaveAssets(); 
            }
            else
            {
                Debug.LogError($"Failed to rename asset: {result}");
            }
        }

        private string SanitizeFilename(string name)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                name = name.Replace(c, '_');
            }
            return name;
        }
    }
}