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
            DrawPropertiesExcluding(serializedObject, "m_Script", "ruleset", "turnQueue");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Turn Management", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Round: {gm.roundNumber}", EditorStyles.miniLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Start Combat / Round"))
            {
                gm.StartNewRound();
            }
            if (GUILayout.Button("End Combat"))
            {
                gm.EndCombat();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Wait"))
            {
                gm.WaitCurrentTurn();
            }
            if (GUILayout.Button("End Turn"))
            {
                gm.EndCurrentTurn();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            // Queue display
            var queue = gm.TurnQueue;
            if (queue != null && queue.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Turn Queue", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                for (int i = 0; i < queue.Count; i++)
                {
                    var u = queue[i];
                    if (u == null) continue;
                    
                    int ini = gm.ruleset?.GetTurnPriority(u) ?? 0;
                    int ap = u.GetStat("AP");
                    int fat = u.GetStat("FAT");
                    int mfat = u.GetBaseStat("FAT", 100);

                    EditorGUILayout.BeginHorizontal();
                    
                    // Column 1: Unit Selection Button
                    if (GUILayout.Button($"{i + 1}. {u.UnitName}", EditorStyles.label, GUILayout.Width(150)))
                    {
                        Selection.activeGameObject = u.gameObject;
                        EditorGUIUtility.PingObject(u.gameObject);
                    }

                    // Column 2: Stats
                    EditorGUILayout.LabelField($"(INI: {ini}, AP: {ap}, FAT: {fat}/{mfat})", EditorStyles.miniLabel);
                    
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }

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
