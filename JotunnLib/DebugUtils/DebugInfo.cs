using System;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Jotunn.DebugUtils
{
    internal class DebugInfo : MonoBehaviour
    {
        private static DebugInfo instance;
        private ConfigEntry<bool> Enabled;
        private ConfigEntry<Vector2> Position;

        private TwoColumnPanel Panel;
        
        // Env
        private Text _envTimeValue;

        // ZoneInfo
        private Text _zonePositionValue;
        private Text _zoneAltitudeValue;
        private Text _zoneSectorValue;
        private Text _zoneZdoCountValue;
        
        // Player
        private Text _debugModeValue;
        private Text _toolValue;
        private Text _hoverValue;

        // Keys
        private Text _keyValue;

        private void Awake()
        {
            instance = this;

            Enabled = Main.Instance.Config.Bind(nameof(DebugInfo), "Enabled", true, "Globally enable or disable the debug info panel.");

            Enabled.SettingChanged += (sender, eventArgs) =>
            {
                DestroyPanel();

                if (Enabled.Value && Hud.instance)
                {
                    CreatePanel(Hud.instance);
                }
            };

            Position =
                Main.Instance.Config.Bind(
                    nameof(DebugInfo),
                    "PanelPosition",
                    new Vector2(40f, 600f),
                    "Position of the DebugInfo panel, starting at the lower left corner.");

            Position.SettingChanged += (sender, eventArgs) => Panel?.SetPosition(Position.Value);

            Main.Harmony.PatchAll(typeof(Patches));
        }

        private static class Patches
        {
            [HarmonyPatch(typeof(Hud), nameof(Hud.Awake)), HarmonyPostfix]
            private static void Hud_Awake(Hud __instance) => instance.Hud_Awake(__instance);

            [HarmonyPatch(typeof(Hud), nameof(Hud.Update)), HarmonyPostfix]
            private static void Hud_Update(Hud __instance) => instance.FastUpdate(__instance);
        }

        private void Hud_Awake(Hud self)
        {
            if (Enabled.Value)
            {
                CreatePanel(self);
            }
        }
        
        private void CreatePanel(Hud hud)
        {
            Panel =
                new TwoColumnPanel(hud.m_rootObject.transform, hud.m_hoverName.font.sourceFontFile, new Color32(0, 0, 0, 96))
                    .SetAnchor(new Vector2(0f, 0f))
                    .SetPosition(Position.Value)
                    .AddPanelRow("Time", out _envTimeValue)
                    .AddPanelRow("Position", out _zonePositionValue)
                    .AddPanelRow("Altitude", out _zoneAltitudeValue)
                    .AddPanelRow("Sector", out _zoneSectorValue)
                    .AddPanelRow("ZDOs", out _zoneZdoCountValue)
                    .AddPanelRow("Tool", out _toolValue)
                    .AddPanelRow("Hover", out _hoverValue)
                    .AddPanelRow("Key", out _keyValue)
                    .AddPanelRow("DebugMode", out _debugModeValue);
            
            InvokeRepeating("LazyUpdate", 1.0f, 0.5f);
        }

        private void DestroyPanel()
        {
            Panel?.DestroyPanel();
            Panel = null;

            CancelInvoke("LazyUpdate");
        }

        private void FastUpdate(Hud self)
        {
            if (Panel == null)
            {
                return;
            }

            if (EnvMan.instance && ZNet.instance)
            {
                UpdateTime();
            }

            if (Player.m_localPlayer)
            {
                UpdatePlayer();
            }

            if (ZInput.instance != null)
            {
                UpdateKeys();
            }
        }

        private void LazyUpdate()
        {
            if (Player.m_localPlayer != null && ZoneSystem.instance != null)
            {
                UpdateZones();
            }
        }

        private void UpdateTime()
        {
            _envTimeValue.text = string.Empty;

            var limit = EnvMan.instance.m_dayLengthSec;
            var fraction = (ZNet.instance.GetTimeSeconds() % limit) / limit;
            var seconds = fraction * 3600 * 24;
            var hours = Math.Floor(seconds / 3600);
            var minutes = Math.Floor((seconds - hours * 3600) / 60);
            var time = hours.ToString().PadLeft(2, '0') + ":" + minutes.ToString().PadLeft(2, '0');

            _envTimeValue.text = $"<color=#ffe082>{time}</color>";
        }

        private void UpdatePlayer()
        {
            _debugModeValue.text = Player.m_debugMode ? "<color=#a5d6a7>enabled</color>" : "<color=#ff9999>disabled</color>";

            _toolValue.text = string.Empty;

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

        private void UpdateKeys()
        {
            var key = ZInput.instance.GetPressedKey(); 
            _keyValue.text = Enum.GetName(typeof(KeyCode), key);
        }

        private void UpdateZones()
        {
            Vector3 position = Player.m_localPlayer.transform.position;

            _zonePositionValue.text =
                    $"X <color=#ffe082>{position.x,-8:0}</color>\tZ <color=#a5d6a7>{position.z,-8:0}</color>\tY <color=#90caf9>{position.y,-8:0}</color>";

            float altitude = position.y - ZoneSystem.instance.m_waterLevel;

            _zoneAltitudeValue.text = $"<color=#ffe082>{altitude:0}</color>";

            Vector2i sector = ZoneSystem.instance.GetZone(position);

            _zoneSectorValue.text = $"<color=#ffe082>{sector.x}</color>, <color=#a5d6a7>{sector.y}</color>";

            int sectorIndex = ZDOMan.instance.SectorToIndex(sector);
            long zdoCount =
                sectorIndex >= 0 && ZDOMan.instance.m_objectsBySector[sectorIndex] != null
                    ? ZDOMan.instance.m_objectsBySector[sectorIndex].Count
                    : 0L;
            
            _zoneZdoCountValue.text = $"<color=#ffe082>{zdoCount}</color>";
        }
    }
}
