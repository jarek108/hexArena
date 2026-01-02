using UnityEditor;
using UnityEngine;
using System.Linq;

namespace HexGame.Editor
{
    [CustomEditor(typeof(Hex))]
    [CanEditMultipleObjects]
    public class HexEditor : UnityEditor.Editor
    {
        private Hex targetHex;

        private void OnEnable()
        {
            targetHex = (Hex)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Script field
            GUI.enabled = false;
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour(targetHex), typeof(Hex), false);
            GUI.enabled = true;

            if (targetHex.Data == null)
            {
                EditorGUILayout.HelpBox("No HexData assigned. Grid needs generation or re-linking.", MessageType.Warning);
                return;
            }

            // --- Identification Section ---
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Coordinate: ({targetHex.Q}, {targetHex.R})", EditorStyles.boldLabel);
            
            // --- Properties Section ---
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUI.BeginChangeCheck();
            float newElevation = EditorGUILayout.FloatField("Elevation", targetHex.Elevation);
            TerrainType newType = (TerrainType)EditorGUILayout.EnumPopup("Terrain", targetHex.TerrainType);
            
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(targetHex, "Change Hex Properties");
                targetHex.Elevation = newElevation;
                targetHex.TerrainType = newType;
                EditorUtility.SetDirty(targetHex);
            }

            GUI.enabled = false;
            
            // Units Section
            if (targetHex.Units != null && targetHex.Units.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Units ({targetHex.Units.Count})", EditorStyles.miniBoldLabel);
                foreach (var unit in targetHex.Units)
                {
                    EditorGUILayout.ObjectField(unit, typeof(Unit), true);
                }
            }
            else
            {
                EditorGUILayout.LabelField("Units: None", EditorStyles.miniLabel);
            }
            
            GUI.enabled = true;
            
            EditorGUILayout.EndVertical();

            // --- States Section ---
            if (targetHex.Data.States != null && targetHex.Data.States.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Active States", EditorStyles.miniBoldLabel);
                string statesText = string.Join(", ", targetHex.Data.States.OrderBy(s => s));
                EditorGUILayout.HelpBox(statesText, MessageType.None);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
