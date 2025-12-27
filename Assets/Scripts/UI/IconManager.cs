using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using HexGame.Tools;
using System.Linq;

namespace HexGame.UI
{
    [Serializable]
    public class IconData
    {
        public string iconName;
        public Sprite iconSprite;
        public string hotkey;
        public UnityEvent onClick;
    }

    [ExecuteAlways]
    public class IconManager : MonoBehaviour
    {
        [SerializeField]
        public List<IconData> icons = new List<IconData>();

        [SerializeField]
        public GameObject iconPrefab;

        [SerializeField]
        public string iconFolder = "Assets/Art/ToolIcons";
        
        [SerializeField] [Range(16, 256)]
        public float iconSize = 64f;
        
        [SerializeField] [Range(0, 20)]
        public float backgroundBorderSize = 2f;
        
        [SerializeField]
        public Color backgroundColor = Color.white;

        [SerializeField] [Range(0, 100)]
        public float spacing = 10f;

        [SerializeField] [Range(0, 100)]
        public float padding = 10f;

        [SerializeField]
        public Color hotkeyColor = Color.black;

        [SerializeField] [Range(8, 72)]
        public int hotkeySize = 14;

        [Header("Selection Visuals")]
        [SerializeField] public Color activeIconColor = Color.yellow;
        [SerializeField] public Color inactiveIconColor = Color.white;

        private ToolManager toolManager;

        private void Start()
        {
            toolManager = FindFirstObjectByType<ToolManager>();
            if (Application.isPlaying)
            {
                RefreshUI();
            }
        }

        private void Update()
        {
            if (!Application.isPlaying) return;
            if (Keyboard.current == null) return;

            foreach (var data in icons)
            {
                if (!string.IsNullOrEmpty(data.hotkey) && Enum.TryParse(data.hotkey, true, out Key key))
                {
                    if (Keyboard.current[key].wasPressedThisFrame)
                    {
                        data.onClick.Invoke();
                    }
                }
            }

            UpdateSelectionHighlights();
        }

        private void UpdateSelectionHighlights()
        {
            if (toolManager == null) return;

            string activeToolTypeName = toolManager.ActiveTool != null ? toolManager.ActiveTool.GetType().Name : "None";

            var allTools = toolManager.GetComponents<ITool>();

            foreach (Transform child in transform)
            {
                Image bg = child.GetComponent<Image>();
                if (bg == null) continue;

                // 1. Find the tool associated with this icon name
                string toolName = child.name.Replace("Icon_", "");
                var tool = allTools.FirstOrDefault(t => t.GetType().Name == toolName);

                if (tool == null) continue;

                bool shouldHighlight = false;

                if (tool is ToggleTool toggle)
                {
                    // Toggles are highlighted if they are ON
                    shouldHighlight = toggle.isActive;
                }
                else
                {
                    // Active tools are highlighted if they are CURRENTLY SELECTED
                    shouldHighlight = (tool.GetType().Name == activeToolTypeName);
                }

                bg.color = shouldHighlight ? activeIconColor : backgroundColor;
            }
        }

        #if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                UnityEditor.EditorApplication.delayCall += SafeRefresh;
            }
        }

        private void SafeRefresh()
        {
            if (this == null) return;
            
            var hlg = GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
            {
                hlg.spacing = spacing;
                hlg.padding = new RectOffset((int)padding, (int)padding, (int)padding, (int)padding);
            }

            ClearUIImmediate();
            RefreshUI();
        }

        private void Reset()
        {
            if (iconPrefab == null)
            {
                iconPrefab = Resources.Load<GameObject>("Prefabs/ToolbarIcon");
            }
        }
        #endif

        public void RefreshUI()
        {
            if (iconPrefab == null) return;

            if (Application.isPlaying)
            {
                for (int i = transform.childCount - 1; i >= 0; i--)
                {
                    Destroy(transform.GetChild(i).gameObject);
                }
            }

            foreach (var data in icons)
            {
                GameObject go = Instantiate(iconPrefab, transform);
                go.name = $"Icon_{data.iconName}";
                
                RectTransform rootRT = go.GetComponent<RectTransform>();
                if (rootRT != null)
                {
                    rootRT.sizeDelta = new Vector2(iconSize, iconSize);
                }

                LayoutElement le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
                le.preferredWidth = iconSize;
                le.preferredHeight = iconSize;

                Image bgImg = go.GetComponent<Image>();
                if (bgImg != null)
                {
                    bgImg.color = backgroundColor;
                }

                // Setup Button Logic
                Button btn = go.GetComponent<Button>();
                if (btn == null) btn = go.AddComponent<Button>();
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => data.onClick.Invoke());

                // Setup Icon Image
                Transform iconChild = go.transform.Find("IconImage");
                if (iconChild != null)
                {
                    RectTransform iconRT = iconChild.GetComponent<RectTransform>();
                    if (iconRT != null)
                    {
                        iconRT.anchorMin = Vector2.zero;
                        iconRT.anchorMax = Vector2.one;
                        iconRT.offsetMin = new Vector2(backgroundBorderSize, backgroundBorderSize);
                        iconRT.offsetMax = new Vector2(-backgroundBorderSize, -backgroundBorderSize);
                    }

                    Image iconImg = iconChild.GetComponent<Image>();
                    if (iconImg != null)
                    {
                        iconImg.sprite = data.iconSprite;
                        iconImg.color = Color.white;
                    }
                }

                // Setup Shortcut Text
                Transform textChild = go.transform.Find("ShortcutText");
                if (textChild != null)
                {
                    Text txt = textChild.GetComponent<Text>();
                    if (txt != null)
                    {
                        txt.text = data.hotkey?.ToUpper();
                        txt.color = hotkeyColor;
                        txt.fontSize = hotkeySize;
                        txt.fontStyle = FontStyle.Bold;
                    }
                }
            }
        }
        
        public void ClearUIImmediate()
        {
             for (int i = transform.childCount - 1; i >= 0; i--)
             {
                 if (transform.GetChild(i) != null)
                    DestroyImmediate(transform.GetChild(i).gameObject);
             }
        }
    }
}
