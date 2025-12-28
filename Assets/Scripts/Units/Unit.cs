using UnityEngine;
using System.Collections.Generic;
using HexGame.Units;

namespace HexGame
{
    [ExecuteAlways]
    public class Unit : MonoBehaviour
    {
        public int Id => gameObject.GetInstanceID();

        public UnitSet unitSet;
        [SerializeField] private int typeIndex;
        public int teamId;

        public int TypeIndex
        {
            get => typeIndex;
            set
            {
                typeIndex = value;
                Initialize();
            }
        }

        public UnitType UnitType 
        {
            get 
            {
                if (unitSet != null && typeIndex >= 0 && typeIndex < unitSet.units.Count)
                    return unitSet.units[typeIndex];
                return null;
            }
        }

        public string UnitName => UnitType != null ? UnitType.Name : "Unknown Unit";

        public Hex CurrentHex { get; private set; }
        [SerializeField, HideInInspector] private int lastQ;
        [SerializeField, HideInInspector] private int lastR;
        
        public Dictionary<string, int> Stats = new Dictionary<string, int>();
        private UnitVisualization currentView;
        private Coroutine moveCoroutine;

        private void OnValidate()
        {
            // Only initialize if we have a set and it's not in the middle of a prefab stage or similar
            if (unitSet != null)
            {
                Initialize();
                if (CurrentHex != null) SetHex(CurrentHex);
            }
        }

        private void Start()
        {
            if (unitSet != null) Initialize();
        }

        public bool IsMoving => moveCoroutine != null;

        public void MoveAlongPath(List<HexData> path, float speed, float pause, System.Action onComplete = null)
        {
            if (moveCoroutine != null) StopCoroutine(moveCoroutine);
            moveCoroutine = StartCoroutine(MoveCoroutine(path, speed, pause, onComplete));
        }

        private System.Collections.IEnumerator MoveCoroutine(List<HexData> path, float speed, float pause, System.Action onComplete)
        {
            var ruleset = GameMaster.Instance?.ruleset;

            // Determine stop index from ruleset
            int stopIndex = ruleset != null ? ruleset.GetMoveStopIndex(this, path) : path.Count;

            // path[0] is current position usually, so we skip it or handle it.
            for (int i = 1; i < stopIndex; i++)
            {
                HexData targetData = path[i];
                var manager = GridVisualizationManager.Instance;
                Hex targetHex = manager.GetHex(targetData.Q, targetData.R);

                Debug.Log($"[Unit] Moving to step {i}/{stopIndex-1}: {targetData.Q},{targetData.R}");

                if (targetHex != null)
                {
                    // Visual movement
                    Vector3 startPos = transform.position;
                    Vector3 endPos = targetHex.transform.position;
                    if (currentView != null) endPos.y += currentView.yOffset;

                    float journey = 0f;
                    float duration = Vector3.Distance(startPos, endPos) / speed;

                    while (journey < 1f && duration > 0)
                    {
                        journey += Time.deltaTime / duration;
                        transform.position = Vector3.Lerp(startPos, endPos, journey);
                        yield return null;
                    }
                    transform.position = endPos;

                    // Logical update (Entry/Departure rules)
                    SetHex(targetHex);

                    if (pause > 0) yield return new WaitForSeconds(pause);
                }
            }

            moveCoroutine = null;
            onComplete?.Invoke();
        }

        public void FacePosition(Vector3 targetPos)
        {
            Vector3 dir = targetPos - transform.position;
            dir.y = 0;
            if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);
        }


        public void Initialize(UnitSet set, int index, int team)
        {
            unitSet = set;
            typeIndex = index;
            teamId = team;
            Initialize();
        }

        private void Initialize()
        {
            Stats.Clear();

            UnitType type = UnitType;
            if (type != null && type.Stats != null)
            {
                foreach (var stat in type.Stats)
                {
                    Stats[stat.id] = stat.value;
                }
            }
            
            // Note: In the new flow, the Unit component is added TO the visualization instance.
            currentView = GetComponent<UnitVisualization>();
            if (currentView != null)
            {
                currentView.Initialize(this);
                currentView.SetPreviewIdentity(UnitName);
            }
        }

        public void SetHex(Hex hex)
        {
            // 1. Departure from old hex
            if (CurrentHex != null)
            {
                GameMaster.Instance?.ruleset?.OnDeparture(this, CurrentHex.Data);
                // Only clear reference if we are actually changing to a new hex or null
                if (CurrentHex != hex) CurrentHex.Unit = null;
            }

            if (hex == null)
            {
                CurrentHex = null;
                return;
            }

            // 2. Entry to new hex
            CurrentHex = hex;
            CurrentHex.Unit = this;
            lastQ = hex.Q;
            lastR = hex.R;
            
            GameMaster.Instance?.ruleset?.OnEntry(this, hex.Data);
            
            UpdateVisualPosition();
        }

        public void UpdateVisualPosition()
        {
            if (CurrentHex == null) return;
            
            Vector3 pos = CurrentHex.transform.position;
            if (currentView != null) pos.y += currentView.yOffset;
            else
            {
                // Fallback: try to find view if it wasn't picked up
                currentView = GetComponent<UnitVisualization>();
                if (currentView != null) pos.y += currentView.yOffset;
            }
            transform.position = pos;
        }
        
        public int GetStat(string statName, int defaultValue = 0)
        {
            if (Stats.TryGetValue(statName, out int val)) return val;
            return defaultValue;
        }

        public UnitSaveData GetSaveData()
        {
            return new UnitSaveData
            {
                q = lastQ,
                r = lastR,
                typeIndex = this.typeIndex,
                teamId = this.teamId
            };
        }
    }

    [System.Serializable]
    public class UnitSaveData
    {
        public int q;
        public int r;
        public int typeIndex;
        public int teamId;
    }
}