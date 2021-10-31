using System;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace Jotunn.DebugUtils
{
    internal class DebugInfo : MonoBehaviour
    {
        private ConfigEntry<bool> _isModEnabled;
        private ConfigEntry<Vector2> _debugPanelPosition;

        private TwoColumnPanel _debugPanel;
        private Text _toolLabel;
        private Text _toolValue;
        private Text _hoverLabel;
        private Text _hoverValue;
        private Text _keyLabel;
        private Text _keyValue;

        private void Awake()
        {
            _isModEnabled = Main.Instance.Config.Bind(nameof(DebugInfo), "Enabled", true, "Globally enable or disable the debug info panel.");

            _isModEnabled.SettingChanged += (sender, eventArgs) =>
            {
                DestroyPanel();

                if (_isModEnabled.Value && Hud.instance)
                {
                    CreatePanel(Hud.instance);
                }
            };

            _debugPanelPosition =
                Main.Instance.Config.Bind(
                    nameof(DebugInfo),
                    "DebugInfoPanelPosition",
                    new Vector2(40f, -300f),
                    "Position of the DebugInfo panel.");

            _debugPanelPosition.SettingChanged +=
                (sender, eventArgs) => _debugPanel.SetPosition(_debugPanelPosition.Value);
            
            On.Hud.Awake += HudAwakePostfix;
            On.Hud.Update += HudUpdatePostfix;
        }

        private void HudAwakePostfix(On.Hud.orig_Awake orig, Hud self)
        {
            orig(self);

            if (_isModEnabled.Value)
            {
                CreatePanel(self);
            }
        }
        
        private void HudUpdatePostfix(On.Hud.orig_Update orig, Hud self)
        {
            orig(self);

            if (_isModEnabled.Value)
            {
                UpdatePanel();
            }
        }

        private void CreatePanel(Hud hud)
        {
            _debugPanel =
                new TwoColumnPanel(hud.m_rootObject.transform, hud.m_hoverName.font)
                    .SetAnchor(new Vector2(0f, 1f))
                    .SetPosition(_debugPanelPosition.Value)
                    .AddPanelRow(out _toolLabel, out _toolValue)
                    .AddPanelRow(out _hoverLabel, out _hoverValue)
                    .AddPanelRow(out _keyLabel, out _keyValue);
            
            _toolLabel.text = "Tool";
            _hoverLabel.text = "Hover";
            _keyLabel.text = "Key";
        }

        private void DestroyPanel()
        {
            _debugPanel?.DestroyPanel();
        }

        private void UpdatePanel()
        {
            if (Player.m_localPlayer != null)
            {
                _toolValue.text = String.Empty;

                var item = Player.m_localPlayer.GetInventory().GetEquipedtems()
                    .FirstOrDefault(x => x.IsWeapon() || x.m_shared.m_buildPieces != null);
                if (item != null)
                {
                    if (item.m_dropPrefab)
                    {
                        _toolValue.text += item.m_dropPrefab.name;
                    }
                    else
                    {
                        _toolValue.text += item.m_shared.m_name;
                    }

                    Piece piece = Player.m_localPlayer.m_buildPieces?.GetSelectedPiece();
                    if (piece != null)
                    {
                        _toolValue.text += ":" + piece.name;
                    }
                }

                _hoverValue.text = string.Empty;

                var hover = Player.m_localPlayer.GetHoverObject();
                if (hover && hover.name != null)
                {
                    _hoverValue.text += hover.name;
                }
            }

            _keyValue.text = string.Empty;

            var key = ZInput.instance.GetPressedKey(); 
            _keyValue.text += Enum.GetName(typeof(KeyCode), key);

        }
    }
}
