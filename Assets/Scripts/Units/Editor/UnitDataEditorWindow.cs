using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace HexGame.Units.Editor
{
    public class UnitDataEditorWindow : EditorWindow
    {
        private enum Mode { Schemas, Sets }
        private Mode currentMode = Mode.Sets;

        private string schemasPath = "Assets/Data/Schemas";
        private string setsPath = "Assets/Data/Sets";

        private Vector2 listScroll;
        private Vector2 editorScroll;

        private string selectedFilePath;
        private object editingObject; // UnitSchema or UnitSet (transient)

        private const string PREF_LAST_FILE = "HexArena_LastUnitDataFile";
        private const string PREF_LAST_MODE = "HexArena_LastUnitDataMode";

        [MenuItem("HexArena/Unit Data Editor")]
        public static void Open()
        {
            GetWindow<UnitDataEditorWindow>("Unit Data Editor");
        }

        private void OnEnable()
        {
            currentMode = (Mode)EditorPrefs.GetInt(PREF_LAST_MODE, (int)Mode.Sets);
            string lastFile = EditorPrefs.GetString(PREF_LAST_FILE, "");

            // Verify the last file belongs to the current mode
            bool lastFileValid = !string.IsNullOrEmpty(lastFile) && File.Exists(lastFile);
            if (lastFileValid)
            {
                string expectedPath = currentMode == Mode.Schemas ? schemasPath : setsPath;
                // Get absolute paths for comparison
                string fullLast = Path.GetFullPath(lastFile);
                string fullExpected = Path.GetFullPath(expectedPath);
                if (!fullLast.StartsWith(fullExpected)) lastFileValid = false;
            }

            if (lastFileValid)
            {
                LoadFile(lastFile);
            }
            else
            {
                LoadFirstAvailable();
            }
        }

        private void LoadFirstAvailable()
        {
            ClearSelection();
            string path = currentMode == Mode.Schemas ? schemasPath : setsPath;
            if (Directory.Exists(path))
            {
                string[] files = Directory.GetFiles(path, "*.json");
                if (files.Length > 0)
                {
                    LoadFile(files[0]);
                }
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            
            // --- Left Panel: File List ---
            EditorGUILayout.BeginVertical(GUILayout.Width(200));
            DrawModeToggle();
            DrawFileList();
            EditorGUILayout.EndVertical();

            // --- Right Panel: Editor ---
            EditorGUILayout.BeginVertical();
            DrawEditor();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawModeToggle()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUI.BeginChangeCheck();
            bool schemas = GUILayout.Toggle(currentMode == Mode.Schemas, "Schemas", EditorStyles.toolbarButton);
            if (schemas && currentMode != Mode.Schemas)
            {
                currentMode = Mode.Schemas;
                LoadFirstAvailable();
            }

            bool sets = GUILayout.Toggle(currentMode == Mode.Sets, "Sets", EditorStyles.toolbarButton);
            if (sets && currentMode != Mode.Sets)
            {
                currentMode = Mode.Sets;
                LoadFirstAvailable();
            }
            if (EditorGUI.EndChangeCheck())
            {
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void ClearSelection()
        {
            selectedFilePath = null;
            editingObject = null;
        }

        private void DrawFileList()
        {
            string path = currentMode == Mode.Schemas ? schemasPath : setsPath;
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField(currentMode == Mode.Schemas ? "Schemas" : "Sets", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Add New", EditorStyles.toolbarButton))
            {
                CreateNewFile(path);
            }
            EditorGUILayout.EndHorizontal();

            listScroll = EditorGUILayout.BeginScrollView(listScroll, "box");

            string[] files = Directory.GetFiles(path, "*.json");
            foreach (string file in files)
            {
                EditorGUILayout.BeginHorizontal();
                string fileName = Path.GetFileName(file);
                GUIStyle style = new GUIStyle(EditorStyles.label);
                if (file == selectedFilePath) style.fontStyle = FontStyle.Bold;

                if (GUILayout.Button(fileName, style, GUILayout.ExpandWidth(true)))
                {
                    LoadFile(file);
                }

                if (GUILayout.Button("R", GUILayout.Width(20)))
                {
                    RenameFileDialog(file);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void RenameFileDialog(string oldPath)
        {
            string oldName = Path.GetFileNameWithoutExtension(oldPath);
            string newPath = EditorUtility.SaveFilePanel("Rename JSON", Path.GetDirectoryName(oldPath), oldName, "json");
            if (!string.IsNullOrEmpty(newPath))
            {
                if (oldPath.ToLower() == newPath.ToLower()) return;
                
                File.Move(oldPath, newPath);
                if (File.Exists(oldPath + ".meta")) File.Move(oldPath + ".meta", newPath + ".meta");
                
                AssetDatabase.Refresh();
                if (selectedFilePath == oldPath) selectedFilePath = newPath;
                LoadFile(newPath);
            }
        }

        private void LoadFile(string path)
        {
            selectedFilePath = path;
            string json = File.ReadAllText(path);

            if (currentMode == Mode.Schemas)
            {
                UnitSchema schema = CreateInstance<UnitSchema>();
                schema.FromJson(json);
                editingObject = schema;
            }
            else
            {
                UnitSet set = CreateInstance<UnitSet>();
                set.FromJson(json);
                editingObject = set;
            }

            EditorPrefs.SetString(PREF_LAST_FILE, path);
            EditorPrefs.SetInt(PREF_LAST_MODE, (int)currentMode);
        }

        private void CreateNewFile(string dir)
        {
            string name = "New" + (currentMode == Mode.Schemas ? "Schema" : "Set");
            string path = Path.Combine(dir, name + ".json");
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            string content = "{}";
            if (currentMode == Mode.Schemas) content = new UnitSchema().ToJson();
            else content = new UnitSet().ToJson();

            File.WriteAllText(path, content);
            AssetDatabase.Refresh();
            LoadFile(path);
        }

        private void DrawEditor()
        {
            if (editingObject == null)
            {
                EditorGUILayout.LabelField("Select a file from the list to edit.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField(Path.GetFileName(selectedFilePath));
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Auto-saving enabled", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            editorScroll = EditorGUILayout.BeginScrollView(editorScroll);

            EditorGUI.BeginChangeCheck();

            if (currentMode == Mode.Schemas)
            {
                UnitSchema schema = (UnitSchema)editingObject;
                schema.id = EditorGUILayout.TextField("Schema ID", schema.id);

                UnitEditorUI.DrawSchemaEditor(schema, ref editorScroll);
            }
            else
            {
                UnitSet set = (UnitSet)editingObject;
                
                set.setName = EditorGUILayout.TextField("Set Name", set.setName);
                string newSchemaId = EditorGUILayout.TextField("Schema ID Ref", set.schemaId);
                if (newSchemaId != set.schemaId)
                {
                    set.schemaId = newSchemaId;
                    set.schema = null; // Force reload by ID
                }

                UnitEditorUI.DrawUnitSetEditor(set, ref editorScroll);
            }

            if (EditorGUI.EndChangeCheck())
            {
                SaveCurrent();
            }

            EditorGUILayout.EndScrollView();
        }

        private void SaveCurrent()
        {
            if (editingObject == null || string.IsNullOrEmpty(selectedFilePath)) return;

            string json = "";
            if (currentMode == Mode.Schemas) json = ((UnitSchema)editingObject).ToJson();
            else json = ((UnitSet)editingObject).ToJson();

            File.WriteAllText(selectedFilePath, json);
            AssetDatabase.Refresh();
            Debug.Log("Saved: " + selectedFilePath);
        }
    }
}
