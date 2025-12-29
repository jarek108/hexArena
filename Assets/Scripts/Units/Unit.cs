using UnityEngine;
using System.Collections.Generic;
using HexGame.Units;
using System.IO;

namespace HexGame
{
    [ExecuteAlways]
    public class Unit : MonoBehaviour
    {
        public int Id => gameObject.GetInstanceID();

        public int teamId;
        [SerializeField] private int typeIndex;

        public UnitSet unitSet => UnitManager.Instance?.ActiveUnitSet;

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
                var set = unitSet;
                if (set != null && typeIndex >= 0 && typeIndex < set.units.Count)
                    return set.units[typeIndex];
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
            Initialize();
            if (CurrentHex != null) SetHex(CurrentHex);
        }

        private void Start()
        {
            Initialize();
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

            // Logical "unoccupy" from start hex while in transit to prevent collisions
            if (CurrentHex != null && CurrentHex.Data.Unit == this)
            {
                CurrentHex.Data.Unit = null;
            }

            // Determine stop index from ruleset
            int stopIndex = ruleset != null ? ruleset.GetMoveStopIndex(this, path) : path.Count;

            // path[0] is current position usually
            for (int i = 1; i < stopIndex; i++)
            {
                // 1. Departure Check
                if (ruleset != null && CurrentHex != null)
                {
                    if (!ruleset.OnDeparture(this, CurrentHex.Data)) break;
                }

                HexData targetData = path[i];
                var manager = GridVisualizationManager.Instance;
                Hex targetHex = manager.GetHex(targetData.Q, targetData.R);

                Debug.Log($"[Unit] Moving to step {i}/{stopIndex-1}: {targetData.Q},{targetData.R}");

                if (targetHex != null)
                {
                    Vector3 startPos = transform.position;
                    Vector3 endPos = targetHex.transform.position;
                    endPos.y += currentView != null ? currentView.yOffset : 0;

                    float t = 0;
                    while (t < 1f)
                    {
                        t += Time.deltaTime * speed;
                        transform.position = Vector3.Lerp(startPos, endPos, t);
                        yield return null;
                    }
                    transform.position = endPos;

                    // 2. Update Reference (Traversal)
                    CurrentHex = targetHex;
                    lastQ = CurrentHex.Q;
                    lastR = CurrentHex.R;

                    // 3. Entry Check
                    if (ruleset != null)
                    {
                        if (!ruleset.OnEntry(this, CurrentHex.Data)) break;
                    }

                    if (pause > 0) yield return new WaitForSeconds(pause);
                }
            }

            // Logical "re-occupy" the final landing hex
            if (CurrentHex != null)
            {
                if (CurrentHex.Data.Unit == null)
                {
                    CurrentHex.Data.Unit = this;
                }
                else if (CurrentHex.Data.Unit != this)
                {
                    Debug.LogWarning($"[Unit] {UnitName} ended move on occupied hex {CurrentHex.Q},{CurrentHex.R}!");
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


        public void Initialize(int index, int team)
        {
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

            gameObject.name = $"{UnitName}_{Id}";
            
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
            // Only clear the old hex if WE are the one logically occupying it.
            if (CurrentHex != null && CurrentHex.Data.Unit == this)
            {
                CurrentHex.Data.Unit = null;
            }

            var ruleset = GameMaster.Instance?.ruleset;
            if (CurrentHex != null && ruleset != null)
            {
                ruleset.OnDeparture(this, CurrentHex.Data);
            }

            CurrentHex = hex;

            if (CurrentHex != null)
            {
                // Only claim the new hex if it's empty.
                if (CurrentHex.Data.Unit == null)
                {
                    CurrentHex.Data.Unit = this;
                }

                if (ruleset != null)
                {
                    ruleset.OnEntry(this, CurrentHex.Data);
                }

                transform.position = CurrentHex.transform.position + new Vector3(0, currentView != null ? currentView.yOffset : 0, 0);
                lastQ = CurrentHex.Q;
                lastR = CurrentHex.R;
            }
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