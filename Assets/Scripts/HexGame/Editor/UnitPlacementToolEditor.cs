using UnityEditor;
using UnityEngine;
using HexGame.Tools;
using System.Linq;

namespace HexGame.Editor
{
    [CustomEditor(typeof(UnitPlacementTool))]
    public class UnitPlacementToolEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty visualizationProp = serializedObject.FindProperty("unitVisualizationPrefab");
            SerializedProperty unitSetProp = serializedObject.FindProperty("activeUnitSet");
            SerializedProperty indexProp = serializedObject.FindProperty("selectedUnitIndex");

            EditorGUILayout.PropertyField(visualizationProp);
            EditorGUILayout.PropertyField(unitSetProp);

            UnitPlacementTool tool = (UnitPlacementTool)target;
            
            // Access private field via reflection if necessary, but we can use the serializedProperty
            // Actually, we can get the object reference from the property
            var unitSet = unitSetProp.objectReferenceValue as HexGame.Units.UnitSet;

            if (unitSet != null && unitSet.units != null && unitSet.units.Count > 0)
            {
                string[] unitNames = unitSet.units.Select((u, i) => $"[{i}] {u.Name}").ToArray();
                
                int currentIndex = indexProp.intValue;
                if (currentIndex < 0 || currentIndex >= unitNames.Length) currentIndex = 0;

                int newIndex = EditorGUILayout.Popup("Selected Unit", currentIndex, unitNames);
                if (newIndex != currentIndex)
                {
                    indexProp.intValue = newIndex;
                }
            }
            else
            {
                GUI.enabled = false;
                EditorGUILayout.Popup("Selected Unit", 0, new string[] { "No units available" });
                GUI.enabled = true;
                
                if (unitSet == null)
                {
                    EditorGUILayout.HelpBox("Assign a UnitSet to select units.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("The assigned UnitSet is empty.", MessageType.Warning);
                }
            }

            // Draw brush size from base BrushTool
            SerializedProperty brushSizeProp = serializedObject.FindProperty("brushSize");
            if (brushSizeProp != null)
            {
                EditorGUILayout.PropertyField(brushSizeProp);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Ghost Visuals", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ghostTransparency"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("disableGhostShadows"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
