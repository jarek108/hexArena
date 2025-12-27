using UnityEditor;
using UnityEngine;

namespace HexGame.Editor
{
    [CustomEditor(typeof(Unit))]
    [CanEditMultipleObjects]
    public class UnitEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            Unit unit = (Unit)target;

            GUI.enabled = false;
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour(unit), typeof(Unit), false);
            GUI.enabled = true;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Unit Identity", EditorStyles.boldLabel);

            GUI.enabled = false;
            EditorGUILayout.IntField("Id", unit.Id);
            EditorGUILayout.TextField("Type", unit.unitName);
            GUI.enabled = true;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("unitSet"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("unitIndex"), new GUIContent("Type Index"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("teamId"));
            
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                
                // Re-initialize logic and refresh links
                unit.Initialize(unit.unitSet, (int)serializedObject.FindProperty("unitIndex").intValue, unit.teamId);
                if (unit.CurrentHex != null)
                {
                    unit.SetHex(unit.CurrentHex);
                }
                
                EditorUtility.SetDirty(unit);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            GUI.enabled = false;
            EditorGUILayout.ObjectField("Current Hex", unit.CurrentHex, typeof(Hex), true);
            GUI.enabled = true;

            if (unit.Stats != null && unit.Stats.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Stats", EditorStyles.boldLabel);
                GUI.enabled = false;
                foreach (var stat in unit.Stats)
                {
                    EditorGUILayout.IntField(stat.Key, stat.Value);
                }
                GUI.enabled = true;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
