using UnityEditor;
using UnityEngine;

namespace HexGame.Editor
{
    [CustomEditor(typeof(GridCreator))]
    public class GridCreatorEditor : UnityEditor.Editor
    {
        private static bool showGeneration = true;
        private static bool showTerrain = true;

        private SerializedProperty gridWidthProp;
        private SerializedProperty gridHeightProp;
        private SerializedProperty noiseScaleProp;
        private SerializedProperty elevationScaleProp;
        private SerializedProperty noiseOffsetProp;
        private SerializedProperty waterLevelProp;
        private SerializedProperty mountainLevelProp;
        private SerializedProperty forestLevelProp;
        private SerializedProperty forestScaleProp;

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
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GridCreator creator = (GridCreator)target;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(gridWidthProp, new GUIContent("Grid Width"));
            EditorGUILayout.PropertyField(gridHeightProp, new GUIContent("Height"));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            showGeneration = EditorGUILayout.BeginFoldoutHeaderGroup(showGeneration, "Generation Settings");
            if (showGeneration)
            {
                EditorGUILayout.PropertyField(noiseScaleProp);
                EditorGUILayout.PropertyField(elevationScaleProp);
                EditorGUILayout.PropertyField(noiseOffsetProp);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            showTerrain = EditorGUILayout.BeginFoldoutHeaderGroup(showTerrain, "Terrain Generation Thresholds");
            if (showTerrain)
            {
                EditorGUILayout.PropertyField(waterLevelProp);
                EditorGUILayout.PropertyField(mountainLevelProp);
                EditorGUILayout.PropertyField(forestLevelProp);
                EditorGUILayout.PropertyField(forestScaleProp);
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Map Operations", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate Grid")) creator.GenerateGrid();
            if (GUILayout.Button("Clear Grid")) creator.ClearGrid();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Persistence", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Grid"))
            {
                string path = EditorUtility.SaveFilePanel("Save Grid", "", "grid.json", "json");
                if (!string.IsNullOrEmpty(path)) creator.SaveGrid(path);
            }
            if (GUILayout.Button("Load Grid"))
            {
                string path = EditorUtility.OpenFilePanel("Load Grid", "", "json");
                if (!string.IsNullOrEmpty(path)) creator.LoadGrid(path);
            }
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }
    }
}