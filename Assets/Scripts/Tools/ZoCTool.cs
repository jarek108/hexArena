using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace HexGame.Tools
{
    public class ZoCTool : ToggleTool
    {
        private void Start()
        {
            var manager = GridVisualizationManager.Instance ?? FindFirstObjectByType<GridVisualizationManager>();
            if (manager != null && manager.stateSettings != null)
            {
                var zoc = manager.stateSettings.FirstOrDefault(s => IsTeamZoC(s.state));
                if (!string.IsNullOrEmpty(zoc.state))
                {
                    isActive = zoc.priority > 0;
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
                if (IsTeamZoC(setting.state))
                {
                    int magnitude = Mathf.Max(1, Mathf.Abs(setting.priority));
                    setting.priority = newState ? magnitude : -magnitude;
                    manager.stateSettings[i] = setting;
                }
            }

            manager.RefreshVisuals();
        }

        private bool IsTeamZoC(string state)
        {
            return state.StartsWith("ZoC");
        }
    }
}
