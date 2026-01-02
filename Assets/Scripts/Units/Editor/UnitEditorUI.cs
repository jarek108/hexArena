using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditorInternal;

namespace HexGame.Units.Editor
{
    public static class UnitEditorUI
    {
        private static ReorderableList schemaList;
        private static UnitSchema lastSchema;

        public static void DrawSchemaEditor(UnitSchema schema, ref Vector2 scrollPos)
        {
            if (schema == null) return;

            if (schema.definitions == null) schema.definitions = new List<UnitStatDefinition>();

            if (schemaList == null || lastSchema != schema)
            {
                lastSchema = schema;
                schemaList = new ReorderableList(schema.definitions, typeof(UnitStatDefinition), true, true, true, true);
                
                schemaList.drawHeaderCallback = (Rect rect) => {
                    EditorGUI.LabelField(rect, "Stat Definitions (ID Based)");
                };

                schemaList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
                    var element = schema.definitions[index];
                    rect.y += 2;
                    float width = rect.width;
                    
                    float x = rect.x;
                    EditorGUI.LabelField(new Rect(x, rect.y, 20, EditorGUIUtility.singleLineHeight), "ID");
                    x += 22;
                    element.id = EditorGUI.TextField(new Rect(x, rect.y, width * 0.25f, EditorGUIUtility.singleLineHeight), element.id);
                    x += width * 0.25f + 5;
                    
                    EditorGUI.LabelField(new Rect(x, rect.y, 35, EditorGUIUtility.singleLineHeight), "Desc");
                    x += 37;
                    element.name = EditorGUI.TextField(new Rect(x, rect.y, width - x + rect.x - 25, EditorGUIUtility.singleLineHeight), element.name);

                    if (GUI.Button(new Rect(rect.x + width - 20, rect.y, 20, EditorGUIUtility.singleLineHeight), "x", EditorStyles.miniButton))
                    {
                        schema.definitions.RemoveAt(index);
                    }
                };
            }

            schemaList.DoLayoutList();
        }

        public static void DrawUnitSetEditor(UnitSet set, ref Vector2 scrollPos)
        {
            if (set == null) return;

            // Missing Schema Handling
            if (string.IsNullOrEmpty(set.schemaId))
            {
                EditorGUILayout.HelpBox("Unit Set has no Schema ID assigned.", MessageType.Error);
                if (GUILayout.Button("Create Schema from Data"))
                {
                    CreateSchemaFromSet(set);
                }
            }

            GUILayout.Space(10);
            string schemaName = "None";
            if (!string.IsNullOrEmpty(set.schemaId)) 
            {
                schemaName = set.schemaId;
            }
            EditorGUILayout.LabelField($"Units ({schemaName})", EditorStyles.boldLabel);

            var schemaDefs = set.schemaDefinitions;

            if (schemaDefs == null)
            {
                EditorGUILayout.HelpBox($"Schema '{set.schemaId}' not found or invalid.", MessageType.Warning);
                schemaDefs = new List<UnitStatDefinition>();
            }

            HashSet<string> schemaIds = new HashSet<string>(schemaDefs.Select(d => d.id));
            List<string> extraIds = new List<string>();
            foreach(var u in set.units) 
            {
                if(u.Stats != null)
                {
                    foreach(var s in u.Stats) 
                    {
                        if (!schemaIds.Contains(s.id) && !extraIds.Contains(s.id)) extraIds.Add(s.id);
                    }
                }
            }

            if (extraIds.Count > 0)
            {
                GUI.backgroundColor = new Color(1f, 0.8f, 0.4f); // Light Orange
                EditorGUILayout.HelpBox($"Found {extraIds.Count} stats not in schema. Add to schema manually via Unit Data Editor.", MessageType.Warning);
                GUI.backgroundColor = Color.white;
            }

            // Header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("ID", GUILayout.Width(60));
            EditorGUILayout.LabelField("Name", GUILayout.Width(120));
            foreach (var def in schemaDefs)
            {
                EditorGUILayout.LabelField(new GUIContent(def.id, def.name), GUILayout.Width(40));
            }
            
            foreach (var extraId in extraIds)
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(40));
                EditorGUILayout.LabelField(extraId, EditorStyles.miniLabel, GUILayout.Width(40));
                if (GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(20)))
                {
                    foreach (var u in set.units) u.Stats.RemoveAll(s => s.id == extraId);
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndHorizontal();
            
            for (int i = 0; i < set.units.Count; i++)
            {
                UnitType unit = set.units[i];
                if (unit == null) continue;

                if (unit.Stats == null) unit.Stats = new List<UnitStatValue>();

                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.LabelField(unit.id, EditorStyles.miniLabel, GUILayout.Width(60));
                unit.Name = EditorGUILayout.TextField(unit.Name, GUILayout.Width(120));

                foreach (var def in schemaDefs)
                {
                    int index = unit.Stats.FindIndex(s => s.id == def.id);
                    
                    if (index != -1)
                    {
                        var stat = unit.Stats[index];
                        int newVal = EditorGUILayout.IntField(stat.value, GUILayout.Width(40));
                        if (newVal != stat.value)
                        {
                            stat.value = newVal;
                            unit.Stats[index] = stat;
                        }
                    }
                    else
                    {
                        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f); // Red
                        int newVal = EditorGUILayout.IntField(0, GUILayout.Width(40));
                        GUI.backgroundColor = Color.white;

                        if (newVal != 0) 
                        {
                            unit.Stats.Add(new UnitStatValue { id = def.id, value = newVal });
                        }
                    }
                }

                foreach (var extraId in extraIds)
                {
                    int index = unit.Stats.FindIndex(s => s.id == extraId);
                    if (index != -1)
                    {
                        var stat = unit.Stats[index];
                        GUI.backgroundColor = Color.yellow;
                        int newVal = EditorGUILayout.IntField(stat.value, GUILayout.Width(40));
                        GUI.backgroundColor = Color.white;

                        if (newVal != stat.value)
                        {
                            stat.value = newVal;
                            unit.Stats[index] = stat;
                        }
                    }
                    else
                    {
                        GUILayout.Space(44); 
                    }
                }

                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    set.units.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Add Unit Type"))
            {
                var newType = new UnitType();
                newType.id = UnitType.GenerateId();
                set.units.Add(newType);
            }
        }

        private static void CreateSchemaFromSet(UnitSet set)
        {
             string folder = "Assets/Data/Schemas";
             if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

             // Note: set.name is no longer available since it's not an SO. We use setName.
             string nameToAnalyze = set.setName;
             string schemaNameCandidate = null;
             
             if (!string.IsNullOrEmpty(nameToAnalyze) && nameToAnalyze.Contains('_'))
             {
                 var parts = nameToAnalyze.Split('_');
                 if (parts.Length >= 2 && !string.IsNullOrEmpty(parts[0]))
                 {
                     schemaNameCandidate = parts[0];
                 }
             }

             if (schemaNameCandidate != null)
             {
                 string existingPath = Path.Combine(folder, schemaNameCandidate + ".json");
                 if (File.Exists(existingPath))
                 {
                     if (EditorUtility.DisplayDialog("Schema Found", 
                         $"A schema JSON named '{schemaNameCandidate}' already exists.\nDo you want to assign it to this set?", 
                         "Yes, Assign", "No, Create New"))
                     {
                         string json = File.ReadAllText(existingPath);
                         var temp = new UnitSchema();
                         temp.FromJson(json);
                         set.schemaId = temp.id;
                         set.schemaDefinitions = null; // Force reload
                         return;
                     }
                 }
             }

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
             string path = Path.Combine(folder, baseName + ".json");
             path = AssetDatabase.GenerateUniqueAssetPath(path);
             
             UnitSchema newSchema = new UnitSchema();
             newSchema.id = Path.GetFileNameWithoutExtension(path);
             newSchema.definitions = new List<UnitStatDefinition>();
             foreach(var id in orderedIds)
             {
                 newSchema.definitions.Add(new UnitStatDefinition { id = id, name = id });
             }
             
             File.WriteAllText(path, newSchema.ToJson());
             AssetDatabase.Refresh();
             
             set.schemaId = newSchema.id;
             set.schemaDefinitions = null; // Force reload
             
             Debug.Log("Created Schema JSON: " + path);
        }
    }
}