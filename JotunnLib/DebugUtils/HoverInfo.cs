using System.Text;
using BepInEx.Configuration;
using UnityEngine;

namespace Jotunn.DebugUtils
{
    internal class HoverInfo : MonoBehaviour
    {
        private ConfigEntry<bool> _isModEnabled;

        private void Awake()
        {
            _isModEnabled = Main.Instance.Config.Bind<bool>(nameof(HoverInfo), "Enabled", false, "Globally enable or disable the hover info.");
            HoverActionsPanel.OnHoverChanged += (GameObject hover) =>
            {
                if (!_isModEnabled.Value)
                {
                    return;
                }
                GenerateHoverInfoText(hover);
            };
        }

        private void GenerateHoverInfoText(GameObject hoverObject)
        {
            if (!hoverObject)
            {
                return;
            }

            StringBuilder sb = new StringBuilder();

            HoverActionsPanel.AddRow("test", "bla");
            HoverActionsPanel.AddRow("random", "info");
            HoverActionsPanel.AddRow("that:", "grows");
        }
    }
}
