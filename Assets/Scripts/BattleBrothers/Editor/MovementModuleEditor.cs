using UnityEditor;
using UnityEngine;

namespace HexGame.Editor
{
    [CustomEditor(typeof(MovementModule))]
    public class MovementModuleEditor : UnityEditor.Editor
    {
        private Vector2 scrollPos;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // 1. Constraints Section
            // Using direct access to ensure [Header] is handled by PropertyField correctly
            var maxElevationDelta = serializedObject.FindProperty("maxElevationDelta");
            var uphillPenalty = serializedObject.FindProperty("uphillPenalty");
            var zocPenalty = serializedObject.FindProperty("zocPenalty");

            if (maxElevationDelta != null) EditorGUILayout.PropertyField(maxElevationDelta);
            if (uphillPenalty != null) EditorGUILayout.PropertyField(uphillPenalty);
            if (zocPenalty != null) EditorGUILayout.PropertyField(zocPenalty);

            EditorGUILayout.Space();

            // 2. Terrain Costs Section (Horizontal Grid)
            EditorGUILayout.LabelField("Terrain Costs", EditorStyles.boldLabel);
            
            // To avoid the [Header("Terrain Costs")] from the script appearing inside the columns,
            // we draw the fields manually instead of using PropertyField.
            // Using a simpler scroll view to avoid skin issues.
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(65));
            EditorGUILayout.BeginHorizontal();

            DrawTerrainField("plainsCost", "Plain");
            DrawTerrainField("waterCost", "Water");
            DrawTerrainField("mountainCost", "Mount.");
            DrawTerrainField("forestCost", "Forest");
            DrawTerrainField("desertCost", "Desert");
            
            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawTerrainField(string propName, string label)
        {
            var prop = serializedObject.FindProperty(propName);
            if (prop == null) return;

            EditorGUILayout.BeginVertical(GUILayout.Width(60));
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel, GUILayout.Width(60));
            
            // Using FloatField directly to skip DecoratorDrawers (like [Header])
            EditorGUI.BeginChangeCheck();
            float newVal = EditorGUILayout.FloatField(prop.floatValue, GUILayout.Width(55));
            if (EditorGUI.EndChangeCheck())
            {
                prop.floatValue = newVal;
            }
            
            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
        }
    }
}
