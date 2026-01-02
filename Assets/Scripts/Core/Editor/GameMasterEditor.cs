using UnityEditor;
using UnityEngine;

namespace HexGame.Editor
{
    [CustomEditor(typeof(GameMaster))]
    public class GameMasterEditor : UnityEditor.Editor
    {
        private UnityEditor.Editor rulesetEditor;
        private Ruleset lastRuleset;

        private void OnDisable()
        {
            if (rulesetEditor != null) DestroyImmediate(rulesetEditor);
        }

        public override void OnInspectorGUI()
        {
            GameMaster gm = (GameMaster)target;
            serializedObject.Update();

            // Draw the GameMaster properties (like the ruleset field itself)
            DrawPropertiesExcluding(serializedObject, "m_Script");

            if (gm.ruleset != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Active Ruleset Settings", EditorStyles.boldLabel);
                
                // Use a help box or specialized background to distinguish the Ruleset's area
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                // Cache/Update the Ruleset editor
                if (rulesetEditor == null || lastRuleset != gm.ruleset)
                {
                    if (rulesetEditor != null) DestroyImmediate(rulesetEditor);
                    rulesetEditor = UnityEditor.Editor.CreateEditor(gm.ruleset);
                    lastRuleset = gm.ruleset;
                }

                // Draw the Ruleset's custom UI (dropdowns, foldouts, etc.)
                if (rulesetEditor != null)
                {
                    rulesetEditor.OnInspectorGUI();
                }

                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("Assign a Ruleset to see its configuration.", MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
