using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace HexGame.Units.Editor
{
    [CustomEditor(typeof(UnitSet))]
    public class UnitSetEditor : UnityEditor.Editor
    {
        private Vector2 scrollPos;
        private UnitSchema pendingSchemaSelection;

        public override void OnInspectorGUI()
        {
            UnitSet set = (UnitSet)target;
            serializedObject.Update();

            // --- Header / Configuration ---
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            
            // Set Name with renaming logic
            EditorGUI.BeginChangeCheck();
            string newSetName = EditorGUILayout.TextField("Set Name", set.setName);
            if (EditorGUI.EndChangeCheck())
            {
                set.setName = newSetName;
                // Rename immediately? Or wait for Enter/Focus lost? 
                // Immediate renaming might be annoying while typing. 
                // But for now, we just update the field. 
                // We'll add a "Apply Rename" button or do it on validation if needed.
                // Actually, the user requirement is "when it is change... automatically convert".
                // Doing it on every keystroke is bad. Let's do it on delayed check or a button.
                // Better: Detect focus lost or specifically handle the event.
                // For simplicity/safety, let's update the internal value but trigger the file rename via a helper check 
                // comparing file name vs expected name.
            }

            // Schema Selection
            EditorGUI.BeginChangeCheck();
            UnitSchema currentSchema = set.schema;
            UnitSchema newSchema = (UnitSchema)EditorGUILayout.ObjectField("Schema", currentSchema, typeof(UnitSchema), false);
            if (EditorGUI.EndChangeCheck())
            {
                if (newSchema != currentSchema) pendingSchemaSelection = newSchema;
            }

            // Check if file name matches convention
            if (set.schema != null && !string.IsNullOrEmpty(set.setName))
            {
                string currentPath = AssetDatabase.GetAssetPath(set);
                string currentFileName = Path.GetFileNameWithoutExtension(currentPath);
                string expectedFileName = SanitizeFilename($"{set.schema.name}_{set.setName}");
                
                if (currentFileName != expectedFileName && pendingSchemaSelection == null)
                {
                    if (GUILayout.Button($"Rename File to '{expectedFileName}'"))
                    {
                        TryRenameAsset(set, expectedFileName);
                        GUIUtility.ExitGUI();
                    }
                }
            }

            // --- Validation & Migration UI ---
            if (pendingSchemaSelection != null && pendingSchemaSelection != set.schema)
            {
                string fromName = set.schema ? set.schema.name : "None";
                string toName = pendingSchemaSelection.name;
                string newFileName = SanitizeFilename($"{toName}_{set.setName}");
                
                string msg = $"Schema change detected!\nFrom: {fromName}\nTo: {toName}\n\nSaving this change will CREATE A NEW FILE named '{newFileName}'.";
                
                EditorGUILayout.HelpBox(msg, MessageType.Warning);
                
                if (GUILayout.Button($"Migrate & Save as '{newFileName}'"))
                {
                    MigrateAndSave(set, pendingSchemaSelection, newFileName);
                    pendingSchemaSelection = null; 
                    GUIUtility.ExitGUI();
                }
                
                if (GUILayout.Button("Cancel Change")) pendingSchemaSelection = null;
                
                serializedObject.ApplyModifiedProperties();
                return;
            }

            // --- Standard Validation ---
            // Delegated to UnitEditorUI
            if (set.schema == null)
            {
                UnitEditorUI.DrawUnitSetEditor(set, ref scrollPos);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            // --- Unit List Editor ---
            UnitEditorUI.DrawUnitSetEditor(set, ref scrollPos);

            serializedObject.ApplyModifiedProperties();
        }

        private string SanitizeFilename(string name)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                name = name.Replace(c, '_');
            }
            return name;
        }

        private void TryRenameAsset(UnitSet set, string newName)
        {
            string path = AssetDatabase.GetAssetPath(set);
            string folder = Path.GetDirectoryName(path);
            string newPath = Path.Combine(folder, newName + ".asset");

            if (File.Exists(newPath))
            {
                bool overwrite = EditorUtility.DisplayDialog("Overwrite File?", 
                    $"File '{newName}.asset' already exists. Overwrite?", "Yes", "Cancel");
                
                if (!overwrite) return; 
                
                // AssetDatabase.RenameAsset cannot overwrite. We must delete target first.
                AssetDatabase.DeleteAsset(newPath);
            }

            string result = AssetDatabase.RenameAsset(path, newName);
            if (string.IsNullOrEmpty(result))
            {
                Debug.Log($"Renamed set to {newName}");
            }
            else
            {
                Debug.LogError($"Rename failed: {result}");
            }
            
            AssetDatabase.SaveAssets();
        }

        private void MigrateAndSave(UnitSet originalSet, UnitSchema newSchema, string newFileName)
        {
            string originalPath = AssetDatabase.GetAssetPath(originalSet);
            string folder = Path.GetDirectoryName(originalPath);
            string newPath = Path.Combine(folder, newFileName + ".asset");
            
            if (File.Exists(newPath))
            {
                 bool overwrite = EditorUtility.DisplayDialog("Overwrite File?", 
                    $"File '{newFileName}.asset' already exists. Overwrite?", "Yes", "Cancel");
                
                if (!overwrite) return;
                // GenerateUniqueAssetPath would be the alternative, but requirements say "Overwrite" logic
                // If overwrite confirmed, AssetDatabase.CreateAsset will overwrite if it exists? 
                // No, usually it's safer to delete or use GenerateUnique if we wanted unique.
                // AssetDatabase.CreateAsset overwrites? Documentation says "creates a new asset".
                // Let's explicitly delete to be sure.
                AssetDatabase.DeleteAsset(newPath);
            }

            UnitSet newSet = Instantiate(originalSet);
            newSet.schema = newSchema;
            // Ensure the internal name matches the file name logic we want
            // newSet.setName is already set because we instantiated logic
            
            AssetDatabase.CreateAsset(newSet, newPath);
            AssetDatabase.SaveAssets();
            
            Selection.activeObject = newSet;
            EditorGUIUtility.PingObject(newSet);
            
            Debug.Log($"Migrated set to {newPath}");
        }
    }
}
