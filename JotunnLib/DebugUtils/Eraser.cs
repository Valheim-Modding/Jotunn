using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace Jotunn.DebugUtils
{
    internal class Eraser : MonoBehaviour
    {
        private ConfigEntry<bool> _isModEnabled;
        private ConfigEntry<KeyCode> _deleteObjectConfig;
        private ButtonConfig _deleteObjectButton;

        private GameObject _hoverActionsPanel;
        private Text _deleteObjectText;
        private GameObject _lastHoverObject;

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

            On.Hud.Awake += HudAwakePostfix;
            On.Hud.UpdateCrosshair += HudUpdateCrosshairPostfix;
            On.Player.Update += PlayerUpdatePostfix;
        }

        private void HudAwakePostfix(On.Hud.orig_Awake orig, Hud self)
        {
            orig(self);

            _hoverActionsPanel = HoverActionsPanel.GetPanel(self);

            _deleteObjectText = Instantiate<Text>(self.m_hoverName, _hoverActionsPanel.transform);
            _deleteObjectText.text = string.Empty;
            _deleteObjectText.gameObject.SetActive(true);
        }

        private void HudUpdateCrosshairPostfix(On.Hud.orig_UpdateCrosshair orig, Hud self, Player player, float bowDrawPercentage)
        {
            orig(self, player, bowDrawPercentage);

            if (!_isModEnabled.Value)
            {
                return;
            }

            GameObject hoverObject = GetValidHoverObject(player);

            if (hoverObject != _lastHoverObject)
            {
                _deleteObjectText.text = GenerateDeleteObjectText(hoverObject);
                _lastHoverObject = hoverObject;
            }
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

        private GameObject GetValidHoverObject(Player player)
        {
            if (!player || !player.GetHoverObject())
            {
                return null;
            }

            GameObject hoverObject = player.GetHoverObject();
            ZNetView zNetView = hoverObject.GetComponentInParent<ZNetView>();

            if (!zNetView || !zNetView.IsValid())
            {
                return null;
            }

            return hoverObject;
        }

        private string GenerateDeleteObjectText(GameObject hoverObject)
        {
            if (!hoverObject)
            {
                return string.Empty;
            }

            return string.Format(
                "[<color={0}>{1}</color>] Delete this object <color={0}>permanently</color>",
                "#f44336",
                _deleteObjectConfig.Value.ToString());
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
