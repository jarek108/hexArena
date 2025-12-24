using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace HexGame.Units.Editor
{
    public static class UnitEditorUI
    {
        public static void DrawSchemaEditor(UnitSchema schema, ref Vector2 scrollPos)
        {
            if (schema == null) return;

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUILayout.LabelField("Stat Definitions", EditorStyles.boldLabel);

            if (schema.definitions == null) schema.definitions = new List<UnitStatDefinition>();

            HashSet<string> seenIds = new HashSet<string>();
            List<int> duplicateIndices = new List<int>();

            // Validation
            for (int i = 0; i < schema.definitions.Count; i++)
            {
                string id = schema.definitions[i].id;
                if (string.IsNullOrEmpty(id)) continue;
                if (seenIds.Contains(id)) duplicateIndices.Add(i);
                seenIds.Add(id);
            }

            // Drawing
            for (int i = 0; i < schema.definitions.Count; i++)
            {
                UnitStatDefinition def = schema.definitions[i];
                
                GUIStyle style = EditorStyles.helpBox;
                if (duplicateIndices.Contains(i)) GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
                else GUI.backgroundColor = Color.white;

                EditorGUILayout.BeginVertical(style);
                GUI.backgroundColor = Color.white;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("ID", GUILayout.Width(20));
                def.id = EditorGUILayout.TextField(def.id, GUILayout.Width(80));
                
                EditorGUILayout.LabelField("Desc", GUILayout.Width(40));
                def.name = EditorGUILayout.TextField(def.name);

                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    schema.definitions.RemoveAt(i);
                    i--;
                    EditorUtility.SetDirty(schema);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            if (duplicateIndices.Count > 0)
            {
                EditorGUILayout.HelpBox("Duplicate IDs detected! IDs must be unique.", MessageType.Error);
            }

            if (GUILayout.Button("Add Stat"))
            {
                schema.definitions.Add(new UnitStatDefinition { id = "New", name = "Description" });
                EditorUtility.SetDirty(schema);
            }
            
            EditorGUILayout.EndScrollView();
        }

        public static void DrawUnitSetEditor(UnitSet set, ref Vector2 scrollPos)
        {
            if (set == null || set.schema == null) return;

            GUILayout.Space(10);
            EditorGUILayout.LabelField($"Units ({set.schema.name})", EditorStyles.boldLabel);

            var columns = set.schema.definitions;

            // List - Start ScrollView BEFORE Header to ensure headers scroll with content
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // Header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name", GUILayout.Width(120));
            foreach (var col in columns)
            {
                EditorGUILayout.LabelField(new GUIContent(col.id, col.name), GUILayout.Width(40));
            }
            
            // Draw dummy headers for orphans if any unit has them
            int maxStats = 0;
            foreach(var u in set.units) if(u != null) maxStats = Mathf.Max(maxStats, u.Stats.Count);
            int orphans = maxStats - columns.Count;
            if(orphans > 0)
            {
                for(int i=0; i<orphans; i++) EditorGUILayout.LabelField(new GUIContent("?", "Orphaned Data"), GUILayout.Width(40));
            }

            EditorGUILayout.LabelField("", GUILayout.Width(20));
            EditorGUILayout.EndHorizontal();
            
            for (int i = 0; i < set.units.Count; i++)
            {
                UnitType unit = set.units[i];
                if (unit == null) continue;

                // Sync: Only ADD zeros, never remove to preserve data
                while (unit.Stats.Count < columns.Count) unit.Stats.Add(0);
                
                EditorGUILayout.BeginHorizontal("box");
                unit.Name = EditorGUILayout.TextField(unit.Name, GUILayout.Width(120));

                // Draw Schema Stats
                for (int s = 0; s < columns.Count; s++)
                {
                    unit.Stats[s] = EditorGUILayout.IntField(unit.Stats[s], GUILayout.Width(40));
                }

                // Draw Orphaned Stats in Red
                for (int s = columns.Count; s < unit.Stats.Count; s++)
                {
                    GUI.backgroundColor = new Color(1f, 0.6f, 0.6f); // Red warning
                    unit.Stats[s] = EditorGUILayout.IntField(unit.Stats[s], GUILayout.Width(40));
                    GUI.backgroundColor = Color.white;
                }

                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    set.units.RemoveAt(i);
                    i--;
                    EditorUtility.SetDirty(set);
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Add Unit Type"))
            {
                set.units.Add(new UnitType());
                EditorUtility.SetDirty(set);
            }

            EditorGUILayout.EndScrollView();
        }
    }
}