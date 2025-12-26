using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace HexGame.Units.Editor
{
    public static class UnitEditorUI
    {
        public static void DrawSchemaEditor(UnitSchema schema, ref Vector2 scrollPos)
        {
            if (schema == null) return;

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUILayout.LabelField("Stat Definitions (ID Based)", EditorStyles.boldLabel);

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

                // Reordering
                if (GUILayout.Button("▲", GUILayout.Width(25)) && i > 0)
                {
                    var temp = schema.definitions[i];
                    schema.definitions[i] = schema.definitions[i - 1];
                    schema.definitions[i - 1] = temp;
                    EditorUtility.SetDirty(schema);
                }
                if (GUILayout.Button("▼", GUILayout.Width(25)) && i < schema.definitions.Count - 1)
                {
                    var temp = schema.definitions[i];
                    schema.definitions[i] = schema.definitions[i + 1];
                    schema.definitions[i + 1] = temp;
                    EditorUtility.SetDirty(schema);
                }

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
            if (set == null) return;

            // Missing Schema Handling
            if (set.schema == null)
            {
                EditorGUILayout.HelpBox("Unit Set has no Schema assigned.", MessageType.Error);
                if (GUILayout.Button("Create Schema from Data"))
                {
                    CreateSchemaFromSet(set);
                }
                return; // Stop drawing editor
            }

            GUILayout.Space(10);
            EditorGUILayout.LabelField($"Units ({set.schema.name})", EditorStyles.boldLabel);

            var schemaDefs = set.schema.definitions;

            // List - Start ScrollView BEFORE Header to ensure headers scroll with content
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // Check for extras and offer to add them
            HashSet<string> schemaIds = new HashSet<string>(schemaDefs.Select(d => d.id));
            HashSet<string> extraIds = new HashSet<string>();
            foreach(var u in set.units) 
            {
                if(u.Stats != null)
                {
                    foreach(var s in u.Stats) 
                    {
                        if (!schemaIds.Contains(s.id)) extraIds.Add(s.id);
                    }
                }
            }

            if (extraIds.Count > 0)
            {
                GUI.backgroundColor = new Color(1f, 0.8f, 0.4f); // Light Orange
                if (GUILayout.Button($"Add {extraIds.Count} Missing Columns to Schema"))
                {
                    foreach(var id in extraIds) 
                    {
                         set.schema.definitions.Add(new UnitStatDefinition { id = id, name = id });
                    }
                    EditorUtility.SetDirty(set.schema);
                }
                GUI.backgroundColor = Color.white;
            }

            // Header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name", GUILayout.Width(120));
            foreach (var def in schemaDefs)
            {
                EditorGUILayout.LabelField(new GUIContent(def.id, def.name), GUILayout.Width(40));
            }
            
            // Header for potential extras
            EditorGUILayout.LabelField("Extras / Orphans", GUILayout.Width(100));

            EditorGUILayout.EndHorizontal();
            
            for (int i = 0; i < set.units.Count; i++)
            {
                UnitType unit = set.units[i];
                if (unit == null) continue;

                if (unit.Stats == null) unit.Stats = new List<UnitStatValue>();

                EditorGUILayout.BeginHorizontal("box");
                unit.Name = EditorGUILayout.TextField(unit.Name, GUILayout.Width(120));

                // 1. Draw Schema Stats (Main Columns)
                foreach (var def in schemaDefs)
                {
                    int index = unit.Stats.FindIndex(s => s.id == def.id);
                    
                    if (index != -1)
                    {
                        // Found: Draw normally
                        var stat = unit.Stats[index];
                        int newVal = EditorGUILayout.IntField(stat.value, GUILayout.Width(40));
                        if (newVal != stat.value)
                        {
                            // Structs are value types, need to replace in list
                            stat.value = newVal;
                            unit.Stats[index] = stat;
                            EditorUtility.SetDirty(set);
                        }
                    }
                    else
                    {
                        // Missing: Draw Red Box with 0
                        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f); // Red
                        int newVal = EditorGUILayout.IntField(0, GUILayout.Width(40));
                        GUI.backgroundColor = Color.white;

                        if (newVal != 0) 
                        {
                            unit.Stats.Add(new UnitStatValue { id = def.id, value = newVal });
                            EditorUtility.SetDirty(set);
                        }
                    }
                }

                // 2. Identify and Draw "Extra" Stats (Not in Schema)
                // schemaIds is already computed
                
                for (int s = 0; s < unit.Stats.Count; s++)
                {
                    var stat = unit.Stats[s];
                    if (!schemaIds.Contains(stat.id))
                    {
                        // Found an extra stat
                        GUI.backgroundColor = Color.yellow;
                        EditorGUILayout.LabelField(stat.id, GUILayout.Width(30)); // Show ID
                        int newVal = EditorGUILayout.IntField(stat.value, GUILayout.Width(30));
                        GUI.backgroundColor = Color.white;

                        if (newVal != stat.value)
                        {
                            stat.value = newVal;
                            unit.Stats[s] = stat;
                            EditorUtility.SetDirty(set);
                        }
                    }
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

        private static void CreateSchemaFromSet(UnitSet set)
        {
             string folder = "Assets/Data/Schemas";
             if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

             // 1. Analyze Name for Pattern [SchemaName]_[SetName]
             // Use set.name (asset filename) as the source of truth, as set.setName might be out of sync
             string nameToAnalyze = set.name;
             string schemaNameCandidate = null;
             
             if (!string.IsNullOrEmpty(nameToAnalyze) && nameToAnalyze.Contains('_'))
             {
                 var parts = nameToAnalyze.Split('_');
                 if (parts.Length >= 2 && !string.IsNullOrEmpty(parts[0]))
                 {
                     schemaNameCandidate = parts[0];
                 }
             }

             // 2. Check if it exists
             if (schemaNameCandidate != null)
             {
                 string existingPath = Path.Combine(folder, schemaNameCandidate + ".asset");
                 UnitSchema existingSchema = AssetDatabase.LoadAssetAtPath<UnitSchema>(existingPath);
                 
                 if (existingSchema != null)
                 {
                     if (EditorUtility.DisplayDialog("Schema Found", 
                         $"A schema named '{schemaNameCandidate}' already exists.\nDo you want to assign it to this set?", 
                         "Yes, Assign", "No, Create New"))
                     {
                         set.schema = existingSchema;
                         EditorUtility.SetDirty(set);
                         return;
                     }
                 }
             }

             // 3. Create New (if not found or user chose new)
             List<string> orderedIds = new List<string>();
             HashSet<string> knownIds = new HashSet<string>();
             
             foreach(var u in set.units)
             {
                 if (u.Stats != null)
                 {
                    foreach(var s in u.Stats) 
                    {
                        if (!string.IsNullOrEmpty(s.id) && !knownIds.Contains(s.id))
                        {
                            knownIds.Add(s.id);
                            orderedIds.Add(s.id);
                        }
                    }
                 }
             }
             
             string baseName = schemaNameCandidate ?? (set.setName + "_Schema");
             string path = Path.Combine(folder, baseName + ".asset");
             path = AssetDatabase.GenerateUniqueAssetPath(path);
             
             UnitSchema newSchema = ScriptableObject.CreateInstance<UnitSchema>();
             newSchema.definitions = new List<UnitStatDefinition>();
             foreach(var id in orderedIds)
             {
                 newSchema.definitions.Add(new UnitStatDefinition { id = id, name = id });
             }
             
             AssetDatabase.CreateAsset(newSchema, path);
             AssetDatabase.SaveAssets();
             
             // 4. Assign
             set.schema = newSchema;
             EditorUtility.SetDirty(set);
             
             EditorGUIUtility.PingObject(newSchema);
        }
    }
}