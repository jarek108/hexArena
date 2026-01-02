using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace HexGame.Editor
{
    [CustomEditor(typeof(Ruleset), true)]
    public class RulesetEditor : UnityEditor.Editor
    {
        private string[] schemaIds;
        private int selectedSchemaIndex = -1;
        private Dictionary<string, UnityEditor.Editor> moduleEditors = new Dictionary<string, UnityEditor.Editor>();
        private static Dictionary<string, bool> foldouts = new Dictionary<string, bool>();

        private void OnEnable()
        {
            RefreshSchemaList();
        }

        private void OnDisable()
        {
            foreach (var editor in moduleEditors.Values)
            {
                if (editor != null) DestroyImmediate(editor);
            }
            moduleEditors.Clear();
        }

        private void RefreshSchemaList()
        {
            string schemaPath = "Assets/Data/Schemas";
            if (!Directory.Exists(schemaPath))
            {
                schemaIds = new string[0];
                return;
            }

            var files = Directory.GetFiles(schemaPath, "*.json");
            List<string> ids = new List<string>();
            foreach (var file in files)
            {
                string json = File.ReadAllText(file);
                string id = ParseIdFromJson(json);
                if (!string.IsNullOrEmpty(id)) ids.Add(id);
                else ids.Add(Path.GetFileNameWithoutExtension(file));
            }
            schemaIds = ids.Distinct().ToArray();

            Ruleset ruleset = (Ruleset)target;
            if (!string.IsNullOrEmpty(ruleset.schema))
            {
                selectedSchemaIndex = System.Array.IndexOf(schemaIds, ruleset.schema);
            }
        }

        private string ParseIdFromJson(string json)
        {
            int idIdx = json.IndexOf("\"id\":");
            if (idIdx != -1)
            {
                int quoteStart = json.IndexOf("\"", idIdx + 5);
                if (quoteStart != -1)
                {
                    int start = quoteStart + 1;
                    int end = json.IndexOf("\"", start);
                    if (end != -1) return json.Substring(start, end - start);
                }
            }
            return null;
        }

        private bool inGroup = false;
        private bool groupFoldout = false;

        public override void OnInspectorGUI()
        {
            Ruleset ruleset = (Ruleset)target;
            serializedObject.Update();

            EditorGUILayout.LabelField("Ruleset Core", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUILayout.Popup("Schema", selectedSchemaIndex, schemaIds);
            if (EditorGUI.EndChangeCheck())
            {
                selectedSchemaIndex = newIndex;
                ruleset.schema = schemaIds[selectedSchemaIndex];
                EditorUtility.SetDirty(ruleset);
            }
            if (GUILayout.Button("Refresh", GUILayout.Width(60)))
            {
                RefreshSchemaList();
            }
            EditorGUILayout.EndHorizontal();

            if (selectedSchemaIndex == -1 && !string.IsNullOrEmpty(ruleset.schema))
            {
                EditorGUILayout.HelpBox($"Current schema '{{ruleset.schema}}' not found in Assets/Data/Schemas!", MessageType.Warning);
            }

            EditorGUILayout.Space();

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            
            string currentGroupName = "";
            inGroup = false;
            groupFoldout = false;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.name == "m_Script" || iterator.name == "schema") continue;

                // Modules
                if (iterator.name == "movement" || iterator.name == "combat" || iterator.name == "tactical")
                {
                    EndGroup();
                    DrawModuleFoldout(iterator);
                    continue;
                }
                
                // Grouping Logic
                string targetGroup = "";
                if (iterator.name.StartsWith("ignore")) targetGroup = "Flow";
                else if (iterator.name.Contains("transition") || iterator.name.Contains("Speed") || iterator.name.Contains("Pause")) targetGroup = "Execution Settings";

                if (targetGroup != currentGroupName)
                {
                    EndGroup();
                    currentGroupName = targetGroup;
                    if (!string.IsNullOrEmpty(currentGroupName))
                    {
                        string key = currentGroupName.Replace(" ", "_").ToLower() + "_foldout";
                        groupFoldout = BeginGroup(currentGroupName, key);
                        inGroup = true;

                        if (currentGroupName == "Flow" && groupFoldout)
                        {
                            DrawFlowRow();
                        }
                        if (currentGroupName == "Execution Settings" && groupFoldout)
                        {
                            DrawExecutionSettings();
                        }
                    }
                }

                if (!inGroup || groupFoldout)
                {
                    if (currentGroupName == "Flow" && iterator.name.StartsWith("ignore"))
                        continue;
                    if (currentGroupName == "Execution Settings" && (iterator.name.Contains("transition") || iterator.name.Contains("Speed") || iterator.name.Contains("Pause")))
                        continue;

                    EditorGUILayout.PropertyField(iterator);
                }
            }
            EndGroup();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawFlowRow()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Ignore...", GUILayout.Width(60));
            
            DrawSmallToggle("ignoreAPs", "APs", 60);
            DrawSmallToggle("ignoreFatigue", "Fatigue", 80);
            DrawSmallToggle("ignoreMoveOrder", "Move Order", 110);
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2);
        }

        private void DrawExecutionSettings()
        {
            EditorGUILayout.BeginHorizontal();
            
            // Assuming we have transitionSpeed and transitionPause
            DrawCompactProperty("transitionSpeed", "Speed", 60);
            GUILayout.Space(10);
            DrawCompactProperty("transitionPause", "Pause", 60);
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2);
        }

        private void DrawCompactProperty(string propName, string label, float width)
        {
            SerializedProperty prop = serializedObject.FindProperty(propName);
            if (prop == null) return;

            EditorGUILayout.BeginHorizontal(GUILayout.Width(width + 50));
            float originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = width;
            EditorGUILayout.PropertyField(prop, new GUIContent(label));
            EditorGUIUtility.labelWidth = originalLabelWidth;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSmallToggle(string propName, string label, float width)
        {
            SerializedProperty prop = serializedObject.FindProperty(propName);
            if (prop == null) return;

            float originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = width;
            EditorGUILayout.PropertyField(prop, new GUIContent(label), GUILayout.Width(width + 20));
            EditorGUIUtility.labelWidth = originalLabelWidth;
            GUILayout.Space(5);
        }

        private bool BeginGroup(string label, string foldoutKey)
        {
            bool foldout = foldouts.ContainsKey(foldoutKey) ? foldouts[foldoutKey] : true;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            foldout = EditorGUILayout.Foldout(foldout, label, true);
            foldouts[foldoutKey] = foldout;
            if (foldout) EditorGUI.indentLevel++;
            return foldout;
        }

        private void EndGroup()
        {
            if (inGroup)
            {
                if (groupFoldout) EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
                inGroup = false;
                groupFoldout = false;
            }
        }

        private void DrawModuleFoldout(SerializedProperty prop)
        {
            Object targetObject = prop.objectReferenceValue;

            if (targetObject == null)
            {
                EditorGUILayout.PropertyField(prop);
                return;
            }

            // Always show foldout if not null (Tactical will just be empty)
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            bool foldout = foldouts.ContainsKey(prop.name) ? foldouts[prop.name] : false;
            
            EditorGUILayout.BeginHorizontal();
            foldout = EditorGUILayout.Foldout(foldout, prop.displayName, true);
            EditorGUILayout.PropertyField(prop, GUIContent.none);
            EditorGUILayout.EndHorizontal();
            
            foldouts[prop.name] = foldout;

            if (foldout)
            {
                EditorGUI.indentLevel++;
                if (!moduleEditors.ContainsKey(prop.name) || moduleEditors[prop.name] == null || moduleEditors[prop.name].target != targetObject)
                {
                    if (moduleEditors.ContainsKey(prop.name) && moduleEditors[prop.name] != null)
                        DestroyImmediate(moduleEditors[prop.name]);
                    
                    moduleEditors[prop.name] = UnityEditor.Editor.CreateEditor(targetObject);
                }

                if (moduleEditors[prop.name] != null)
                {
                    EditorGUILayout.Space(2);
                    moduleEditors[prop.name].OnInspectorGUI();
                }
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }
    }
}
