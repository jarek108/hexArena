using UnityEngine;
using System.Collections.Generic;
using HexGame.Units;
using System.IO;
using System.Linq;

namespace HexGame
{
    [ExecuteAlways]
    public class Unit : MonoBehaviour
    {
        [System.Serializable]
        public struct ProjectedHexState
        {
            public int q;
            public int r;
            public string state;
        }

        public int Id => gameObject.GetInstanceID();

        public int teamId;
        [SerializeField] private string unitTypeId;

        public UnitSet unitSet => UnitManager.Instance?.ActiveUnitSet;

        public string UnitTypeId
        {
            get => unitTypeId;
            set
            {
                unitTypeId = value;
                Initialize();
            }
        }

        public UnitType UnitType 
        {
            get 
            {
                var set = unitSet;
                if (set != null && !string.IsNullOrEmpty(unitTypeId))
                    return set.units.FirstOrDefault(u => u.id == unitTypeId);
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
        private Grid _cachedGrid;

        [SerializeField, HideInInspector] private List<ProjectedHexState> ownedHexStates = new List<ProjectedHexState>();

        private void OnValidate()
        {
            Initialize();
            if (CurrentHex != null) SetHex(CurrentHex);
        }

        private void Start()
        {
            Initialize();
        }

        private void OnDisable()
        {
            ClearOwnedHexStates();
        }

        public void AddOwnedHexState(HexData hex, string state)
        {
            if (hex == null) return;
            hex.AddState(state);
            ownedHexStates.Add(new ProjectedHexState { q = hex.Q, r = hex.R, state = state });
        }

        public void ClearOwnedHexStates()
        {
            var grid = GridVisualizationManager.Instance?.Grid ?? _cachedGrid;
            if (grid == null || ownedHexStates.Count == 0) return;

            foreach (var projection in ownedHexStates)
            {
                var hex = grid.GetHexAt(projection.q, projection.r);
                hex?.RemoveState(projection.state);
            }
            ownedHexStates.Clear();
        }

        public void RemoveOwnedHexStatesByPrefix(string prefix)
        {
            var grid = GridVisualizationManager.Instance?.Grid ?? _cachedGrid;
            if (grid == null || ownedHexStates.Count == 0) return;

            for (int i = ownedHexStates.Count - 1; i >= 0; i--)
            {
                if (ownedHexStates[i].state.StartsWith(prefix))
                {
                    var hex = grid.GetHexAt(ownedHexStates[i].q, ownedHexStates[i].r);
                    hex?.RemoveState(ownedHexStates[i].state);
                    ownedHexStates.RemoveAt(i);
                }
            }
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

            // Determine stop index from ruleset (which now handles budget)
            int stopIndex = ruleset != null ? ruleset.GetMoveStopIndex(this, path) : path.Count;

            // path[0] is current position usually
            for (int i = 1; i < stopIndex; i++)
            {
                HexData targetData = path[i];
                HexData previousData = CurrentHex != null ? CurrentHex.Data : null;

                // 1. Logic Check (Can we still make this step?)
                if (ruleset != null)
                {
                    var verification = ruleset.TryMoveStep(this, previousData, targetData);
                    if (!verification.isValid)
                    {
                        Debug.Log($"[Unit] Move interrupted: {verification.reason}");
                        break; 
                    }
                }

                var manager = GridVisualizationManager.Instance;
                Hex targetHex = manager.GetHex(targetData.Q, targetData.R);

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

                    // 2. Logic Execution
                    if (ruleset != null)
                    {
                        ruleset.PerformMove(this, previousData, targetData);
                    }

                    // 3. Update Visual/Reference Reference
                    CurrentHex = targetHex;
                    lastQ = CurrentHex.Q;
                    lastR = CurrentHex.R;

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


        public void Initialize(string typeId, int team)
        {
            unitTypeId = typeId;
            teamId = team;
            Initialize();
        }

        private void Initialize()
        {
            Stats.Clear();
            _cachedGrid = GridVisualizationManager.Instance?.Grid;

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
            if (_cachedGrid == null) _cachedGrid = GridVisualizationManager.Instance?.Grid;

            HexData previousData = CurrentHex != null ? CurrentHex.Data : null;
            if (previousData != null)
            {
                previousData.RemoveUnit(this);
            }

            // If leaving the grid (moving to null), clear our projected footprint.
            if (previousData != null && hex == null)
            {
                ClearOwnedHexStates();
            }

            var ruleset = GameMaster.Instance?.ruleset;

            CurrentHex = hex;

            if (CurrentHex != null)
            {
                if (ruleset != null)
                {
                    ruleset.PerformMove(this, previousData, CurrentHex.Data);
                }
                else
                {
                    // Fallback if no ruleset exists
                    CurrentHex.Data.AddUnit(this);
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
                unitTypeId = this.unitTypeId,
                teamId = this.teamId
            };
        }
    }

    [System.Serializable]
    public class UnitSaveData
    {
        public int q;
        public int r;
        public string unitTypeId;
        public int teamId;
    }
}