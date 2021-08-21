using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace Jotunn.DebugUtils
{
    internal class HoverInfo : MonoBehaviour
    {
        private ConfigEntry<bool> _isModEnabled;
        private ConfigEntry<Vector2> _hoverPiecePanelPosition;
        private ConfigEntry<Vector2> _placementGhostPanelPosition;

        private readonly Gradient _percentGradient = CreatePercentGradient();
        private readonly int _healthHashCode = "health".GetStableHashCode();

        private TwoColumnPanel _hoverPiecePanel;
        private Text _pieceNameTextLabel;
        private Text _pieceNameTextValue;
        private Text _pieceHealthTextLabel;
        private Text _pieceHealthTextValue;
        private Text _pieceStabilityTextLabel;
        private Text _pieceStabilityTextValue;
        private Text _pieceEulerTextLabel;
        private Text _pieceEulerTextValue;
        private Text _pieceRotationTextLabel;
        private Text _pieceRotationTextValue;

        private TwoColumnPanel _placementGhostPanel;
        private Text _ghostNameTextLabel;
        private Text _ghostNameTextValue;
        private Text _ghostEulerTextLabel;
        private Text _ghostEulerTextValue;
        private Text _ghostRotationTextLabel;
        private Text _ghostRotationTextValue;

        private void Awake()
        {
            _isModEnabled = Main.Instance.Config.Bind(nameof(HoverInfo), "Enabled", true, "Globally enable or disable the hover info.");

            _isModEnabled.SettingChanged += (sender, eventArgs) =>
            {
                DestroyPanels();

                if (_isModEnabled.Value && Hud.instance)
                {
                    CreatePanels(Hud.instance);
                }
            };

            _hoverPiecePanelPosition =
                Main.Instance.Config.Bind(
                    nameof(HoverInfo),
                    "hoverPiecePanelPosition",
                    new Vector2(-100, 200),
                    "Position of the HoverPiece properties panel.");

            _hoverPiecePanelPosition.SettingChanged +=
                (sender, eventArgs) => _hoverPiecePanel.SetPosition(_hoverPiecePanelPosition.Value);

            _placementGhostPanelPosition =
                Main.Instance.Config.Bind(
                    nameof(HoverInfo),
                    "placementGhostPanelPosition",
                    new Vector2(100, 15),
                    "Position of the PlacementGhost properties panel.");

            _placementGhostPanelPosition.SettingChanged +=
                (sender, eventArgs) => _placementGhostPanel.SetPosition(_placementGhostPanelPosition.Value);

            On.Hud.Awake += HudAwakePostfix;
            On.Hud.UpdateCrosshair += HudUpdateCrosshairPostfix;
        }

        private void HudAwakePostfix(On.Hud.orig_Awake orig, Hud self)
        {
            orig(self);

            if (_isModEnabled.Value)
            {
                CreatePanels(self);
            }
        }

        private void HudUpdateCrosshairPostfix(On.Hud.orig_UpdateCrosshair orig, Hud self, Player player, float bowDrawPercentage)
        {
            orig(self, player, bowDrawPercentage);

            if (_isModEnabled.Value)
            {
                UpdateHoverPieceProperties(player.m_hoveringPiece);
                UpdatePlacementGhostProperties(player.m_placementGhost);

                self.m_pieceHealthRoot.gameObject.SetActive(false);
            }
        }

        private void CreatePanels(Hud hud)
        {
            _hoverPiecePanel =
                new TwoColumnPanel(hud.m_crosshair.transform, hud.m_hoverName.font)
                    .SetPosition(_hoverPiecePanelPosition.Value)
                    .AddPanelRow(out _pieceNameTextLabel, out _pieceNameTextValue)
                    .AddPanelRow(out _pieceHealthTextLabel, out _pieceHealthTextValue)
                    .AddPanelRow(out _pieceStabilityTextLabel, out _pieceStabilityTextValue)
                    .AddPanelRow(out _pieceRotationTextLabel, out _pieceRotationTextValue)
                    .AddPanelRow(out _pieceEulerTextLabel, out _pieceEulerTextValue);

            _pieceNameTextLabel.text = "Piece \u25c8";
            _pieceHealthTextLabel.text = "Health \u2661";
            _pieceStabilityTextLabel.text = "Stability \u2616";
            _pieceRotationTextLabel.text = "Rotation \u2318";
            _pieceEulerTextLabel.text = "Euler \u29bf";

            _placementGhostPanel =
                new TwoColumnPanel(hud.m_crosshair.transform, hud.m_hoverName.font)
                    .SetPosition(_placementGhostPanelPosition.Value)
                    .AddPanelRow(out _ghostNameTextLabel, out _ghostNameTextValue)
                    .AddPanelRow(out _ghostRotationTextLabel, out _ghostRotationTextValue)
                    .AddPanelRow(out _ghostEulerTextLabel, out _ghostEulerTextValue);

            _ghostNameTextLabel.text = "Placing \u25a5";
            _ghostRotationTextLabel.text = "Rotation \u2318";
            _ghostEulerTextLabel.text = "Euler \u29bf";
        }

        private void DestroyPanels()
        {
            _hoverPiecePanel?.DestroyPanel();
            _placementGhostPanel?.DestroyPanel();
        }

        private void UpdateHoverPieceProperties(Piece piece)
        {
            if (!piece || !piece.TryGetComponent(out WearNTear wearNTear))
            {
                _hoverPiecePanel.SetActive(false);
                return;
            }

            _hoverPiecePanel.SetActive(true);

            _pieceNameTextValue.text = $"<color=#FFCA28>{Localization.instance.Localize(piece.m_name)}</color>";

            float health = wearNTear.m_nview.m_zdo.GetFloat(_healthHashCode, wearNTear.m_health);
            float healthPercent = Mathf.Clamp01(health / wearNTear.m_health);

            _pieceHealthTextValue.text =
                string.Format(
                    "<color={0}>{1:N0}</color> /<color={2}>{3}</color> (<color=#{4}>{5:P0}</color>)",
                    "#9CCC65",
                    health,
                    "#FAFAFA",
                    wearNTear.m_health,
                    ColorUtility.ToHtmlStringRGB(_percentGradient.Evaluate(healthPercent)),
                    healthPercent);

            float support = wearNTear.GetSupport();
            float maxSupport = wearNTear.GetMaxSupport();
            float supportPrecent = Mathf.Clamp01(support / maxSupport);

            _pieceStabilityTextValue.text =
                string.Format(
                  "<color={0}>{1:N0}</color> /<color={2}>{3}</color> (<color=#{4}>{5:P0}</color>)",
                  "#4FC3F7",
                  support,
                  "#FAFAFA",
                  maxSupport,
                  ColorUtility.ToHtmlStringRGB(_percentGradient.Evaluate(supportPrecent)),
                  supportPrecent);
            
            _pieceEulerTextValue.text = $"{wearNTear.transform.rotation.eulerAngles}";
            _pieceRotationTextValue.text = $"{wearNTear.transform.rotation}";
        }

        private void UpdatePlacementGhostProperties(GameObject placementGhost)
        {
            if (!placementGhost || !placementGhost.TryGetComponent(out Piece piece))
            {
                _placementGhostPanel.SetActive(false);
                return;
            }

            _placementGhostPanel.SetActive(true);

            _ghostNameTextValue.text = $"<color=#FFCA28>{Localization.instance.Localize(piece.m_name)}</color>";
            _ghostEulerTextValue.text = $"{placementGhost.transform.rotation.eulerAngles}";
            _ghostRotationTextValue.text = $"{placementGhost.transform.rotation}";
        }

        private static Gradient CreatePercentGradient()
        {
            Gradient gradient = new Gradient();

            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color32(239, 83, 80, 255), 0f),
                    new GradientColorKey(new Color32(255, 238, 88, 255), 0.5f),
                    new GradientColorKey(new Color32(156, 204, 101, 255), 1f),
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f),
                });

            return gradient;
        }
    }
}
