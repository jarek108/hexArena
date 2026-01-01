using UnityEditor;
using UnityEngine;

namespace HexGame.Editor
{
    [CustomEditor(typeof(CombatModule))]
    public class CombatModuleEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // 1. Melee Modifiers
            EditorGUILayout.LabelField("Melee Modifiers", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            DrawPair("meleeHighGroundBonus", "High Ground", "meleeLowGroundPenalty", "Low Ground");
            DrawPair("surroundBonus", "Surround", "longWeaponProximityPenalty", "Proximity Pen.");
            
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // 2. Ranged Modifiers
            EditorGUILayout.LabelField("Ranged Modifiers", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            DrawPair("rangedHighGroundBonus", "High Ground", "rangedLowGroundPenalty", "Low Ground");
            DrawPair("rangedDistancePenalty", "Dist. Penalty", "coverMissChance", "Cover Miss %");
            DrawPair("scatterHitPenalty", "Scatter Hit Pen.", "scatterDamagePenalty", "Scatter Dmg Pen.");
            
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPair(string prop1Name, string label1, string prop2Name, string label2)
        {
            var prop1 = serializedObject.FindProperty(prop1Name);
            var prop2 = serializedObject.FindProperty(prop2Name);

            EditorGUILayout.BeginHorizontal();
            
            if (prop1 != null)
            {
                EditorGUILayout.BeginHorizontal(GUILayout.MinWidth(100));
                float originalLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 80;
                EditorGUILayout.PropertyField(prop1, new GUIContent(label1));
                EditorGUIUtility.labelWidth = originalLabelWidth;
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(10);

            if (prop2 != null)
            {
                EditorGUILayout.BeginHorizontal(GUILayout.MinWidth(100));
                float originalLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 80;
                EditorGUILayout.PropertyField(prop2, new GUIContent(label2));
                EditorGUIUtility.labelWidth = originalLabelWidth;
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
