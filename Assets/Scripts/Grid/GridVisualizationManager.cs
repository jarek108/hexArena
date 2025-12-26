using UnityEngine;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HexGame
{
    [ExecuteAlways]
    public class GridVisualizationManager : MonoBehaviour
    {
        public static GridVisualizationManager Instance { get; private set; }

        [System.Serializable]
        public struct RimSettings
        {
            public Color color;
            [Range(0f, 1f)] public float width;
            public float pulsation;
        }

        [System.Serializable]
        public struct StateSetting
        {
            public HexState state;
            public int priority;
            public RimSettings visuals;
        }

        public Material hexSurfaceMaterial;
        public Material hexMaterialSides;

        public Color colorPlains = Color.green;
        public Color colorWater = Color.blue;
        public Color colorMountains = new Color(0.5f, 0.5f, 0.5f);
        public Color colorForest = new Color(0.1f, 0.4f, 0.1f);
        public Color colorDesert = new Color(0.8f, 0.8f, 0.4f);

        [SerializeField] public List<StateSetting> stateSettings = new List<StateSetting>();

        public bool showGrid = true;
        public float gridWidth = 0.05f;

        [SerializeField] private float hexSize = 1f;
        [SerializeField] private bool isPointyTop = true;

        public Grid Grid { get; set; }
        private Mesh hexMesh;
        public Mesh GetHexMesh() { return hexMesh; }

        private void OnValidate()
        {
            #if UNITY_EDITOR
            if (hexSurfaceMaterial == null) hexSurfaceMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/HexSurfaceMaterial.mat");
            if (hexMaterialSides == null) hexMaterialSides = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/HexSideMaterial.mat");
            #endif

            if (stateSettings == null) stateSettings = new List<StateSetting>();

            // Auto-populate and ensure defaults for all states
            foreach (HexState state in System.Enum.GetValues(typeof(HexState)))
            {
                int index = stateSettings.FindIndex(s => s.state == state);
                bool isNew = index == -1;
                
                RimSettings currentVisuals = isNew ? new RimSettings() : stateSettings[index].visuals;

                // Apply defaults if new OR if visuals are essentially uninitialized (Black & 0 width)
                if (isNew || (currentVisuals.color == Color.black && currentVisuals.width == 0f))
                {
                    RimSettings defaultVisuals;
                    switch (state)
                    {
                        case HexState.Hovered:
                            defaultVisuals = new RimSettings { color = Color.yellow, width = 0.15f, pulsation = 5f };
                            break;
                        case HexState.Selected:
                            defaultVisuals = new RimSettings { color = Color.red, width = 0.2f, pulsation = 2f };
                            break;
                        case HexState.Target:
                            defaultVisuals = new RimSettings { color = Color.blue, width = 0.15f, pulsation = 0f };
                            break;
                        case HexState.AttackRange:
                            defaultVisuals = new RimSettings { color = new Color(1f, 0.5f, 0f), width = 0.15f, pulsation = 3f }; // Orange
                            break;
                        case HexState.Path:
                            defaultVisuals = new RimSettings { color = Color.white, width = 0.2f, pulsation = 0f };
                            break;
                        case HexState.ZoneOfControl:
                            defaultVisuals = new RimSettings { color = Color.magenta, width = 0.1f, pulsation = 1f };
                            break;
                        default:
                            defaultVisuals = new RimSettings { color = Color.black, width = 0f };
                            break;
                    }

                    if (isNew)
                    {
                        stateSettings.Add(new StateSetting 
                        { 
                            state = state, 
                            priority = (int)state * 10, 
                            visuals = defaultVisuals
                        });
                    }
                    else
                    {
                        var s = stateSettings[index];
                        s.visuals = defaultVisuals;
                        stateSettings[index] = s;
                    }
                }
            }

            UpdateGridVisibility();
            SyncMaterialWithDefault();
            RefreshVisuals();
        }

        private void OnEnable()
        {
            Instance = this;
            if (Application.IsPlaying(this)) return;
            if (Grid == null) RebuildGridFromChildren();
        }

        private void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        public void ToggleShowGrid()
        {
            showGrid = !showGrid;
            UpdateGridVisibility();
            SyncMaterialWithDefault();
            RefreshVisuals();
        }

        public void RefreshVisuals()
        {
            foreach (Transform child in transform)
            {
                Hex hex = child.GetComponent<Hex>();
                if (hex != null) RefreshVisuals(hex);
            }
        }

        public void RefreshVisuals(Hex hex)
        {
            if (hex == null || hex.Data == null || stateSettings == null || hex.Data.States == null) return;

            // Apply base color based on terrain
            SetHexColor(hex, GetDefaultHexColor(hex));

            // Find settings for all active states (excluding Default, which is our fallback)
            var activeSettings = stateSettings
                .Where(s => s.state != HexState.Default && hex.Data.States.Contains(s.state))
                .OrderByDescending(s => s.priority)
                .ToList();

            StateSetting bestSetting;
            if (activeSettings.Count > 0)
            {
                bestSetting = activeSettings[0];
            }
            else
            {
                bestSetting = stateSettings.FirstOrDefault(s => s.state == HexState.Default);
            }

            SetHexRim(hex, bestSetting.visuals);
        }

        private void UpdateGridVisibility()
        {
            if (stateSettings == null) return;
            var defaultIndex = stateSettings.FindIndex(s => s.state == HexState.Default);
            if (defaultIndex != -1)
            {
                var setting = stateSettings[defaultIndex];
                setting.visuals.width = showGrid ? gridWidth : 0f;
                stateSettings[defaultIndex] = setting;
            }
        }

        public void SyncMaterialWithDefault()
        {
            if (hexSurfaceMaterial == null) return;
            var defaultSetting = stateSettings.FirstOrDefault(s => s.state == HexState.Default);
            
            hexSurfaceMaterial.SetColor("_RimColor", defaultSetting.visuals.color);
            hexSurfaceMaterial.SetFloat("_RimWidth", defaultSetting.visuals.width);
            hexSurfaceMaterial.SetFloat("_RimPulsationSpeed", defaultSetting.visuals.pulsation);
            
            #if UNITY_EDITOR
            if (!Application.isPlaying) EditorUtility.SetDirty(hexSurfaceMaterial);
            #endif
        }

        private void RebuildGridFromChildren()
        {
             Hex[] childHexes = GetComponentsInChildren<Hex>();
             if (childHexes.Length == 0) return;

             int maxQ = 0; int maxR = 0;
             foreach(var hex in childHexes) {
                 if (hex.Q > maxQ) maxQ = hex.Q;
                 if (hex.R > maxR) maxR = hex.R;
             }
             Grid = new Grid(maxQ + 1, maxR + 1);

             foreach(var hex in childHexes) {
                 if (hex.Data == null) {
                     HexData data = new HexData(hex.Q, hex.R);
                     data.Elevation = hex.Elevation;
                     data.TerrainType = hex.TerrainType;
                     hex.AssignData(data);
                 }
                 Grid.AddHex(hex.Data);
             }
             RefreshVisuals();
        }

        public void InitializeVisuals()
        {
            Instance = this;
            hexMesh = CreateHexMesh();
            #if UNITY_EDITOR
            if (hexSurfaceMaterial == null) hexSurfaceMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/HexSurfaceMaterial.mat");
            if (hexMaterialSides == null) hexMaterialSides = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/HexSideMaterial.mat");

            // If material missing, try loading the custom shader directly
            if (hexSurfaceMaterial == null)
            {
                Shader s = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Materials/HexSurfaceShader.shadergraph");
                if (s != null)
                {
                    hexSurfaceMaterial = new Material(s);
                    hexSurfaceMaterial.name = "CustomSurfaceFallback";
                }
            }
            #endif

            // Clone to ensure unique instance for this manager (especially important for EditMode tests)
            if (hexSurfaceMaterial != null) hexSurfaceMaterial = new Material(hexSurfaceMaterial);
            if (hexMaterialSides != null) hexMaterialSides = new Material(hexMaterialSides);

            // Last resort fallbacks
            if (hexSurfaceMaterial == null)
            {
                Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                hexSurfaceMaterial = new Material(s);
                hexSurfaceMaterial.name = "StandardFallback";
            }
            if (hexMaterialSides == null)
            {
                Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                hexMaterialSides = new Material(s);
                hexMaterialSides.name = "FallbackSides";
            }
        }

        public void VisualizeGrid(Grid newGrid)
        {
            Grid = newGrid;
            InitializeVisuals();

            foreach (HexData hexData in Grid.GetAllHexes())
            {
                Vector3 localPos = HexToWorld(hexData.Q, hexData.R);
                localPos.y = hexData.Elevation;

                GameObject hexGO = new GameObject($"Hex ({hexData.Q}, {hexData.R})");
                hexGO.transform.SetParent(this.transform);
                hexGO.transform.localPosition = localPos;
                
                hexGO.AddComponent<MeshFilter>().sharedMesh = hexMesh;
                MeshRenderer mr = hexGO.AddComponent<MeshRenderer>();
                mr.sharedMaterials = new Material[] { hexSurfaceMaterial, hexMaterialSides };
                hexGO.AddComponent<MeshCollider>().sharedMesh = hexMesh;

                Hex hex = hexGO.AddComponent<Hex>();
                hex.AssignData(hexData); 
            }
            RefreshVisuals();
        }

        private Mesh CreateHexMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "HexagonalMesh";
            float size = hexSize;
            float depth = 5f;

            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> topTriangles = new List<int>();
            List<int> sideTriangles = new List<int>();
            
            vertices.Add(Vector3.zero); 
            uvs.Add(new Vector2(1f, 0f));
            
            for (int i = 0; i < 6; i++) {
                float angle_rad = Mathf.Deg2Rad * (60 * i + (isPointyTop ? 30 : 0));
                vertices.Add(new Vector3(size * Mathf.Cos(angle_rad), 0, size * Mathf.Sin(angle_rad)));
                uvs.Add(new Vector2(0f, 0f));
            }
            for (int i = 0; i < 6; i++) {
                topTriangles.Add(0);
                topTriangles.Add((i == 5) ? 1 : i + 2);
                topTriangles.Add(i + 1);
            }

            int sideStartIndex = vertices.Count;
            for (int i = 0; i < 6; i++) {
                float angle_rad = Mathf.Deg2Rad * (60 * i + (isPointyTop ? 30 : 0));
                float next_angle_rad = Mathf.Deg2Rad * (60 * ((i + 1) % 6) + (isPointyTop ? 30 : 0));

                vertices.Add(new Vector3(size * Mathf.Cos(angle_rad), 0, size * Mathf.Sin(angle_rad)));
                vertices.Add(new Vector3(size * Mathf.Cos(next_angle_rad), 0, size * Mathf.Sin(next_angle_rad)));
                vertices.Add(new Vector3(size * Mathf.Cos(angle_rad), -depth, size * Mathf.Sin(angle_rad)));
                vertices.Add(new Vector3(size * Mathf.Cos(next_angle_rad), -depth, size * Mathf.Sin(next_angle_rad)));
                
                uvs.Add(new Vector2(0, 1)); uvs.Add(new Vector2(1, 1)); uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(1, 0));
                
                int currentBase = sideStartIndex + (i * 4);
                sideTriangles.Add(currentBase); sideTriangles.Add(currentBase + 1); sideTriangles.Add(currentBase + 3);
                sideTriangles.Add(currentBase); sideTriangles.Add(currentBase + 3); sideTriangles.Add(currentBase + 2);
            }

            mesh.SetVertices(vertices); mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(topTriangles, 0); mesh.SetTriangles(sideTriangles, 1);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        public Vector3 HexToWorld(Hex hex) { return HexToWorld(hex.Q, hex.R); }
        public Vector3 HexToWorld(int q, int r) {
             float x, z;
            if (isPointyTop) {
                x = hexSize * Mathf.Sqrt(3) * (q + r / 2f);
                z = hexSize * 3f / 2f * r;
            } else {
                x = hexSize * 3f / 2f * q;
                z = hexSize * Mathf.Sqrt(3) * (r + q / 2f);
            }
            return new Vector3(x, 0, z);
        }

        public Hex GetHexView(HexData data) {
            if (data == null) return null;
            foreach (Transform child in transform) {
                Hex hex = child.GetComponent<Hex>();
                if (hex != null && hex.Data == data) return hex;
            }
            return null;
        }

        public Hex WorldToHex(Vector3 worldPos) {
            Vector3 localPos = transform.InverseTransformPoint(worldPos);
            float q_float, r_float;
            if (isPointyTop) {
                q_float = (Mathf.Sqrt(3) / 3f * localPos.x - 1f / 3f * localPos.z) / hexSize;
                r_float = (2f / 3f * localPos.z) / hexSize;
            } else {
                q_float = (2f / 3f * localPos.x) / hexSize;
                r_float = (-1f / 3f * localPos.x + Mathf.Sqrt(3) / 3f * localPos.z) / hexSize;
            }
            Vector3Int coords = RoundToHex(q_float, r_float);
            if(Grid == null) RebuildGridFromChildren();
            return GetHexView(Grid.GetHexAt(coords.x, coords.y));
        }

        private Vector3Int RoundToHex(float q_float, float r_float) {
            float s_float = -q_float - r_float;
            int q = Mathf.RoundToInt(q_float); int r = Mathf.RoundToInt(r_float); int s = Mathf.RoundToInt(s_float);
            float q_diff = Mathf.Abs(q - q_float); float r_diff = Mathf.Abs(r - r_float); float s_diff = Mathf.Abs(s - s_float);
            if (q_diff > r_diff && q_diff > s_diff) q = -r - s;
            else if (r_diff > s_diff) r = -q - s;
            else s = -q - r;
            return new Vector3Int(q, r, s);
        }

        // Visual cache for EditMode/Tests - static to persist in tests
        private static Dictionary<string, Color> baseColorCache = new Dictionary<string, Color>();
        private static Dictionary<string, RimSettings> rimSettingsCache = new Dictionary<string, RimSettings>();

        public void ClearCache() {
            baseColorCache.Clear();
            rimSettingsCache.Clear();
        }

        private string GetHexKey(Hex hex) => $"{hex.Q}_{hex.R}";

        public void SetHexRim(Hex hex, RimSettings settings) {
            if (hex == null) return;
            Renderer renderer = hex.GetComponent<Renderer>();
            if (renderer != null) {
                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(propertyBlock, 0); 
                propertyBlock.SetColor("_RimColor", settings.color);
                propertyBlock.SetFloat("_RimWidth", settings.width);
                propertyBlock.SetFloat("_RimPulsationSpeed", settings.pulsation);
                renderer.SetPropertyBlock(propertyBlock, 0);
                
                if (!Application.isPlaying) rimSettingsCache[GetHexKey(hex)] = settings;
            }
        }
        
        public void SetHexColor(Hex hex, Color color) {
            if (hex == null) return;
            Renderer renderer = hex.GetComponent<Renderer>();
            if (renderer != null) {
                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(propertyBlock, 0);
                propertyBlock.SetColor("_BaseColor", color);
                renderer.SetPropertyBlock(propertyBlock, 0);
                
                if (!Application.isPlaying) baseColorCache[GetHexKey(hex)] = color;
            }
        }

        public Color GetHexColor(Hex hex) {
            if (hex == null) return Color.clear;
            if (!Application.isPlaying && baseColorCache.TryGetValue(GetHexKey(hex), out Color c)) return c;
            
            Renderer renderer = hex.GetComponent<Renderer>();
            if (renderer == null) return Color.clear;
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propertyBlock, 0);
            return propertyBlock.GetColor("_BaseColor");
        }

        public Color GetHexRimColor(Hex hex) {
            if (hex == null) return Color.clear;
            if (!Application.isPlaying && rimSettingsCache.TryGetValue(GetHexKey(hex), out RimSettings s)) return s.color;

            Renderer renderer = hex.GetComponent<Renderer>();
            if (renderer == null) return Color.clear;
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propertyBlock, 0);
            return propertyBlock.GetColor("_RimColor");
        }

        public float GetHexRimWidth(Hex hex) {
            if (hex == null) return 0f;
            if (!Application.isPlaying && rimSettingsCache.TryGetValue(GetHexKey(hex), out RimSettings s)) return s.width;

            Renderer renderer = hex.GetComponent<Renderer>();
            if (renderer == null) return 0f;
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propertyBlock, 0);
            return propertyBlock.GetFloat("_RimWidth");
        }

        public float GetHexRimPulsation(Hex hex) {
            if (hex == null) return 0f;
            if (!Application.isPlaying && rimSettingsCache.TryGetValue(GetHexKey(hex), out RimSettings s)) return s.pulsation;

            Renderer renderer = hex.GetComponent<Renderer>();
            if (renderer == null) return 0f;
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propertyBlock, 0);
            return propertyBlock.GetFloat("_RimPulsationSpeed");
        }

        public Color GetDefaultHexColor(Hex hex) {
            switch (hex.TerrainType)
            {
                case TerrainType.Plains: return colorPlains;
                case TerrainType.Water: return colorWater;
                case TerrainType.Mountains: return colorMountains;
                case TerrainType.Forest: return colorForest;
                case TerrainType.Desert: return colorDesert;
                default: return Color.white;
            }
        }

        public RimSettings GetDefaultRimSettings() {
            if (stateSettings == null || stateSettings.Count == 0) 
                return new RimSettings { color = Color.black, width = 0f };
            return stateSettings.FirstOrDefault(s => s.state == HexState.Default).visuals;
        }
    }
}
