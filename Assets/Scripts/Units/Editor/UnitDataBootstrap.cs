using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using HexGame.Units;

namespace HexGame.Units.Editor
{
    public static class UnitDataBootstrap
    {
        [MenuItem("HexGame/Bootstrap/Create Battle Brothers Data")]
        public static void CreateBattleBrothersData()
        {
            // Ensure folders exist
            if (!Directory.Exists("Assets/Data/Schemas")) Directory.CreateDirectory("Assets/Data/Schemas");
            if (!Directory.Exists("Assets/Data/Sets")) Directory.CreateDirectory("Assets/Data/Sets");

            // --- 1. Create Schema ---
            string schemaPath = "Assets/Data/Schemas/BattleBrothers.asset";
            UnitSchema schema = AssetDatabase.LoadAssetAtPath<UnitSchema>(schemaPath);
            
            if (schema == null)
            {
                schema = ScriptableObject.CreateInstance<UnitSchema>();
                AssetDatabase.CreateAsset(schema, schemaPath);
            }

            // Define Stats (Index mapping for reference)
            // 0:HP, 1:FAT, 2:RES, 3:INI, 4:MAT, 5:RAT, 6:MDF, 7:RDF, 8:AP, 9:VIS, 10:MRNG, 11:RRNG
            schema.definitions = new List<UnitStatDefinition>
            {
                new UnitStatDefinition { id = "HP",  name = "Hitpoints" },
                new UnitStatDefinition { id = "FAT", name = "Fatigue (Max Stamina)" },
                new UnitStatDefinition { id = "RES", name = "Resolve (Bravery)" },
                new UnitStatDefinition { id = "INI", name = "Initiative (Turn Order)" },
                new UnitStatDefinition { id = "MAT", name = "Melee Skill" },
                new UnitStatDefinition { id = "RAT", name = "Ranged Skill" },
                new UnitStatDefinition { id = "MDF", name = "Melee Defense" },
                new UnitStatDefinition { id = "RDF", name = "Ranged Defense" },
                new UnitStatDefinition { id = "AP",  name = "Action Points" },
                new UnitStatDefinition { id = "VIS", name = "Vision Range" },
                new UnitStatDefinition { id = "MRNG", name = "Melee Range" },
                new UnitStatDefinition { id = "RRNG", name = "Ranged Range" }
            };
            
            EditorUtility.SetDirty(schema);

            // --- 2. Create Unit Set ---
            string setPath = "Assets/Data/Sets/BattleBrothers_Core.asset";
            UnitSet set = AssetDatabase.LoadAssetAtPath<UnitSet>(setPath);

            if (set == null)
            {
                set = ScriptableObject.CreateInstance<UnitSet>();
                AssetDatabase.CreateAsset(set, setPath);
            }

            set.schema = schema;
            
            // Clear existing for a clean generation
            set.units.Clear();

            // Helper to create unit
            void CreateUnit(string name, int hp, int fat, int res, int ini, int mat, int rat, int mdf, int rdf, int ap, int vis, int mrng, int rrng)
            {
                UnitType u = new UnitType { Name = name };
                u.Stats = new List<UnitStatValue>
                {
                    new UnitStatValue { id = "HP", value = hp },
                    new UnitStatValue { id = "FAT", value = fat },
                    new UnitStatValue { id = "RES", value = res },
                    new UnitStatValue { id = "INI", value = ini },
                    new UnitStatValue { id = "MAT", value = mat },
                    new UnitStatValue { id = "RAT", value = rat },
                    new UnitStatValue { id = "MDF", value = mdf },
                    new UnitStatValue { id = "RDF", value = rdf },
                    new UnitStatValue { id = "AP", value = ap },
                    new UnitStatValue { id = "VIS", value = vis },
                    new UnitStatValue { id = "MRNG", value = mrng },
                    new UnitStatValue { id = "RRNG", value = rrng }
                };
                set.units.Add(u);
            }

            // --- 3. Populate Units ---
            
            // Shielded Warrior (Tanky, Std Ranges)
            CreateUnit("Shielded Warrior", 70, 100, 50, 100, 65, 30, 20, 20, 9, 7, 1, 0);

            // 2H Warrior (Damage, Std Ranges)
            CreateUnit("2H Warrior", 80, 110, 55, 95, 75, 30, 5, 5, 9, 7, 1, 0);

            // Pikeman (Reach Weapon - Melee Range 2)
            CreateUnit("Pikeman", 60, 100, 45, 95, 60, 30, 5, 10, 9, 7, 2, 0);

            // Archer (Ranged - Range 6)
            CreateUnit("Archer", 55, 90, 40, 110, 40, 70, 5, 10, 9, 8, 1, 6);

            // Undead
            // Skeleton (Fragile but persistent)
            CreateUnit("Skeleton", 40, 80, 100, 80, 50, 20, 5, 15, 9, 7, 1, 0);

            // Zombie (High HP, low stats)
            CreateUnit("Zombie", 100, 100, 100, 60, 45, 0, 0, 0, 6, 7, 1, 0);

            // Monsters
            // Direwolf (Fast)
            CreateUnit("Direwolf", 50, 120, 60, 130, 70, 0, 10, 10, 12, 7, 1, 0);

            // Nachzehrer
            CreateUnit("Nachzehrer", 120, 100, 40, 90, 60, 0, 5, 0, 9, 7, 1, 0);

            // Lindwurm
            CreateUnit("Lindwurm", 400, 200, 100, 80, 85, 0, 30, 20, 9, 7, 2, 0); // Reach 2 for big monster

            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
