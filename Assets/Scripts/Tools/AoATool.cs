using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace HexGame.Tools
{
    public class AoATool : ToggleTool
    {
        private void OnEnable()
        {
            SyncState();
        }

        private void SyncState()
        {
            var manager = GridVisualizationManager.Instance ?? FindFirstObjectByType<GridVisualizationManager>();
            if (manager != null && manager.stateSettings != null)
            {
                var aoa = manager.stateSettings.FirstOrDefault(s => IsTeamAoA(s.state));
                if (!string.IsNullOrEmpty(aoa.state))
                {
                    isActive = aoa.priority > 0;
                }
            }
        }

        public override void OnToggle(bool newState)
        {
            var manager = GridVisualizationManager.Instance ?? FindFirstObjectByType<GridVisualizationManager>();
            if (manager == null) return;

            for (int i = 0; i < manager.stateSettings.Count; i++)
            {
                var setting = manager.stateSettings[i];
                if (IsTeamAoA(setting.state))
                {
                    int magnitude = Mathf.Max(1, Mathf.Abs(setting.priority));
                    setting.priority = newState ? magnitude : -magnitude;
                    manager.stateSettings[i] = setting;
                }
            }

            manager.RefreshVisuals();
        }

        private bool IsTeamAoA(string state)
        {
            // Matches AoA0, AoA1, etc. but not identity-fused ones like AoA0_123
            // (Wait, RefreshVisuals handles prefix matching, so we only need to toggle the template priorities)
            return state == "AoA0" || state == "AoA1";
        }
    }
}
