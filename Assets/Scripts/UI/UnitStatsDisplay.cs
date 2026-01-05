using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using HexGame;
using System.Linq;
using TMPro;

namespace HexGame.UI
{
    [ExecuteAlways]
    public class UnitStatsDisplay : MonoBehaviour
    {
        public enum SelectionMode { Hover, LClick, RClick, AnyClick }

        public Unit displayedUnit;

        public RectTransform panel;
        public Image dividerLine;
        public TextMeshProUGUI unitNameText;
        public TextMeshProUGUI unitStatsText;

        public TMP_FontAsset nameFont;
        public int nameFontSize = 20;
        public Color nameColor = Color.white;
        public TextAlignmentOptions nameAlignment = TextAlignmentOptions.TopLeft;

        public TMP_FontAsset statsFont;
        public int statsFontSize = 14;
        public Color statsColor = new Color(0.8f, 0.8f, 0.8f);
        public TextAlignmentOptions statsAlignment = TextAlignmentOptions.TopLeft;

        public SelectionMode chooseUnitOn = SelectionMode.Hover;
        public bool continuouslyVisible = false;
        public bool keepShowingLastUnit = false;
        public bool multilineUnitNames = false;
        
        public Color backgroundColor = new Color(0, 0, 0, 0.7f);
        public Sprite backgroundSprite;
        public bool useGradient = true;
        public Color gradientColorBottom = new Color(0, 0, 0, 0.9f);
        
        public bool useDividerLine = true;
        public Color dividerColor = new Color(1, 1, 1, 0.3f);
        public float dividerHeight = 1f;

        public float paddingX = 15f;
        public float paddingY = 10f;
        public float nameToStatsSpacing = 5f;
        public float statIdToValueSpacing = 80f;
        public Vector2 panelPosition = new Vector2(20, -20); // Top left

        private HexRaycaster raycaster;

        private void Start()
        {
            raycaster = FindFirstObjectByType<HexRaycaster>();
            EnsureUI();
        }

        private void Update()
        {
            if (Application.isPlaying)
            {
                UpdateTarget();
            }
            UpdateUI();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                // Delay call to ensure we don't modify hierarchy during OnValidate
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.delayCall += () => {
                    if (this != null)
                    {
                        EnsureUI();
                        UpdateUI();
                    }
                };
                #endif
            }
        }

        private void UpdateTarget()
        {
            if (raycaster == null) return;
            
            bool shouldUpdate = false;
            if (chooseUnitOn == SelectionMode.Hover)
            {
                shouldUpdate = true;
            }
            else if (Mouse.current != null)
            {
                bool lClick = Mouse.current.leftButton.wasPressedThisFrame;
                bool rClick = Mouse.current.rightButton.wasPressedThisFrame;

                if (chooseUnitOn == SelectionMode.LClick && lClick) shouldUpdate = true;
                else if (chooseUnitOn == SelectionMode.RClick && rClick) shouldUpdate = true;
                else if (chooseUnitOn == SelectionMode.AnyClick && (lClick || rClick)) shouldUpdate = true;
            }

            if (shouldUpdate)
            {
                Unit foundUnit = null;
                if (raycaster.currentHex != null)
                {
                    foundUnit = raycaster.currentHex.Unit;
                }

                if (foundUnit != null)
                {
                    displayedUnit = foundUnit;
                }
                else if (!keepShowingLastUnit)
                {
                    displayedUnit = null;
                }
            }
        }

        private void UpdateUI()
        {
            if (panel == null) return;

            bool shouldShow = continuouslyVisible || displayedUnit != null || !Application.isPlaying;
            panel.gameObject.SetActive(shouldShow);

            if (shouldShow)
            {
                // Force layout updates
                panel.anchoredPosition = panelPosition;
                var bg = panel.GetComponent<Image>();
                if (bg != null) 
                {
                    bg.color = backgroundColor;
                    bg.sprite = backgroundSprite;
                    bg.type = backgroundSprite != null ? Image.Type.Sliced : Image.Type.Simple;
                }

                // Gradient handling
                var grad = panel.GetComponent<UIGradient>();
                if (useGradient)
                {
                    if (grad == null) grad = panel.gameObject.AddComponent<UIGradient>();
                    grad.enabled = true;
                    grad.colorTop = backgroundColor;
                    grad.colorBottom = gradientColorBottom;
                }
                else if (grad != null)
                {
                    grad.enabled = false;
                }

                if (dividerLine != null)
                {
                    dividerLine.gameObject.SetActive(useDividerLine);
                    dividerLine.color = dividerColor;
                    
                    var le = dividerLine.GetComponent<LayoutElement>();
                    if (le == null) le = dividerLine.gameObject.AddComponent<LayoutElement>();
                    le.minHeight = dividerHeight;
                    le.preferredHeight = dividerHeight;
                }

                var vlg = panel.GetComponent<VerticalLayoutGroup>();
                if (vlg != null)
                {
                    vlg.padding = new RectOffset((int)paddingX, (int)paddingX, (int)paddingY, (int)paddingY);
                    vlg.spacing = nameToStatsSpacing;
                }

                ApplyFontSettings();

                // Force layout rebuild so ContentSizeFitter and VerticalLayoutGroup update immediately
                LayoutRebuilder.ForceRebuildLayoutImmediate(panel);

                if (displayedUnit != null)
                {
                    if (unitNameText != null) 
                    {
                        string name = displayedUnit.UnitName;
                        if (multilineUnitNames) name = name.Replace(" ", "\n");
                        unitNameText.text = name;
                    }
                    
                    if (unitStatsText != null)
                    {
                        var set = displayedUnit.unitSet;
                        var schema = set != null ? set.schemaDefinitions : null;
                        if (schema != null && schema.Count > 0)
                        {
                            System.Text.StringBuilder sb = new System.Text.StringBuilder();
                            foreach (var def in schema)
                            {
                                int val = displayedUnit.GetStat(def.id);
                                int max = displayedUnit.GetBaseStat(def.id);
                                // Use TMP <noparse> to ensure IDs don't mess with tags, and <pos> for alignment
                                sb.AppendLine($"{def.id}:<pos={statIdToValueSpacing}>{val}/{max}");
                            }
                            unitStatsText.text = sb.ToString().TrimEnd();
                        }
                        else
                        {
                            unitStatsText.text = "No stats available";
                        }
                    }
                }
                else
                {
                    if (unitNameText != null) 
                        unitNameText.text = Application.isPlaying ? "No Unit Selected" : "Unit Name";
                    if (unitStatsText != null)
                        unitStatsText.text = Application.isPlaying ? "" : "Stats list...";
                }
            }
        }

        private void ApplyFontSettings()
        {
            if (unitNameText != null)
            {
                if (nameFont != null) unitNameText.font = nameFont;
                unitNameText.fontSize = nameFontSize;
                unitNameText.color = nameColor;
                unitNameText.alignment = nameAlignment;
                unitNameText.enableWordWrapping = true;
            }

            if (unitStatsText != null)
            {
                if (statsFont != null) unitStatsText.font = statsFont;
                unitStatsText.fontSize = statsFontSize;
                unitStatsText.color = statsColor;
                unitStatsText.alignment = statsAlignment;
                unitStatsText.enableWordWrapping = false;
            }
        }

        private void EnsureUI()
        {
            // 1. Try to find panel in children of THIS object first (preferred structure)
            if (panel == null)
            {
                panel = GetComponentInChildren<RectTransform>(true);
                // Filter out if it's our own transform
                if (panel != null && panel.gameObject == this.gameObject) panel = null;
                
                // If still null, check by name under this transform
                if (panel == null)
                {
                    Transform t = transform.Find("UnitStatsPanel");
                    if (t != null) panel = t.GetComponent<RectTransform>();
                }
            }

            // 2. Fallback to finding or creating Canvas
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGo = new GameObject("UI Canvas");
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGo.AddComponent<CanvasScaler>();
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            if (panel == null)
            {
                // Check globally if it exists somewhere else
                GameObject existing = GameObject.Find("UnitStatsPanel");
                if (existing != null) panel = existing.GetComponent<RectTransform>();
            }

            if (panel == null)
            {
                // Create Panel
                GameObject panelGo = new GameObject("UnitStatsPanel");
                panel = panelGo.AddComponent<RectTransform>();
                panel.transform.SetParent(canvas.transform, false); // Parent to Canvas directly

                // Anchor to top-left
                panel.anchorMin = new Vector2(0, 1);
                panel.anchorMax = new Vector2(0, 1);
                panel.pivot = new Vector2(0, 1);
                
                Image bg = panelGo.AddComponent<Image>();
                bg.color = backgroundColor;
            }
            else if (panel.parent != canvas.transform)
            {
                panel.SetParent(canvas.transform, false);
            }

            // Ensure Layout Components
            VerticalLayoutGroup vlg = panel.GetComponent<VerticalLayoutGroup>();
            if (vlg == null) vlg = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;

            ContentSizeFitter csf = panel.GetComponent<ContentSizeFitter>();
            if (csf == null) csf = panel.gameObject.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Ensure text references are linked
            if (unitNameText == null) EnsureTextElement(ref unitNameText, "UnitNameText");
            
            if (dividerLine == null)
            {
                Transform t = panel.Find("DividerLine");
                if (t != null) dividerLine = t.GetComponent<Image>();
            }
            if (dividerLine == null)
            {
                GameObject lineGo = new GameObject("DividerLine");
                lineGo.transform.SetParent(panel, false);
                dividerLine = lineGo.AddComponent<Image>();
            }
            // Ensure name is first, then divider, then stats
            unitNameText.transform.SetAsFirstSibling();
            dividerLine.transform.SetSiblingIndex(1);
            
            if (unitStatsText == null) EnsureTextElement(ref unitStatsText, "UnitStatsText");
            unitStatsText.transform.SetAsLastSibling();
        }

        private void EnsureTextElement(ref TextMeshProUGUI textField, string name)
        {
            if (textField == null)
            {
                Transform t = panel.Find(name);
                if (t != null) textField = t.GetComponent<TextMeshProUGUI>();
            }

            if (textField == null)
            {
                GameObject textGo = new GameObject(name);
                textGo.transform.SetParent(panel, false);
                textField = textGo.AddComponent<TextMeshProUGUI>();
                textField.raycastTarget = false;
                
                // Try to load default font
                var defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                if (defaultFont != null) textField.font = defaultFont;
            }
        }
    }
}
