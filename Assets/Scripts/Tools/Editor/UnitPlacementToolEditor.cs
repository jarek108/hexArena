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

            SerializedProperty indexProp = serializedObject.FindProperty("selectedUnitIndex");
            SerializedProperty teamProp = serializedObject.FindProperty("selectedTeamId");

            EditorGUILayout.PropertyField(teamProp);

            UnitPlacementTool tool = (UnitPlacementTool)target;
            var unitManager = UnitManager.Instance;

            if (unitManager != null && unitManager.ActiveUnitSet != null && unitManager.ActiveUnitSet.units != null && unitManager.ActiveUnitSet.units.Count > 0)
            {
                var unitSet = unitManager.ActiveUnitSet;
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
                EditorGUILayout.HelpBox("Ensure UnitManager has an active UnitSet.", MessageType.Info);
            }

            SerializedProperty brushSizeProp = serializedObject.FindProperty("brushSize");
            if (brushSizeProp != null)
            {
                EditorGUILayout.PropertyField(brushSizeProp);
            }

            SerializedProperty maxBrushSizeProp = serializedObject.FindProperty("maxBrushSize");
            if (maxBrushSizeProp != null)
            {
                EditorGUILayout.PropertyField(maxBrushSizeProp);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Ghost Visuals", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ghostTransparency"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("disableGhostShadows"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}