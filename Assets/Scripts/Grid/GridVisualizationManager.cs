using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;
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
            public string state;
            public int priority;
            public RimSettings visuals;
        }

        [Header("State Visuals")]
        public string stateVisualsFile = "default_states.json";
        public List<StateSetting> stateSettings = new List<StateSetting>();

        public Material hexSurfaceMaterial;
        public Material hexMaterialSides;

        public Color colorPlains = Color.green;
        public Color colorWater = Color.blue;
        public Color colorMountains = new Color(0.5f, 0.5f, 0.5f);
        public Color colorForest = new Color(0.1f, 0.4f, 0.1f);
        public Color colorDesert = new Color(0.8f, 0.8f, 0.4f);

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

            if (stateSettings == null || stateSettings.Count == 0)
            {
                LoadStateSettings();
            }

            UpdateGridVisibility();
            SyncMaterialWithDefault();
            RefreshVisuals();
        }

        public void SortSettings()
        {
            if (stateSettings != null)
            {
                // Order by priority descending, keeping Default at the very bottom (0)
                stateSettings = stateSettings
                    .OrderByDescending(s => s.priority)
                    .ToList();
            }
        }

        public void LoadStateSettings(string explicitPath = null)
        {
            string path = explicitPath;
            if (string.IsNullOrEmpty(path))
            {
                path = Path.Combine(Application.dataPath, "Data/StateVisuals", stateVisualsFile);
            }

            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                StateSettingsWrapper wrapper = JsonUtility.FromJson<StateSettingsWrapper>(json);
                stateSettings = wrapper.settings;
            }
        }

        public void SaveStateSettings(string explicitPath = null)
        {
            string path = explicitPath;
            if (string.IsNullOrEmpty(path))
            {
                string dir = Path.Combine(Application.dataPath, "Data/StateVisuals");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                path = Path.Combine(dir, stateVisualsFile);
            }
            
            StateSettingsWrapper wrapper = new StateSettingsWrapper { settings = stateSettings };
            string json = JsonUtility.ToJson(wrapper, true);
            File.WriteAllText(path, json);
        }

        [System.Serializable]
        private class StateSettingsWrapper
        {
            public List<StateSetting> settings;
        }

        private void OnEnable()
        {
            Instance = this;
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

        public RimSettings GetDefaultRimSettings()
        {
            var setting = stateSettings.FirstOrDefault(s => s.state == "Default");
            if (string.IsNullOrEmpty(setting.state))
            {
                return new RimSettings { color = Color.black, width = showGrid ? gridWidth : 0f, pulsation = 0f };
            }
            return setting.visuals;
        }

        public void RefreshVisuals(Hex hex)
        {
            if (hex == null || hex.Data == null || stateSettings == null || hex.Data.States == null) return;

            // Apply base color based on terrain
            SetHexColor(hex, GetDefaultHexColor(hex));

            // Find the highest priority setting where the hex has a matching state.
            // A match is either an exact string match OR a prefix match followed by '_'
            // (e.g., rule "ZoC_0" matches state "ZoC_0_123").
            var bestSetting = stateSettings
                .OrderByDescending(s => s.priority)
                .FirstOrDefault(s => s.priority > 0 && 
                    hex.Data.States.Any(activeState => 
                        activeState == s.state || activeState.StartsWith(s.state + "_")
                    )
                );

            // Fallback to Default if no positive-priority state is found
            if (string.IsNullOrEmpty(bestSetting.state))
            {
                SetHexRim(hex, GetDefaultRimSettings());
            }
            else
            {
                SetHexRim(hex, bestSetting.visuals);
            }
        }

        private void UpdateGridVisibility()
        {
            // If Default is in the list, update its width
            int defaultIndex = stateSettings.FindIndex(s => s.state == "Default");
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
            var visuals = GetDefaultRimSettings();
            
            hexSurfaceMaterial.SetColor("_RimColor", visuals.color);
            hexSurfaceMaterial.SetFloat("_RimWidth", visuals.width);
            hexSurfaceMaterial.SetFloat("_RimPulsationSpeed", visuals.pulsation);
            
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

        public Hex GetHex(int q, int r)
        {
            if (Grid == null) return null;
            return GetHexView(Grid.GetHexAt(q, r));
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
    }
}
