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
            
            // Using a more flexible approach for columns
            if (prop1 != null)
            {
                EditorGUILayout.BeginVertical(GUILayout.MinWidth(150));
                float originalLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 120;
                
                EditorGUI.BeginChangeCheck();
                float val = EditorGUILayout.FloatField(label1, prop1.floatValue);
                if (EditorGUI.EndChangeCheck()) prop1.floatValue = val;

                EditorGUIUtility.labelWidth = originalLabelWidth;
                EditorGUILayout.EndVertical();
            }

            GUILayout.Space(10);

            if (prop2 != null)
            {
                EditorGUILayout.BeginVertical(GUILayout.MinWidth(150));
                float originalLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 120;
                
                EditorGUI.BeginChangeCheck();
                float val = EditorGUILayout.FloatField(label2, prop2.floatValue);
                if (EditorGUI.EndChangeCheck()) prop2.floatValue = val;

                EditorGUIUtility.labelWidth = originalLabelWidth;
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
