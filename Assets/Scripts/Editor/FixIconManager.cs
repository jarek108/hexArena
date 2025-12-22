using UnityEditor;
using UnityEngine;
using HexGame.UI;

namespace HexGame.Editor
{
    public class FixIconManager : EditorWindow
    {
        [MenuItem("HexGame/Fix Icon Manager")]
        public static void Fix()
        {
            IconManager manager = Object.FindFirstObjectByType<IconManager>();
            if (manager == null)
            {
                Debug.LogError("IconManager not found in scene!");
                return;
            }

            GameObject prefab = Resources.Load<GameObject>("Prefabs/ToolbarIcon");
            if (prefab == null)
            {
                Debug.LogError("ToolbarIcon prefab not found in Resources/Prefabs!");
                return;
            }

            Undo.RecordObject(manager, "Fix Icon Manager");
            manager.iconPrefab = prefab;
            
            manager.ClearUIImmediate();
            manager.RefreshUI();
            
            EditorUtility.SetDirty(manager);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            
            Debug.Log("IconManager Fixed and Refreshed.");
        }
    }
}
