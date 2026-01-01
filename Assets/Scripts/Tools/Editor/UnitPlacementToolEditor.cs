using UnityEditor;
using UnityEngine;
using HexGame.Tools;
using System.Linq;
using System.IO;

namespace HexGame.Editor
{
    [CustomEditor(typeof(UnitPlacementTool))]
    public class UnitPlacementToolEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var unitManager = UnitManager.Instance;
            if (unitManager == null)
            {
                EditorGUILayout.HelpBox("UnitManager instance not found.", MessageType.Error);
                return;
            }

            // 1. Unit Set View (Sync with UnitManager)
            string setsDir = "Assets/Data/Sets";
            string[] setFiles = Directory.Exists(setsDir) ? Directory.GetFiles(setsDir, "*.json") : new string[0];
            string[] setNames = setFiles.Select(Path.GetFileName).ToArray();
            int setIndex = System.Array.IndexOf(setFiles.Select(f => f.Replace("\\", "/")).ToArray(), unitManager.activeUnitSetPath);

            EditorGUI.BeginChangeCheck();
            setIndex = EditorGUILayout.Popup("Unit Set", setIndex, setNames);
            if (EditorGUI.EndChangeCheck() && setIndex >= 0)
            {
                Undo.RecordObject(unitManager, "Change Unit Set");
                unitManager.activeUnitSetPath = setFiles[setIndex].Replace("\\", "/");
                unitManager.LoadActiveSet();
                EditorUtility.SetDirty(unitManager);
            }

            // 2. Unit Selection
            var activeSet = unitManager.ActiveUnitSet;
            if (activeSet != null && activeSet.units.Count > 0)
            {
                string[] unitNames = activeSet.units.Select((u, i) => $"[{i}] {u.Name}").ToArray();
                SerializedProperty idProp = serializedObject.FindProperty("selectedUnitId");
                
                int currentIndex = activeSet.units.FindIndex(u => u.id == idProp.stringValue);
                if (currentIndex == -1) currentIndex = 0;

                int newIndex = EditorGUILayout.Popup("Unit Type", currentIndex, unitNames);
                if (newIndex != currentIndex && newIndex >= 0 && newIndex < activeSet.units.Count)
                {
                    idProp.stringValue = activeSet.units[newIndex].id;
                }
            }

            // 3. Team Selection (Simple ID)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("selectedTeamId"), new GUIContent("Team ID"));

            // 4. Brush Settings
            EditorGUILayout.PropertyField(serializedObject.FindProperty("brushSize"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxBrushSize"));

            // 5. Visuals
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ghostTransparency"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("disableGhostShadows"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}