using System;
using System.Collections.Generic;
using System.Text;

namespace HexGame.Units
{
    [Serializable]
    public class UnitSchemaData
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
            // Parse ID
            int idIdx = json.IndexOf("\"id\":");
            if (idIdx != -1)
            {
                int q1 = json.IndexOf("\"", idIdx + 4);
                int q2 = json.IndexOf("\"", q1 + 1);
                if (q1 != -1 && q2 != -1) id = json.Substring(q1 + 1, q2 - q1 - 1);
            }

            // Parse Definitions
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
                
                // Parse ID
                int dIdIdx = objJson.IndexOf("\"id\":");
                if (dIdIdx != -1)
                {
                    int q1 = objJson.IndexOf("\"", dIdIdx + 4);
                    int q2 = objJson.IndexOf("\"", q1 + 1);
                    if (q1 != -1 && q2 != -1) def.id = objJson.Substring(q1 + 1, q2 - q1 - 1);
                }
                // Parse Name
                int nameIdx = objJson.IndexOf("\"name\":");
                if (nameIdx != -1)
                {
                    int q1 = objJson.IndexOf("\"", nameIdx + 6);
                    int q2 = objJson.IndexOf("\"", q1 + 1);
                    if (q1 != -1 && q2 != -1) def.name = objJson.Substring(q1 + 1, q2 - q1 - 1);
                }

                definitions.Add(def);
                pos = objEnd + 1;

                int arrayEnd = json.IndexOf("]", pos);
                int nextObj = json.IndexOf("{", pos);
                if (nextObj == -1 || (arrayEnd != -1 && arrayEnd < nextObj)) break;
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
    }
}
