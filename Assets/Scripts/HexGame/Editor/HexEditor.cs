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
            if (targetHex != null && targetHex.Data != null)
            {
                targetHex.Data.OnStateChanged += HandleStateChanged;
            }
        }

        private void OnDisable()
        {
            if (targetHex != null && targetHex.Data != null)
            {
                targetHex.Data.OnStateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged()
        {
            Repaint();
        }

        public override void OnInspectorGUI()
        {
            // If data was assigned after OnEnable, try to subscribe
            if (targetHex.Data != null)
            {
                // Simple way to ensure we are subscribed if data appears late 
                // (e.g. after generation while inspector is open)
                targetHex.Data.OnStateChanged -= HandleStateChanged;
                targetHex.Data.OnStateChanged += HandleStateChanged;
            }

            // Script field
            GUI.enabled = false;
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour(targetHex), typeof(Hex), false);
            GUI.enabled = true;

            EditorGUILayout.Space();
            
            if (targetHex.Data == null)
            {
                EditorGUILayout.HelpBox("No HexData assigned to this View. This usually happens if the Grid was not generated or re-linked.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Hex Coordinates (Q, R, S)", EditorStyles.boldLabel);
            GUI.enabled = false;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.IntField(GUIContent.none, targetHex.Q);
            EditorGUILayout.IntField(GUIContent.none, targetHex.R);
            EditorGUILayout.IntField(GUIContent.none, targetHex.S);
            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Terrain Properties", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            float newElevation = EditorGUILayout.FloatField("Elev", targetHex.Elevation, GUILayout.Width(100));
            if (newElevation != targetHex.Elevation)
            {
                Undo.RecordObject(targetHex, "Change Hex Elevation");
                targetHex.Elevation = newElevation;
            }

            TerrainType newType = (TerrainType)EditorGUILayout.EnumPopup(GUIContent.none, targetHex.TerrainType);
            if (newType != targetHex.TerrainType)
            {
                Undo.RecordObject(targetHex, "Change Hex Terrain");
                targetHex.TerrainType = newType;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Active States (Logic Layer)", EditorStyles.boldLabel);
            
            if (targetHex.Data.States == null || targetHex.Data.States.Count == 0)
            {
                EditorGUILayout.LabelField("None", EditorStyles.miniLabel);
            }
            else
            {
                // Draw states as tags
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                foreach (var state in targetHex.Data.States.OrderBy(s => s))
                {
                    EditorGUILayout.LabelField($"• {state}", EditorStyles.wordWrappedLabel);
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Hierarchy", EditorStyles.boldLabel);
            GUI.enabled = false;
            EditorGUILayout.ObjectField("Occupying Unit", targetHex.Unit, typeof(Unit), true);
            GUI.enabled = true;
        }
    }
}