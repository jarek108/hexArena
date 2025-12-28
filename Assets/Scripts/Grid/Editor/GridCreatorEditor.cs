using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

namespace HexGame.Editor
{
    [CustomEditor(typeof(GridCreator))]
    public class GridCreatorEditor : UnityEditor.Editor
    {
        private static bool showSettings = true;

        private SerializedProperty gridWidthProp;
        private SerializedProperty gridHeightProp;
        private SerializedProperty noiseScaleProp;
        private SerializedProperty elevationScaleProp;
        private SerializedProperty noiseOffsetProp;
        private SerializedProperty waterLevelProp;
        private SerializedProperty mountainLevelProp;
        private SerializedProperty forestLevelProp;
        private SerializedProperty forestScaleProp;

        private string[] availableLevels;
        private int selectedLevelIndex = 0;

        private void OnEnable()
        {
            gridWidthProp = serializedObject.FindProperty("gridWidth");
            gridHeightProp = serializedObject.FindProperty("gridHeight");
            noiseScaleProp = serializedObject.FindProperty("noiseScale");
            elevationScaleProp = serializedObject.FindProperty("elevationScale");
            noiseOffsetProp = serializedObject.FindProperty("noiseOffset");
            waterLevelProp = serializedObject.FindProperty("waterLevel");
            mountainLevelProp = serializedObject.FindProperty("mountainLevel");
            forestLevelProp = serializedObject.FindProperty("forestLevel");
            forestScaleProp = serializedObject.FindProperty("forestScale");
            
            RefreshLevelList();
        }

        private void RefreshLevelList()
        {
            string dir = Path.Combine(Application.dataPath, "Data/Levels");
            if (!Directory.Exists(dir))
            {
                availableLevels = new string[0];
                return;
            }

            availableLevels = Directory.GetFiles(dir, "*.json")
                .Select(Path.GetFileName)
                .ToArray();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            GridCreator creator = (GridCreator)target;

            // --- Generation Section ---
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Map Generation", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(gridWidthProp, new GUIContent("W"));
            EditorGUILayout.PropertyField(gridHeightProp, new GUIContent("H"));
            EditorGUILayout.EndHorizontal();

            showSettings = EditorGUILayout.Foldout(showSettings, "Settings", true);
            if (showSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(noiseScaleProp);
                EditorGUILayout.PropertyField(elevationScaleProp);
                EditorGUILayout.PropertyField(noiseOffsetProp);
                EditorGUILayout.Space(5);
                EditorGUILayout.PropertyField(waterLevelProp);
                EditorGUILayout.PropertyField(mountainLevelProp);
                EditorGUILayout.PropertyField(forestLevelProp);
                EditorGUILayout.PropertyField(forestScaleProp);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate", GUILayout.Height(25))) creator.GenerateGrid();
            if (GUILayout.Button("Clear", GUILayout.Height(25))) creator.ClearGrid();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            // --- Persistence Section ---
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Persistence", EditorStyles.boldLabel);
            
            string defaultDir = Path.Combine(Application.dataPath, "Data/Levels");

            EditorGUILayout.BeginHorizontal();
            if (availableLevels != null && availableLevels.Length > 0)
            {
                if (selectedLevelIndex >= availableLevels.Length) selectedLevelIndex = 0;
                selectedLevelIndex = EditorGUILayout.Popup(selectedLevelIndex, availableLevels);
                if (GUILayout.Button("Load", GUILayout.Width(60)))
                {
                    creator.LoadGrid(Path.Combine(defaultDir, availableLevels[selectedLevelIndex]));
                }
            }
            else
            {
                EditorGUILayout.LabelField("No levels found", EditorStyles.miniLabel);
            }
            
            if (GUILayout.Button("Ref.", GUILayout.Width(40))) RefreshLevelList();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save New..."))
            {
                string path = EditorUtility.SaveFilePanel("Save Grid", defaultDir, "grid.json", "json");
                if (!string.IsNullOrEmpty(path)) 
                { 
                    creator.SaveGrid(path); 
                    RefreshLevelList(); 
                    GUIUtility.ExitGUI();
                }
            }
            if (GUILayout.Button("Import..."))
            {
                string path = EditorUtility.OpenFilePanel("Load Grid", defaultDir, "json");
                if (!string.IsNullOrEmpty(path)) 
                {
                    creator.LoadGrid(path);
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
