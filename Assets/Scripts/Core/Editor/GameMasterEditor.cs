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

            // Draw the GameMaster properties (primarily the ruleset field)
            DrawPropertiesExcluding(serializedObject, "m_Script");

            if (gm.ruleset != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Active Ruleset Configuration", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                if (rulesetEditor == null || lastRuleset != gm.ruleset)
                {
                    if (rulesetEditor != null) DestroyImmediate(rulesetEditor);
                    rulesetEditor = UnityEditor.Editor.CreateEditor(gm.ruleset);
                    lastRuleset = gm.ruleset;
                }

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
