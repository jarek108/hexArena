using UnityEngine;

namespace HexGame
{
    [ExecuteAlways]
    public class Hex : MonoBehaviour
    {
        public HexData Data { get; private set; }

        // Serialized backing fields for persistence (View memory)
        [SerializeField, HideInInspector] private int viewQ;
        [SerializeField, HideInInspector] private int viewR;
        [SerializeField, HideInInspector] private int viewS;
        [SerializeField, HideInInspector] private float viewElevation;
        [SerializeField, HideInInspector] private TerrainType viewTerrainType;

        private TerrainType? _previewTerrain;

        // Properties: Read from Data (Logic) if active, otherwise Backup (View)
        public int Q => Data != null ? Data.Q : viewQ;
        public int R => Data != null ? Data.R : viewR;
        public int S => Data != null ? Data.S : viewS;

        public float Elevation
        {
            get => Data != null ? Data.Elevation : viewElevation;
            set
            {
                if (Data != null) 
                {
                    Data.Elevation = value;
                }
                else
                {
                    viewElevation = value; 
                    UpdatePosition();
                }
            }
        }

        public TerrainType TerrainType
        {
            get 
            {
                if (_previewTerrain.HasValue) return _previewTerrain.Value;
                return Data != null ? Data.TerrainType : viewTerrainType;
            }
            set
            {
                if (Data != null)
                {
                    Data.TerrainType = value;
                }
                else
                {
                    viewTerrainType = value; 
                    UpdateVisuals();
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

        public Unit Unit
        {
            get => Data?.Unit;
            set
            {
                if (Data != null) Data.Unit = value;
            }
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

            // Sync View Memory with Data
            viewQ = data.Q;
            viewR = data.R;
            viewS = data.S;
            viewElevation = data.Elevation;
            viewTerrainType = data.TerrainType;

            name = $"Hex ({data.Q}, {data.R})";
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
            // Sync View Memory
            viewTerrainType = Data.TerrainType;
            UpdateVisuals();
        }

        private void HandleElevationChanged()
        {
            // Sync View Memory
            viewElevation = Data.Elevation;
            UpdatePosition();

            // Trigger unit repositioning if present
            if (Unit != null)
            {
                Unit.UpdateVisualPosition();
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
