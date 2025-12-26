using UnityEditor;
using UnityEngine;
using System.IO;

namespace HexGame.Units.Editor
{
    public class UnitSchemaWindow : EditorWindow
    {
        private UnitSchema selectedSchema;
        private Vector2 scrollPos;
        private const string PREF_SCHEMA_PATH = "HexGame_LastUnitSchemaPath";

        [MenuItem("HexGame/Unit Schemas")]
        public static void OpenWindow()
        {
            GetWindow<UnitSchemaWindow>("Unit Schemas").Show();
        }

        private void OnEnable()
        {
            string schemaPath = EditorPrefs.GetString(PREF_SCHEMA_PATH, "");
            if (!string.IsNullOrEmpty(schemaPath)) selectedSchema = AssetDatabase.LoadAssetAtPath<UnitSchema>(schemaPath);
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Target Schema:", GUILayout.Width(100));
            
            EditorGUI.BeginChangeCheck();
            selectedSchema = (UnitSchema)EditorGUILayout.ObjectField(selectedSchema, typeof(UnitSchema), false);
            if (EditorGUI.EndChangeCheck())
            {
                if (selectedSchema != null) EditorPrefs.SetString(PREF_SCHEMA_PATH, AssetDatabase.GetAssetPath(selectedSchema));
                else EditorPrefs.SetString(PREF_SCHEMA_PATH, "");
            }
            
            if (GUILayout.Button("New Schema", GUILayout.Width(80)))
            {
                CreateNewAsset("Assets/Data/Schemas", "NewUnitSchema.asset");
            }
            EditorGUILayout.EndHorizontal();

            if (selectedSchema != null)
            {
                UnitEditorUI.DrawSchemaEditor(selectedSchema, ref scrollPos);
            }
            else
            {
                EditorGUILayout.HelpBox("Select a Schema to edit.", MessageType.Info);
            }
        }

        private void CreateNewAsset(string folder, string defaultName)
        {
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            
            string path = Path.Combine(folder, defaultName);
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            
            UnitSchema asset = CreateInstance<UnitSchema>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            
            selectedSchema = asset;
            EditorPrefs.SetString(PREF_SCHEMA_PATH, AssetDatabase.GetAssetPath(selectedSchema));
            EditorGUIUtility.PingObject(asset);
        }
    }
}