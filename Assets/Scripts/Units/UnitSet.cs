using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace HexGame.Units
{
    [CreateAssetMenu(fileName = "NewUnitSet", menuName = "HexGame/Unit Set")]
    public class UnitSet : ScriptableObject
    {
        public string setName = "NewSet";
        public string schemaId; // Referred to by ID

        private UnitSchema _schema;
        public UnitSchema schema
        {
            get
            {
                if (_schema == null && !string.IsNullOrEmpty(schemaId))
                {
                    _schema = ResolveSchemaById(schemaId);
                }
                return _schema;
            }
            set
            {
                _schema = value;
            }
        }

        private UnitSchema ResolveSchemaById(string id)
        {
            string folder = "Assets/Data/Schemas";
            if (!Directory.Exists(folder)) return null;

            string[] files = Directory.GetFiles(folder, "*.json");
            foreach (var file in files)
            {
                string json = File.ReadAllText(file);
                // Quick check for ID without full parse if possible, or just parse
                // Since these are small, full parse is safe
                var tempSchema = ScriptableObject.CreateInstance<UnitSchema>();
                tempSchema.FromJson(json);
                if (tempSchema.id == id) return tempSchema;
            }
            return null;
        }

        public List<UnitType> units = new List<UnitType>();

        public string ToJson()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"setName\": \"{setName}\",");
            sb.AppendLine($"  \"schemaId\": \"{schemaId}\",");
            sb.AppendLine("  \"units\": [");
            for (int i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                sb.AppendLine("    {");
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
            // 1. Get setName
            int setNameIndex = json.IndexOf("\"setName\":");
            if (setNameIndex != -1)
            {
                int quoteStart = json.IndexOf("\"", setNameIndex + 10);
                if (quoteStart != -1)
                {
                    int start = quoteStart + 1;
                    int end = json.IndexOf("\"", start);
                    if (end != -1) setName = json.Substring(start, end - start);
                }
            }

            // 2. Get schemaId
            int schemaIdIndex = json.IndexOf("\"schemaId\":");
            if (schemaIdIndex != -1)
            {
                int quoteStart = json.IndexOf("\"", schemaIdIndex + 11);
                if (quoteStart != -1)
                {
                    int start = quoteStart + 1;
                    int end = json.IndexOf("\"", start);
                    if (end != -1) schemaId = json.Substring(start, end - start);
                }
            }

            // 3. Units
            int unitsListStart = json.IndexOf("\"units\":");
            if (unitsListStart == -1) return;

            int arrayStart = json.IndexOf("[", unitsListStart);
            if (arrayStart == -1) return;

            units.Clear();
            int unitSearchPos = arrayStart + 1;
            while (true)
            {
                int unitStart = json.IndexOf("{", unitSearchPos);
                if (unitStart == -1) break;

                int unitEnd = FindMatchingBrace(json, unitStart);
                if (unitEnd == -1) break;

                string unitJson = json.Substring(unitStart, unitEnd - unitStart + 1);
                UnitType unit = ParseUnitJson(unitJson);
                if (unit != null) units.Add(unit);

                unitSearchPos = unitEnd + 1;
                
                int arrayEnd = json.IndexOf("]", unitSearchPos);
                int nextUnit = json.IndexOf("{", unitSearchPos);
                if (nextUnit == -1 || (arrayEnd != -1 && arrayEnd < nextUnit)) break;
            }
        }

        private int FindMatchingBrace(string s, int start)
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

        private UnitType ParseUnitJson(string json)
        {
            UnitType unit = new UnitType();
            
            // Name
            int nameIndex = json.IndexOf("\"Name\":");
            if (nameIndex != -1)
            {
                int quoteStart = json.IndexOf("\"", nameIndex + 7);
                if (quoteStart != -1)
                {
                    int start = quoteStart + 1;
                    int end = json.IndexOf("\"", start);
                    if (end != -1) unit.Name = json.Substring(start, end - start);
                }
            }

            // Stats
            int statsStart = json.IndexOf("\"Stats\":");
            if (statsStart != -1)
            {
                int braceStart = json.IndexOf("{", statsStart);
                if (braceStart != -1)
                {
                    int braceEnd = FindMatchingBrace(json, braceStart);
                    if (braceEnd != -1)
                    {
                        string statsJson = json.Substring(braceStart + 1, braceEnd - braceStart - 1);

                        // Split by comma but be aware of whitespace
                        string[] pairs = statsJson.Split(',');
                        foreach (var pair in pairs)
                        {
                            string[] kv = pair.Split(':');
                            if (kv.Length == 2)
                            {
                                string id = kv[0].Trim().Trim('\"');
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

        private class UnitSetJsonProxy
        {
            public string setName;
            public List<UnitTypeJsonProxy> units;
        }

        private class UnitTypeJsonProxy
        {
            public string Name;
            public Dictionary<string, int> Stats;
        }
    }
}
