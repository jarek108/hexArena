using UnityEditor;
using UnityEngine;
using System.IO;

namespace HexGame.Units.Editor
{
    public class UnitDatabaseWindow : EditorWindow
    {
        private enum Mode { Sets, Schemas }
        private Mode currentMode = Mode.Sets;

        private UnitSet selectedSet;
        private UnitSchema selectedSchema;
        private Vector2 scrollPos;

        private const string PREF_SET_PATH = "HexGame_LastUnitSetPath";
        private const string PREF_SCHEMA_PATH = "HexGame_LastUnitSchemaPath";
        private const string PREF_MODE = "HexGame_LastWindowMode";

        [MenuItem("HexGame/Unit Database")]
        public static void OpenWindow()
        {
            GetWindow<UnitDatabaseWindow>("Unit Database").Show();
        }

        private void OnEnable()
        {
            // Load Last State
            string setPath = EditorPrefs.GetString(PREF_SET_PATH, "");
            if (!string.IsNullOrEmpty(setPath)) selectedSet = AssetDatabase.LoadAssetAtPath<UnitSet>(setPath);

            string schemaPath = EditorPrefs.GetString(PREF_SCHEMA_PATH, "");
            if (!string.IsNullOrEmpty(schemaPath)) selectedSchema = AssetDatabase.LoadAssetAtPath<UnitSchema>(schemaPath);

            currentMode = (Mode)EditorPrefs.GetInt(PREF_MODE, 0);
        }

        private void OnGUI()
        {
            DrawToolbar();

            GUILayout.Space(10);

            if (currentMode == Mode.Sets)
            {
                DrawSetMode();
            }
            else
            {
                DrawSchemaMode();
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            string[] modes = { "Unit Sets", "Schemas" };
            
            EditorGUI.BeginChangeCheck();
            currentMode = (Mode)GUILayout.Toolbar((int)currentMode, modes, EditorStyles.toolbarButton);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetInt(PREF_MODE, (int)currentMode);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSetMode()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Target Unit Set:", GUILayout.Width(100));
            
            EditorGUI.BeginChangeCheck();
            selectedSet = (UnitSet)EditorGUILayout.ObjectField(selectedSet, typeof(UnitSet), false);
            if (EditorGUI.EndChangeCheck())
            {
                if (selectedSet != null) EditorPrefs.SetString(PREF_SET_PATH, AssetDatabase.GetAssetPath(selectedSet));
                else EditorPrefs.SetString(PREF_SET_PATH, "");
            }
            
            if (GUILayout.Button("New Set", GUILayout.Width(80)))
            {
                CreateNewAsset<UnitSet>("Assets/Data/Sets", "NewUnitSet.asset");
            }
            EditorGUILayout.EndHorizontal();

            if (selectedSet != null)
            {
                if (selectedSet.schema == null)
                {
                    EditorGUILayout.HelpBox("Selected Set has no Schema assigned. Please fix in Inspector.", MessageType.Error);
                }
                else
                {
                    UnitEditorUI.DrawUnitSetEditor(selectedSet, ref scrollPos);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Select a Unit Set to edit.", MessageType.Info);
            }
        }

        private void DrawSchemaMode()
        {
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
                CreateNewAsset<UnitSchema>("Assets/Data/Schemas", "NewUnitSchema.asset");
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

        private void CreateNewAsset<T>(string folder, string defaultName) where T : ScriptableObject
        {
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            
            string path = Path.Combine(folder, defaultName);
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            
            T asset = CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            
            // Select the new one
            if (asset is UnitSet set) selectedSet = set;
            if (asset is UnitSchema schema) selectedSchema = schema;
            
            EditorGUIUtility.PingObject(asset);
        }
    }
}