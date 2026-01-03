using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace HexGame.Editor
{
    [CustomEditor(typeof(BattleBrothersRuleset))]
    public class BattleBrothersRulesetEditor : UnityEditor.Editor
    {
        private string[] unitSchemaIds;
        private int selectedUnitSchemaIndex = -1;
        private Dictionary<string, UnityEditor.Editor> moduleEditors = new Dictionary<string, UnityEditor.Editor>();
        private static Dictionary<string, bool> foldouts = new Dictionary<string, bool>();

        private void OnEnable()
        {
            RefreshSchemaList();
        }

        private void OnDisable()
        {
            foreach (var editor in moduleEditors.Values) if (editor != null) DestroyImmediate(editor);
            moduleEditors.Clear();
        }

        private void RefreshSchemaList()
        {
            string path = "Assets/Data/Schemas";
            if (!Directory.Exists(path)) { unitSchemaIds = new string[0]; return; }

            unitSchemaIds = Directory.GetFiles(path, "*.json")
                .Select(f => {
                    string json = File.ReadAllText(f);
                    return ParseId(json) ?? Path.GetFileNameWithoutExtension(f);
                }).Distinct().ToArray();

            var ruleset = (Ruleset)target;
            if (!string.IsNullOrEmpty(ruleset.unitSchema))
                selectedUnitSchemaIndex = System.Array.IndexOf(unitSchemaIds, ruleset.unitSchema);
        }

        private string ParseId(string json)
        {
            int idx = json.IndexOf("\"id\":");
            if (idx == -1) return null;
            int q1 = json.IndexOf("\"", idx + 5);
            if (q1 == -1) return null;
            int q2 = json.IndexOf("\"", q1 + 1);
            return (q2 != -1) ? json.Substring(q1 + 1, q2 - q1 - 1) : null;
        }

        public override void OnInspectorGUI()
        {
            var ruleset = (BattleBrothersRuleset)target;
            serializedObject.Update();

            // 1. Schema Selection
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUILayout.Popup("Unit schema", selectedUnitSchemaIndex, unitSchemaIds);
            if (EditorGUI.EndChangeCheck())
            {
                selectedUnitSchemaIndex = newIndex;
                ruleset.unitSchema = unitSchemaIds[selectedUnitSchemaIndex];
                EditorUtility.SetDirty(ruleset);
            }
            if (GUILayout.Button("Refresh", GUILayout.Width(60))) RefreshSchemaList();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 2. Flow Group (Ignore Flags)
            bool debugFoldout = BeginGroup("Debug Settings", "flow_flags_foldout");
            if (debugFoldout)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Ignore...", GUILayout.Width(60));
                DrawToggle(serializedObject.FindProperty("ignoreAPs"), "APs", 40);
                DrawToggle(serializedObject.FindProperty("ignoreFatigue"), "Fatigue", 60);
                DrawToggle(serializedObject.FindProperty("ignoreMoveOrder"), "Move Order", 80);
                EditorGUILayout.EndHorizontal();
            }
            EndGroup(debugFoldout);


            // Special handling for Flow Module to include buttons and custom queue
            DrawFlowModule(serializedObject.FindProperty("flow"), ruleset);

            // 3. Execution Group
            bool execFoldout = BeginGroup("Execution Settings", "exec_foldout");
            if (execFoldout)
            {
                EditorGUILayout.BeginHorizontal();
                DrawCompactProp(serializedObject.FindProperty("transitionSpeed"), "Speed", 50);
                GUILayout.Space(20);
                DrawCompactProp(serializedObject.FindProperty("transitionPause"), "Pause", 50);
                EditorGUILayout.EndHorizontal();
            }
            EndGroup(execFoldout);

            // 4. Modules
            DrawModule(serializedObject.FindProperty("movement"));
            DrawModule(serializedObject.FindProperty("combat"));
            DrawModule(serializedObject.FindProperty("tactical"));
            

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawFlowModule(SerializedProperty prop, BattleBrothersRuleset ruleset)
        {
            if (prop == null || prop.objectReferenceValue == null) { EditorGUILayout.PropertyField(prop); return; }
            var flow = (FlowModule)prop.objectReferenceValue;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            bool state = foldouts.ContainsKey("flow_module") ? foldouts["flow_module"] : true;
            
            EditorGUILayout.BeginHorizontal();
            state = EditorGUILayout.Foldout(state, "Flow Control", true);
            EditorGUILayout.PropertyField(prop, GUIContent.none);
            EditorGUILayout.EndHorizontal();
            foldouts["flow_module"] = state;

            if (state)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField($"Round: {flow.roundNumber}", EditorStyles.miniLabel);
                
                // Buttons
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Start Combat")) ruleset.StartCombat();
                if (GUILayout.Button("End Combat")) ruleset.StopCombat();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Wait")) ruleset.WaitTurn();
                if (GUILayout.Button("End Turn")) ruleset.AdvanceTurn();
                EditorGUILayout.EndHorizontal();

                // Queue
                var queue = flow.TurnQueue;
                if (queue != null && queue.Count > 0)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Turn Queue", EditorStyles.boldLabel);
                    for (int i = 0; i < queue.Count; i++)
                    {
                        var u = queue[i];
                        if (u == null) continue;
                        
                        int ini = ruleset.GetTurnPriority(u);
                        // If unit is in the 'waited' set, it might need the penalty shown, 
                        // but FlowModule.TurnQueue getter already returns the sorted version.
                        
                        int ap = u.GetStat("AP");
                        int fat = u.GetStat("FAT");
                        int mfat = u.GetBaseStat("FAT", 100);

                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button($"{i + 1}. {u.UnitName}", EditorStyles.label, GUILayout.Width(140)))
                        {
                            Selection.activeGameObject = u.gameObject;
                            EditorGUIUtility.PingObject(u.gameObject);
                        }
                        EditorGUILayout.LabelField($"(INI: {ini}, AP: {ap}, FAT: {fat}/{mfat})", EditorStyles.miniLabel);
                        EditorGUILayout.EndHorizontal();
                    }
                }

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawToggle(SerializedProperty prop, string label, float width)
        {
            if (prop == null) return;
            EditorGUILayout.BeginHorizontal(GUILayout.Width(width + 30));
            EditorGUILayout.LabelField(label, GUILayout.Width(width));
            prop.boolValue = EditorGUILayout.Toggle(prop.boolValue, GUILayout.Width(20));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawCompactProp(SerializedProperty prop, string label, float width)
        {
            if (prop == null) return;
            EditorGUILayout.BeginHorizontal(GUILayout.Width(width + 60));
            EditorGUILayout.LabelField(label, GUILayout.Width(width));
            EditorGUILayout.PropertyField(prop, GUIContent.none, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();
        }

        private bool BeginGroup(string label, string key)
        {
            bool state = foldouts.ContainsKey(key) ? foldouts[key] : true;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            state = EditorGUILayout.Foldout(state, label, true);
            foldouts[key] = state;
            if (state) EditorGUI.indentLevel++;
            return state;
        }

        private void EndGroup(bool state)
        {
            if (state) EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private void DrawModule(SerializedProperty prop)
        {
            if (prop == null || prop.objectReferenceValue == null) { EditorGUILayout.PropertyField(prop); return; }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            bool state = foldouts.ContainsKey(prop.name) ? foldouts[prop.name] : false;
            
            EditorGUILayout.BeginHorizontal();
            foldouts[prop.name] = EditorGUILayout.Foldout(state, prop.displayName, true);
            EditorGUILayout.PropertyField(prop, GUIContent.none);
            EditorGUILayout.EndHorizontal();

            if (foldouts[prop.name])
            {
                EditorGUI.indentLevel++;
                var editor = moduleEditors.ContainsKey(prop.name) ? moduleEditors[prop.name] : null;
                if (editor == null || editor.target != prop.objectReferenceValue)
                {
                    if (editor != null) DestroyImmediate(editor);
                    editor = UnityEditor.Editor.CreateEditor(prop.objectReferenceValue);
                    moduleEditors[prop.name] = editor;
                }
                if (editor != null) { EditorGUILayout.Space(2); editor.OnInspectorGUI(); }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }
    }
}
