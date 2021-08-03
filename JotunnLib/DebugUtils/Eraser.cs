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

        private TwoColumnPanel _eraserPanel;
        private Text _eraserLabel;
        private Text _eraserValue;

        private void Awake()
        {
            _isModEnabled = Main.Instance.Config.Bind<bool>(nameof(Eraser), "Enabled", false, "Globally enable or disable the prefab eraser.");
            _isModEnabled.SettingChanged += (sender, eventArgs) =>
            {
                DestroyPanel();

                if (_isModEnabled.Value && Hud.instance)
                {
                    CreatePanel(Hud.instance);
                }
            };

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

        private void CreatePanel(Hud hud)
        {
            _eraserPanel =
                new TwoColumnPanel(hud.m_crosshair.transform, hud.m_hoverName.font)
                    .SetPosition(new Vector2(0, 50))
                    .AddPanelRow(out _eraserLabel, out _eraserValue);
            
            _eraserLabel.text = $"[<color=#f44336>{_deleteObjectConfig.Value}</color>]";
            _eraserValue.text = $"[Delete this object <color=#f44336>permanently</color>]";
        }

        private void DestroyPanel()
        {
            _eraserPanel?.DestroyPanel();
        }

        private void HudAwakePostfix(On.Hud.orig_Awake orig, Hud self)
        {
            orig(self);

            if (_isModEnabled.Value)
            {
                CreatePanel(self);
            }
        }

        private void HudUpdateCrosshairPostfix(On.Hud.orig_UpdateCrosshair orig, Hud self, Player player, float bowDrawPercentage)
        {
            orig(self, player, bowDrawPercentage);

            if (_isModEnabled.Value)
            {
                GameObject hover = GetValidHoverObject(player);
                _eraserPanel.SetActive(hover);
            }
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
