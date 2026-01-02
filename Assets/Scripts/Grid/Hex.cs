using UnityEngine;

namespace HexGame
{
    [ExecuteAlways]
    public class Hex : MonoBehaviour
    {
        public HexData Data { get; private set; }

        private TerrainType? _previewTerrain;

        // Properties: Read directly from Data
        public int Q => Data != null ? Data.Q : 0;
        public int R => Data != null ? Data.R : 0;
        public int S => Data != null ? Data.S : 0;

        public float Elevation
        {
            get => Data != null ? Data.Elevation : 0f;
            set
            {
                if (Data != null) 
                {
                    Data.Elevation = value;
                }
            }
        }

        public TerrainType TerrainType
        {
            get 
            {
                if (_previewTerrain.HasValue) return _previewTerrain.Value;
                return Data != null ? Data.TerrainType : TerrainType.Plains;
            }
            set
            {
                if (Data != null)
                {
                    Data.TerrainType = value;
                }
            }
        }

        public void SetPreviewTerrain(TerrainType? type)
        {
            if (_previewTerrain != type)
            {
                _previewTerrain = type;
                UpdateVisuals();
            }
        }

        public System.Collections.Generic.List<Unit> Units => Data?.Units;

        public Unit Unit
        {
            get => Data?.Unit;
            set { if (Data != null) Data.Unit = value; }
        }

        public void AssignData(HexData data)
        {
            if (Data != null)
            {
                Data.OnStateChanged -= HandleStateChanged;
                Data.OnTerrainChanged -= HandleTerrainChanged;
                Data.OnElevationChanged -= HandleElevationChanged;
            }

            Data = data;
            
            if (Data != null)
            {
                Data.OnStateChanged += HandleStateChanged;
                Data.OnTerrainChanged += HandleTerrainChanged;
                Data.OnElevationChanged += HandleElevationChanged;
            }

            name = $"Hex ({Q}, {R})";
            UpdatePosition();
            UpdateVisuals();
            HandleStateChanged(); // Ensure initial state (Default) is applied
        }

        private void OnEnable()
        {
            if (Data != null)
            {
                Data.OnStateChanged -= HandleStateChanged;
                Data.OnStateChanged += HandleStateChanged;
                Data.OnTerrainChanged -= HandleTerrainChanged;
                Data.OnTerrainChanged += HandleTerrainChanged;
                Data.OnElevationChanged -= HandleElevationChanged;
                Data.OnElevationChanged += HandleElevationChanged;
            }
        }

        private void OnDisable()
        {
            if (Data != null)
            {
                Data.OnStateChanged -= HandleStateChanged;
                Data.OnTerrainChanged -= HandleTerrainChanged;
                Data.OnElevationChanged -= HandleElevationChanged;
            }
        }

        public void HandleStateChanged()
        {
            var manager = FindFirstObjectByType<GridVisualizationManager>() ?? GridVisualizationManager.Instance;
            if (manager != null)
            {
                manager.RefreshVisuals(this);
            }
        }

        private void HandleTerrainChanged()
        {
            UpdateVisuals();
        }

        private void HandleElevationChanged()
        {
            UpdatePosition();

            // Trigger unit repositioning if present
            if (Data != null && Data.Units != null)
            {
                foreach (var unit in Data.Units)
                {
                    if (unit != null) unit.UpdateVisualPosition();
                }
            }
        }
        
        private void UpdatePosition()
        {
            Vector3 pos = transform.position;
            pos.y = Elevation;
            transform.position = pos;
        }

        [ContextMenu("Update Hex Visuals")]
        private void OnValidate()
        {
            UpdatePosition(); 
            UpdateVisuals();
            HandleStateChanged(); 
        }

        private void UpdateVisuals()
        {
            GridVisualizationManager manager = FindFirstObjectByType<GridVisualizationManager>() ?? GridVisualizationManager.Instance;
            if (manager != null && GetComponent<Renderer>() != null)
            {
                manager.RefreshVisuals(this);
            }
        }

        public static Vector3Int Add(Vector3Int a, Vector3Int b) => HexMath.Add(a, b);
        public static Vector3Int Subtract(Vector3Int a, Vector3Int b) => HexMath.Subtract(a, b);
        public static Vector3Int Scale(Vector3Int a, int k) => HexMath.Scale(a, k);
        public static Vector3Int Direction(int direction) => HexMath.Direction(direction);
        public static Vector3Int Neighbor(Vector3Int hex, int direction) => HexMath.Neighbor(hex, direction);
        public static int Distance(Vector3Int a, Vector3Int b) => HexMath.Distance(a, b);
        
        public Vector3Int Coordinates => new Vector3Int(Q, R, S);
    }
}
