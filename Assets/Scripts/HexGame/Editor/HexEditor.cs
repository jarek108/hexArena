using UnityEditor;
using UnityEngine;

namespace HexGame.Editor
{
    [CustomEditor(typeof(Hex))]
    [CanEditMultipleObjects]
    public class HexEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            Hex hex = (Hex)target;

            // Script field
            GUI.enabled = false;
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour(hex), typeof(Hex), false);
            GUI.enabled = true;

            EditorGUILayout.Space();
            
            if (hex.Data == null)
            {
                EditorGUILayout.HelpBox("No HexData assigned to this View. This usually happens if the Grid was not generated or re-linked.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Coordinates (Logic Layer)", EditorStyles.boldLabel);
            GUI.enabled = false;
            EditorGUILayout.IntField("Q (Column)", hex.Q);
            EditorGUILayout.IntField("R (Row)", hex.R);
            EditorGUILayout.IntField("S (Cube Z)", hex.S);
            GUI.enabled = true;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Properties (Logic Layer)", EditorStyles.boldLabel);

            // Since we are in the inspector for the Hex (View), 
            // we proxy the edits to the underlying data properties.
            // Note: Changing these in the inspector won't be "undoable" in the standard way
            // unless we use SerializedProperties on HexData, but for a simple view, 
            // direct assignment is fine for debugging.

            float newElevation = EditorGUILayout.FloatField("Elevation", hex.Elevation);
            if (newElevation != hex.Elevation)
            {
                Undo.RecordObject(hex, "Change Hex Elevation");
                hex.Elevation = newElevation;
            }

            TerrainType newType = (TerrainType)EditorGUILayout.EnumPopup("Terrain Type", hex.TerrainType);
            if (newType != hex.TerrainType)
            {
                Undo.RecordObject(hex, "Change Hex Terrain");
                hex.TerrainType = newType;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("State", EditorStyles.boldLabel);
            GUI.enabled = false;
            EditorGUILayout.ObjectField("Occupying Unit", hex.Unit, typeof(Unit), true);
            GUI.enabled = true;
        }
    }
}