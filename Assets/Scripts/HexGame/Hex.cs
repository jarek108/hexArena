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
                viewElevation = value; // Keep view in sync
                UpdatePosition();
            }
        }

        public TerrainType TerrainType
        {
            get => Data != null ? Data.TerrainType : viewTerrainType;
            set
            {
                if (Data != null)
                {
                    Data.TerrainType = value;
                }
                viewTerrainType = value; // Keep view in sync
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
            }

            Data = data;
            
            if (Data != null)
            {
                Data.OnStateChanged += HandleStateChanged;
                Data.OnTerrainChanged += HandleTerrainChanged;
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
            }
        }

        private void OnDisable()
        {
            if (Data != null)
            {
                Data.OnStateChanged -= HandleStateChanged;
                Data.OnTerrainChanged -= HandleTerrainChanged;
            }
        }

        public void HandleStateChanged()
        {
            var visualizer = FindFirstObjectByType<HexStateVisualizer>();
            if (visualizer != null)
            {
                visualizer.RefreshVisuals(this);
            }
        }

        private void HandleTerrainChanged()
        {
            // Sync View Memory
            viewTerrainType = Data.TerrainType;
            UpdateVisuals();
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
            HexGridManager manager = FindFirstObjectByType<HexGridManager>();
            if (manager != null && GetComponent<Renderer>() != null)
            {
                manager.SetHexColor(this, manager.GetDefaultHexColor(this));
            }
        }

        // Static directions for neighbors (pointy-top orientation)
        private static readonly Vector3Int[] directions =
        {
            new Vector3Int(1, 0, -1), new Vector3Int(1, -1, 0), new Vector3Int(0, -1, 1),
            new Vector3Int(-1, 0, 1), new Vector3Int(-1, 1, 0), new Vector3Int(0, 1, -1)
        };

        public static Vector3Int Add(Vector3Int a, Vector3Int b)
        {
            return a + b;
        }

        public static Vector3Int Subtract(Vector3Int a, Vector3Int b)
        {
            return a - b;
        }
        
        public static Vector3Int Scale(Vector3Int a, int k)
        {
            return a * k;
        }

        public static Vector3Int Direction(int direction)
        {
            return directions[direction % 6]; 
        }

        public static Vector3Int Neighbor(Vector3Int hex, int direction)
        {
            return Add(hex, Direction(direction));
        }

        public static int Distance(Vector3Int a, Vector3Int b)
        {
            return (Mathf.Abs(a.x - b.x)
                  + Mathf.Abs(a.y - b.y)
                  + Mathf.Abs(a.z - b.z)) / 2;
        }
        
        public Vector3Int Coordinates => new Vector3Int(Q, R, S);
    }
}