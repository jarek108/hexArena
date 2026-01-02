using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace HexGame.Units
{
    [Serializable]
    public class UnitStatDefinition
    {
        public string id;
        public string name;
    }

    [Serializable]
    public struct UnitStatValue
    {
        public string id;
        public int value;
    }

    [Serializable]
    public class UnitType
    {
        public string id; // Persistent 8-char ID
        public string Name = "New Unit";
        public List<UnitStatValue> Stats = new List<UnitStatValue>();

        public static string GenerateId()
        {
            return Guid.NewGuid().ToString("n").Substring(0, 8);
        }
    }

    [Serializable]
    public class UnitSchema
    {
        public string id = "NewSchema";
        public List<UnitStatDefinition> definitions = new List<UnitStatDefinition>();

        public string ToJson()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"    \"id\": \"{id}\",");
            sb.AppendLine("    \"definitions\": [");
            for (int i = 0; i < definitions.Count; i++)
            {
                var def = definitions[i];
                sb.AppendLine("        {");
                sb.AppendLine($"            \"id\": \"{def.id}\",");
                sb.AppendLine($"            \"name\": \"{def.name}\"");
                sb.Append("        }");
                if (i < definitions.Count - 1) sb.AppendLine(",");
                else sb.AppendLine();
            }
            sb.AppendLine("    ]");
            sb.AppendLine("}");
            return sb.ToString();
        }

        public void FromJson(string json)
        {
            // Simple manual parsing to avoid dependencies and handle flexible formatting
            id = ParseStringField(json, "id") ?? id;
            
            definitions = new List<UnitStatDefinition>();
            int defsStart = json.IndexOf("\"definitions\":");
            if (defsStart == -1) return;

            int arrayStart = json.IndexOf("[", defsStart);
            if (arrayStart == -1) return;

            int pos = arrayStart + 1;
            while (true)
            {
                int objStart = json.IndexOf("{", pos);
                if (objStart == -1) break;
                int objEnd = FindMatchingBrace(json, objStart);
                if (objEnd == -1) break;

                string objJson = json.Substring(objStart, objEnd - objStart + 1);
                UnitStatDefinition def = new UnitStatDefinition();
                def.id = ParseStringField(objJson, "id");
                def.name = ParseStringField(objJson, "name");

                definitions.Add(def);
                pos = objEnd + 1;

                int arrayEnd = json.IndexOf("]", pos);
                int nextObj = json.IndexOf("{", pos);
                if (nextObj == -1 || (arrayEnd != -1 && arrayEnd < nextObj)) break;
            }
        }

        private static string ParseStringField(string json, string field)
        {
            int idx = json.IndexOf($"\"{field}\":");
            if (idx == -1) return null;
            int q1 = json.IndexOf("\"", idx + field.Length + 3);
            if (q1 == -1) return null;
            int q2 = json.IndexOf("\"", q1 + 1);
            if (q2 == -1) return null;
            return json.Substring(q1 + 1, q2 - q1 - 1);
        }

        private static int FindMatchingBrace(string s, int start)
        {
            int depth = 0;
            for (int i = start; i < s.Length; i++)
            {
                if (s[i] == '{') depth++;
                else if (s[i] == '}')
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }
            return -1;
        }
    }

    [Serializable]
    public class UnitSet
    {
        public string setName = "NewSet";
        public string schemaId;

        private List<UnitStatDefinition> _schemaDefinitions;
        public List<UnitStatDefinition> schemaDefinitions
        {
            get
            {
                if (_schemaDefinitions == null && !string.IsNullOrEmpty(schemaId))
                {
                    _schemaDefinitions = ResolveSchemaDefinitionsById(schemaId);
                }
                return _schemaDefinitions;
            }
            set => _schemaDefinitions = value;
        }

        public List<UnitType> units = new List<UnitType>();

        private List<UnitStatDefinition> ResolveSchemaDefinitionsById(string id)
        {
            string folder = "Assets/Data/Schemas";
            if (!Directory.Exists(folder)) return null;

            string[] files = Directory.GetFiles(folder, "*.json");
            foreach (var file in files)
            {
                string json = File.ReadAllText(file);
                UnitSchema schema = new UnitSchema();
                schema.FromJson(json);
                if (schema.id == id) return schema.definitions;
            }
            return null;
        }

        public string ToJson()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"setName\": \"{setName}\",");
            sb.AppendLine($"  \"schemaId\": \"{schemaId}\",");
            sb.AppendLine("  \"units\": [");
            for (int i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                sb.AppendLine("    {");
                sb.AppendLine($"      \"id\": \"{unit.id}\",");
                sb.AppendLine($"      \"Name\": \"{unit.Name}\",");
                sb.AppendLine("      \"Stats\": {");
                for (int j = 0; j < unit.Stats.Count; j++)
                {
                    var stat = unit.Stats[j];
                    sb.Append($"        \"{stat.id}\": {stat.value}");
                    if (j < unit.Stats.Count - 1) sb.AppendLine(",");
                    else sb.AppendLine();
                }
                sb.AppendLine("      }");
                sb.Append("    }");
                if (i < units.Count - 1) sb.AppendLine(",");
                else sb.AppendLine();
            }
            sb.AppendLine("  ]");
            sb.AppendLine("}");
            return sb.ToString();
        }

        public void FromJson(string json)
        {
            setName = ParseStringField(json, "setName") ?? setName;
            schemaId = ParseStringField(json, "schemaId") ?? schemaId;

            int unitsListStart = json.IndexOf("\"units\":");
            if (unitsListStart == -1) return;

            int arrayStart = json.IndexOf("[", unitsListStart);
            if (arrayStart == -1) return;

            units.Clear();
            int pos = arrayStart + 1;
            while (true)
            {
                int objStart = json.IndexOf("{", pos);
                if (objStart == -1) break;
                int objEnd = FindMatchingBrace(json, objStart);
                if (objEnd == -1) break;

                string objJson = json.Substring(objStart, objEnd - objStart + 1);
                UnitType unit = ParseUnitJson(objJson);
                if (unit != null) units.Add(unit);

                pos = objEnd + 1;
                int arrayEnd = json.IndexOf("]", pos);
                int nextObj = json.IndexOf("{", pos);
                if (nextObj == -1 || (arrayEnd != -1 && arrayEnd < nextObj)) break;
            }
        }

        private UnitType ParseUnitJson(string json)
        {
            UnitType unit = new UnitType();
            unit.id = ParseStringField(json, "id");
            
            if (string.IsNullOrEmpty(unit.id))
            {
                unit.id = UnitType.GenerateId();
                Debug.LogWarning($"[UnitData] Unit '{ParseStringField(json, "Name") ?? "Unknown"}' is missing an ID! Generated temporary ID: {unit.id}. Save the UnitSet to persist this ID.");
            }

            unit.Name = ParseStringField(json, "Name") ?? unit.Name;

            int statsStart = json.IndexOf("\"Stats\":");
            if (statsStart != -1)
            {
                int braceStart = json.IndexOf("{", statsStart);
                if (braceStart != -1)
                {
                    int braceEnd = FindMatchingBrace(json, braceStart);
                    if (braceEnd != -1)
                    {
                        string statsInner = json.Substring(braceStart + 1, braceEnd - braceStart - 1);
                        string[] pairs = statsInner.Split(',');
                        foreach (var pair in pairs)
                        {
                            string[] kv = pair.Split(':');
                            if (kv.Length == 2)
                            {
                                string id = kv[0].Trim().Trim('"');
                                string valStr = kv[1].Trim();
                                if (int.TryParse(valStr, out int val))
                                {
                                    unit.Stats.Add(new UnitStatValue { id = id, value = val });
                                }
                            }
                        }
                    }
                }
            }
            return unit;
        }

        private static string ParseStringField(string json, string field)
        {
            int idx = json.IndexOf($"\"{field}\":");
            if (idx == -1) return null;
            int q1 = json.IndexOf("\"", idx + field.Length + 3);
            if (q1 == -1) return null;
            int q2 = json.IndexOf("\"", q1 + 1);
            if (q2 == -1) return null;
            return json.Substring(q1 + 1, q2 - q1 - 1);
        }

        private static int FindMatchingBrace(string s, int start)
        {
            int depth = 0;
            for (int i = start; i < s.Length; i++)
            {
                if (s[i] == '{') depth++;
                else if (s[i] == '}')
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }
            return -1;
        }
    }
}