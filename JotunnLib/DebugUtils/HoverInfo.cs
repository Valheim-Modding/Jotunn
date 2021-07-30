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

            HoverActionsPanel.AddRow("Name", hoverObject.name);

            if (hoverObject.TryGetComponent<ZNetView>(out var znet))
            {
                HoverActionsPanel.AddRow("IsOwner", znet.IsOwner().ToString());
            }

            if (hoverObject.TryGetComponent<Piece>(out var piece))
            {
                HoverActionsPanel.AddRow("Is", "Piece");
            }
        }
    }
}
