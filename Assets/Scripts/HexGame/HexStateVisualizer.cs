using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace HexGame
{
    [ExecuteAlways]
    [RequireComponent(typeof(HexGridManager))]
    public class HexStateVisualizer : MonoBehaviour
    {
        [System.Serializable]
        public struct StateSetting
        {
            public HexState state;
            public int priority;
            public HexGridManager.RimSettings visuals;
        }

        private HexGridManager _gridManager;
        private HexGridManager gridManager
        {
            get
            {
                if (_gridManager == null) _gridManager = GetComponent<HexGridManager>();
                return _gridManager;
            }
        }

        [HideInInspector] 
        [SerializeField] public List<StateSetting> stateSettings = new List<StateSetting>();

        private void OnValidate()
        {
            if (stateSettings == null) stateSettings = new List<StateSetting>();

            // Auto-populate missing states from enum
            foreach (HexState state in System.Enum.GetValues(typeof(HexState)))
            {
                if (!stateSettings.Any(s => s.state == state))
                {
                    stateSettings.Add(new StateSetting 
                    { 
                        state = state, 
                        priority = (int)state * 10, 
                        visuals = new HexGridManager.RimSettings { color = Color.black, width = 0f } 
                    });
                }
            }

            SyncMaterialWithDefault();

            // Refresh the grid visuals immediately when settings change in the inspector
            // This works in both Editor and Play Mode.
            if (gridManager != null)
            {
                gridManager.RefreshVisuals();
            }
        }

        private void SyncMaterialWithDefault()
        {
            if (gridManager == null || gridManager.hexSurfaceMaterial == null) return;
            
            var defaultSetting = stateSettings.FirstOrDefault(s => s.state == HexState.Default);
            
            // Update shared material properties
            gridManager.hexSurfaceMaterial.SetColor("_RimColor", defaultSetting.visuals.color);
            gridManager.hexSurfaceMaterial.SetFloat("_RimWidth", defaultSetting.visuals.width);
            gridManager.hexSurfaceMaterial.SetFloat("_RimPulsationSpeed", defaultSetting.visuals.pulsation);
            
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(gridManager.hexSurfaceMaterial);
            }
            #endif
        }

        public void RefreshVisuals(Hex hex)
        {
            if (hex == null || hex.Data == null || gridManager == null || stateSettings == null || hex.Data.States == null) return;

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
                // Fallback to Default state configuration
                bestSetting = stateSettings.FirstOrDefault(s => s.state == HexState.Default);
            }

            gridManager.SetHexRim(hex, bestSetting.visuals);
        }
        
        public HexGridManager.RimSettings GetDefaultRimSettings()
        {
            if (stateSettings == null || stateSettings.Count == 0) 
                return new HexGridManager.RimSettings { color = Color.black, width = 0f };

            return stateSettings.FirstOrDefault(s => s.state == HexState.Default).visuals;
        }
    }
}