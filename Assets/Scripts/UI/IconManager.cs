using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using HexGame.Tools;
using System.Linq;
using TMPro;

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
        public string iconFolder = "Assets/Resources/Art/ToolIcons";
        
        [SerializeField] [Range(16, 256)]
        public float iconSize = 64f;
        
        [SerializeField] [Range(0, 20)]
        public float iconPadding = 4f;
        
        [SerializeField]
        public Color backgroundColor = new Color(0, 0, 0, 0.6f);

        [SerializeField]
        public bool useGradient = true;
        [SerializeField]
        public Color gradientColorBottom = new Color(0, 0, 0, 0.9f);

        [SerializeField] [Range(0, 100)]
        public float spacing = 10f;

        [SerializeField] [Range(0, 100)]
        public float padding = 10f;

        [Header("Hotkey Box")]
        public Color hotkeyBoxColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        public Color hotkeyTextColor = Color.white;
        public int hotkeyFontSize = 12;
        public Vector2 hotkeyBoxSize = new Vector2(20, 20);

        [Header("Selection Visuals")]
        public Color activeColor = new Color(1f, 0.8f, 0.2f, 1f); // Gold
        public float selectedScale = 1.15f;
        public float animationSpeed = 10f;

        private ToolManager toolManager;
        private Dictionary<string, RectTransform> iconRoots = new Dictionary<string, RectTransform>();

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
            
            HandleInput();
            UpdateSelectionHighlights();
        }

        private void HandleInput()
        {
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
        }

        private void UpdateSelectionHighlights()
        {
            if (toolManager == null) return;
            if (iconRoots == null) iconRoots = new Dictionary<string, RectTransform>();

            string activeToolTypeName = toolManager.ActiveTool != null ? toolManager.ActiveTool.GetType().Name : "None";
            var allTools = toolManager.GetComponents<ITool>();

            foreach (var kvp in iconRoots)
            {
                string toolName = kvp.Key.Replace("Icon_", "");
                RectTransform rt = kvp.Value;
                var tool = allTools.FirstOrDefault(t => t.GetType().Name == toolName);

                if (tool == null) continue;

                bool shouldHighlight = false;
                if (tool is ToggleTool toggle) shouldHighlight = toggle.isActive;
                else shouldHighlight = (tool.GetType().Name == activeToolTypeName);

                // Lerp Scale
                float targetScale = shouldHighlight ? selectedScale : 1f;
                rt.localScale = Vector3.Lerp(rt.localScale, Vector3.one * targetScale, Time.deltaTime * animationSpeed);

                // Update Background Color / Glow
                Image bg = rt.GetComponent<Image>();
                if (bg != null)
                {
                    Color targetColor = shouldHighlight ? activeColor : backgroundColor;
                    bg.color = Color.Lerp(bg.color, targetColor, Time.deltaTime * animationSpeed);
                    
                    // Update gradient if present
                    var grad = rt.GetComponent<UIGradient>();
                    if (grad != null && grad.enabled)
                    {
                        grad.colorTop = bg.color;
                        grad.colorBottom = shouldHighlight ? activeColor * 0.8f : gradientColorBottom;
                    }
                }
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

            if (iconRoots == null) iconRoots = new Dictionary<string, RectTransform>();
            iconRoots.Clear();
            if (Application.isPlaying)
            {
                for (int i = transform.childCount - 1; i >= 0; i--)
                {
                    Destroy(transform.GetChild(i).gameObject);
                }
            }

            foreach (var data in icons)
            {
                if (data == null) continue;
                if (iconPrefab == null || iconPrefab.Equals(null)) break;
                
                GameObject go = Instantiate(iconPrefab, transform);
                if (go == null || go.Equals(null)) continue;
                
                string safeName = string.IsNullOrEmpty(data.iconName) ? "Tool" : data.iconName;
                go.name = $"Icon_{safeName}";
                
                RectTransform rootRT = go.GetComponent<RectTransform>();
                if (rootRT != null)
                {
                    rootRT.sizeDelta = new Vector2(iconSize, iconSize);
                    if (Application.isPlaying) iconRoots[go.name] = rootRT;
                }

                LayoutElement le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
                le.preferredWidth = iconSize;
                le.preferredHeight = iconSize;

                // Background & Effects
                Image bgImg = go.GetComponent<Image>();
                if (bgImg != null)
                {
                    bgImg.color = backgroundColor;
                    // Add Shadow for separation
                    if (go.GetComponent<Shadow>() == null)
                    {
                        var shadow = go.AddComponent<Shadow>();
                        shadow.effectColor = new Color(0, 0, 0, 0.5f);
                        shadow.effectDistance = new Vector2(2, -2);
                    }

                    // Add Gradient
                    if (useGradient)
                    {
                        var grad = go.GetComponent<UIGradient>() ?? go.AddComponent<UIGradient>();
                        grad.colorTop = backgroundColor;
                        grad.colorBottom = gradientColorBottom;
                    }
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
                        iconRT.offsetMin = new Vector2(iconPadding, iconPadding);
                        iconRT.offsetMax = new Vector2(-iconPadding, -iconPadding);
                    }

                    Image iconImg = iconChild.GetComponent<Image>();
                    if (iconImg != null)
                    {
                        iconImg.sprite = data.iconSprite;
                        iconImg.color = Color.white;
                        iconImg.raycastTarget = false;
                    }
                }

                // Setup Shortcut Text & Box
                Transform hotkeyRoot = go.transform.Find("ShortcutText");
                if (hotkeyRoot != null && hotkeyRoot.gameObject != null)
                {
                    GameObject boxGo = hotkeyRoot.gameObject;
                    
                    // 1. Clean up ANY existing graphics from the root (could be Text or TMP)
                    var legacyText = boxGo.GetComponent<Text>();
                    if (legacyText != null) { if (Application.isPlaying) Destroy(legacyText); else DestroyImmediate(legacyText); }
                    
                    var tmpOnRoot = boxGo.GetComponent<TextMeshProUGUI>();
                    if (tmpOnRoot != null) { if (Application.isPlaying) Destroy(tmpOnRoot); else DestroyImmediate(tmpOnRoot); }

                    // 2. Setup the Box (Image)
                    Image box = boxGo.GetComponent<Image>();
                    if (box == null) box = boxGo.AddComponent<Image>();
                    if (box != null) 
                    {
                        box.color = hotkeyBoxColor;
                        box.raycastTarget = false;
                    }

                    RectTransform boxRT = boxGo.GetComponent<RectTransform>();
                    if (boxRT != null)
                    {
                        boxRT.sizeDelta = hotkeyBoxSize;
                        boxRT.anchorMin = new Vector2(1, 0);
                        boxRT.anchorMax = new Vector2(1, 0);
                        boxRT.pivot = new Vector2(1, 0);
                        boxRT.anchoredPosition = new Vector2(-2, 2);
                    }

                    // 3. Setup the Label child (Text)
                    Transform labelTransform = hotkeyRoot.Find("Label");
                    GameObject labelGo;
                    if (labelTransform == null)
                    {
                        labelGo = new GameObject("Label");
                        labelGo.transform.SetParent(hotkeyRoot, false);
                    }
                    else
                    {
                        labelGo = labelTransform.gameObject;
                    }

                    RectTransform labelRT = labelGo.GetComponent<RectTransform>() ?? labelGo.AddComponent<RectTransform>();
                    if (labelRT != null)
                    {
                        labelRT.anchorMin = Vector2.zero;
                        labelRT.anchorMax = Vector2.one;
                        labelRT.sizeDelta = Vector2.zero;
                    }

                    TextMeshProUGUI txt = labelGo.GetComponent<TextMeshProUGUI>() ?? labelGo.AddComponent<TextMeshProUGUI>();
                    if (txt != null)
                    {
                        txt.text = data.hotkey != null ? data.hotkey.ToUpper() : "";
                        txt.color = hotkeyTextColor;
                        txt.fontSize = hotkeyFontSize;
                        txt.alignment = TextAlignmentOptions.Center;
                        txt.fontStyle = FontStyles.Bold;
                        txt.raycastTarget = false;
                        
                        var defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                        if (defaultFont != null) txt.font = defaultFont;
                    }

                    // Add shadow to box for depth
                    var boxShadow = boxGo.GetComponent<Shadow>() ?? boxGo.AddComponent<Shadow>();
                    if (boxShadow != null) boxShadow.effectDistance = new Vector2(1, -1);
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

        #if UNITY_EDITOR
        public void PopulateTools()
        {
            toolManager = FindFirstObjectByType<ToolManager>();
            if (toolManager == null)
            {
                toolManager = GetComponent<ToolManager>();
                if (toolManager == null) return;
            }

            icons.Clear();
            var tools = toolManager.GetComponents<ITool>();
            HashSet<string> usedHotkeys = new HashSet<string>();

            foreach (var tool in tools)
            {
                string toolTypeName = tool.GetType().Name;
                string cleanName = toolTypeName.Replace("Tool", "");

                IconData newData = new IconData();
                newData.iconName = toolTypeName;
                newData.onClick = new UnityEvent();
                
                // Assign Sprite
                newData.iconSprite = FindSpriteForTool(cleanName);

                // Assign Hotkey
                string hotkey = AssignHotkey(toolTypeName, usedHotkeys);
                if (hotkey != null)
                {
                    newData.hotkey = hotkey;
                    usedHotkeys.Add(hotkey);
                }

                // Setup Event
                UnityEditor.Events.UnityEventTools.AddStringPersistentListener(
                    newData.onClick, 
                    toolManager.SelectToolByName, 
                    toolTypeName
                );

                icons.Add(newData);
            }

            UnityEditor.EditorUtility.SetDirty(this);
            ClearUIImmediate(); 
            RefreshUI();
        }

        private Sprite FindSpriteForTool(string name)
        {
            string[] searchNames = { $"Icon_{name}", "Icon_Select" };
            if (name == "Pathfinding") searchNames = new[] { "Icon_Pathfinding", "Icon_Select" };

            foreach (var searchName in searchNames)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets($"{searchName} t:Sprite", new[] { iconFolder });
                if (guids.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }
            }
            return null;
        }

        private string AssignHotkey(string name, HashSet<string> used)
        {
            string upperName = name.ToUpper();
            foreach (char c in upperName)
            {
                string key = c.ToString();
                if (char.IsLetter(c) && !used.Contains(key)) return key;
            }
            string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            foreach (char c in alphabet)
            {
                string key = c.ToString();
                if (!used.Contains(key)) return key;
            }
            return null;
        }
        #endif
    }
}
