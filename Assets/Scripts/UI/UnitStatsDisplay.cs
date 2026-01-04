using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using HexGame;
using System.Linq;

namespace HexGame.UI
{
    [ExecuteAlways]
    public class UnitStatsDisplay : MonoBehaviour
    {
        public enum SelectionMode { Hover, LClick, RClick, AnyClick }

        public Unit displayedUnit;

        [Header("UI References")]
        public RectTransform panel;
        public Text unitNameText;
        public Text unitStatsText;

        [Header("Font Settings")]
        public Font nameFont;
        public int nameFontSize = 20;
        public Color nameColor = Color.white;
        public TextAnchor nameAlignment = TextAnchor.UpperLeft;

        public Font statsFont;
        public int statsFontSize = 14;
        public Color statsColor = new Color(0.8f, 0.8f, 0.8f);
        public TextAnchor statsAlignment = TextAnchor.UpperLeft;

        [Header("Settings")]
        public SelectionMode chooseUnitOn = SelectionMode.Hover;
        public bool continuouslyVisible = false;
        public bool keepShowingLastUnit = false;
        public Color backgroundColor = new Color(0, 0, 0, 0.7f);
        public Vector2 panelSize = new Vector2(250, 200);
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
                ApplyFontSettings();

                if (displayedUnit != null)
                {
                    if (unitNameText != null) unitNameText.text = displayedUnit.UnitName;
                    
                    if (unitStatsText != null)
                    {
                        var schema = displayedUnit.unitSet?.schemaDefinitions;
                        if (schema != null && schema.Count > 0)
                        {
                            System.Text.StringBuilder sb = new System.Text.StringBuilder();
                            foreach (var def in schema)
                            {
                                int val = displayedUnit.GetStat(def.id);
                                sb.AppendLine($"{def.name}: {val}");
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
            }

            if (unitStatsText != null)
            {
                if (statsFont != null) unitStatsText.font = statsFont;
                unitStatsText.fontSize = statsFontSize;
                unitStatsText.color = statsColor;
                unitStatsText.alignment = statsAlignment;
            }
        }

        private void EnsureUI()
        {
            // Find or create Canvas
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGo = new GameObject("UI Canvas");
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGo.AddComponent<CanvasScaler>();
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            // Check if panel already exists under this canvas to prevent duplicates
            if (panel == null)
            {
                Transform existing = canvas.transform.Find("UnitStatsPanel");
                if (existing != null)
                {
                    panel = existing.GetComponent<RectTransform>();
                }
            }

            if (panel == null)
            {
                // Create Panel
                GameObject panelGo = new GameObject("UnitStatsPanel");
                panelGo.transform.SetParent(canvas.transform, false);
                panel = panelGo.AddComponent<RectTransform>();

                // Anchor to top-left
                panel.anchorMin = new Vector2(0, 1);
                panel.anchorMax = new Vector2(0, 1);
                panel.pivot = new Vector2(0, 1);
                
                Image bg = panelGo.AddComponent<Image>();
                bg.color = backgroundColor;
            }

            panel.sizeDelta = panelSize;
            panel.anchoredPosition = panelPosition;
            if (panel.GetComponent<Image>() != null) panel.GetComponent<Image>().color = backgroundColor;

            EnsureTextElement(ref unitNameText, "UnitNameText", 15, -15, 0.85f, 1.0f);
            EnsureTextElement(ref unitStatsText, "UnitStatsText", 15, -15, 0.0f, 0.85f);
        }

        private void EnsureTextElement(ref Text textField, string name, float left, float right, float minV, float maxV)
        {
            if (textField == null)
            {
                Transform t = panel.Find(name);
                if (t != null) textField = t.GetComponent<Text>();
            }

            if (textField == null)
            {
                GameObject textGo = new GameObject(name);
                textGo.transform.SetParent(panel, false);
                textField = textGo.AddComponent<Text>();
                textField.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            RectTransform rt = textField.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, minV);
            rt.anchorMax = new Vector2(1, maxV);
            rt.offsetMin = new Vector2(left, 5);
            rt.offsetMax = new Vector2(right, -5);
        }
    }
}
