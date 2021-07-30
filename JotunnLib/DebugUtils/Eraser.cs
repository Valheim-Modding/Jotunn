using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace Jotunn.DebugUtils
{
    internal class Eraser : MonoBehaviour
    {
        private ConfigEntry<bool> _isModEnabled;
        private ConfigEntry<KeyCode> _deleteObjectConfig;
        private ButtonConfig _deleteObjectButton;

        private void Awake()
        {
            _isModEnabled = Main.Instance.Config.Bind<bool>(nameof(Eraser), "Enabled", false, "Globally enable or disable the prefab eraser.");
            _deleteObjectConfig = Main.Instance.Config.Bind<KeyCode>(nameof(Eraser), "KeyCode", KeyCode.KeypadMinus, "Key for the prefab eraser.");
            _deleteObjectButton = new ButtonConfig
            {
                Name = "DebugDeleteObject",
                Config = _deleteObjectConfig
            };
            InputManager.Instance.AddButton(Main.ModGuid, _deleteObjectButton);

            HoverActionsPanel.OnHoverChanged += (GameObject hover) =>
            {
                if (!_isModEnabled.Value)
                {
                    return;
                }
                GenerateDeleteObjectText(hover);
            };

            On.Player.Update += PlayerUpdatePostfix;
        }

        private void GenerateDeleteObjectText(GameObject hoverObject)
        {
            if (!hoverObject)
            {
                return;
            }

            HoverActionsPanel.AddRow(
                $"[<color=#f44336>{_deleteObjectConfig.Value}</color>]",
                $"[Delete this object <color=#f44336>permanently</color>]");
        }

        private void PlayerUpdatePostfix(On.Player.orig_Update orig, Player self)
        {
            orig(self);

            if (!_isModEnabled.Value
                || self != Player.m_localPlayer
                || !self.m_hovering
                || !ZInput.GetButtonDown(_deleteObjectButton.Name))
            {
                return;
            }

            DeleteHoverObject(self);
        }

        private void DeleteHoverObject(Player player)
        {
            if (!player || !player.m_hovering)
            {
                return;
            }

            ZNetView zNetView = player.m_hovering.GetComponentInParent<ZNetView>();

            if (!zNetView || !zNetView.IsValid())
            {
                return;
            }

            UnityEngine.Object.Instantiate<GameObject>(
                ZNetScene.instance.GetPrefab("fx_guardstone_permitted_removed"),
                player.m_hovering.transform.position,
                player.m_hovering.transform.rotation);

            Logger.LogInfo(string.Format("Deleted {0} (uid: {1})", player.m_hovering.name, zNetView.GetZDO().m_uid));

            zNetView.GetZDO().SetOwner(ZDOMan.instance.GetMyID());
            ZNetScene.instance.Destroy(zNetView.gameObject);
        }
    }
}
