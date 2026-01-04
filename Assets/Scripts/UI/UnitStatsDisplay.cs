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

        [Header("UI References")]
        public RectTransform panel;
        public Text unitNameText;

        [Header("Settings")]
        public SelectionMode chooseUnitOn = SelectionMode.Hover;
        public bool continuouslyVisible = false;
        public bool keepShowingLastUnit = false;
        public Color backgroundColor = new Color(0, 0, 0, 0.7f);
        public Vector2 panelSize = new Vector2(250, 60);
        public Vector2 panelPosition = new Vector2(20, -20); // Top left

        public Unit displayedUnit;
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
                    if (this != null) EnsureUI();
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
                if (displayedUnit != null)
                {
                    if (unitNameText != null) unitNameText.text = displayedUnit.UnitName;
                }
                else
                {
                    if (unitNameText != null) 
                        unitNameText.text = Application.isPlaying ? "No Unit Selected" : "Unit Name";
                }
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
                    // Also try to link the text if it exists
                    if (unitNameText == null)
                    {
                        Transform t = existing.Find("UnitNameText");
                        if (t != null) unitNameText = t.GetComponent<Text>();
                    }
                }
            }

            if (panel != null) 
            {
                // Ensure text exists if panel was found but text wasn't linked
                if (unitNameText == null) CreateText(panel);
                return;
            }

            // Create Panel
            GameObject panelGo = new GameObject("UnitStatsPanel");
            panelGo.transform.SetParent(canvas.transform, false);
            panel = panelGo.AddComponent<RectTransform>();

            // Anchor to top-left
            panel.anchorMin = new Vector2(0, 1);
            panel.anchorMax = new Vector2(0, 1);
            panel.pivot = new Vector2(0, 1);
            panel.sizeDelta = panelSize;
            panel.anchoredPosition = panelPosition;

            Image bg = panelGo.AddComponent<Image>();
            bg.color = backgroundColor;

            CreateText(panel);
        }

        private void CreateText(RectTransform parent)
        {
            if (unitNameText != null) return;

            GameObject textGo = new GameObject("UnitNameText");
            textGo.transform.SetParent(parent.transform, false);
            unitNameText = textGo.AddComponent<Text>();
            
            // Try to find a font
            unitNameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            unitNameText.fontSize = 24;
            unitNameText.color = Color.white;
            unitNameText.alignment = TextAnchor.MiddleLeft;

            RectTransform textRT = textGo.GetComponent<RectTransform>();
            textRT.anchorMin = new Vector2(0, 0);
            textRT.anchorMax = new Vector2(1, 1);
            textRT.pivot = new Vector2(0, 1);
            textRT.offsetMin = new Vector2(15, 0);
            textRT.offsetMax = new Vector2(-15, 0);
        }
    }
}
