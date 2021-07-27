using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace Jotunn.DebugUtils
{
    internal class FreeFly : MonoBehaviour
    {
        private ConfigEntry<bool> _isModEnabled;
        private ConfigEntry<KeyCode> _freeFlyToggleConfig;
        private ButtonConfig _freeFlyToggleButton;
        private ConfigEntry<float> _freeFlySmoothness;

        private void Awake()
        {
            _isModEnabled = Main.Instance.Config.Bind(nameof(FreeFly), "Enabled", true, "Globally enable or disable FreeFly.");
            _freeFlyToggleConfig = Main.Instance.Config.Bind<KeyCode>(nameof(FreeFly), "KeyCode", KeyCode.KeypadPlus, "Key to toggle FreeFly.");
            _freeFlyToggleButton = new ButtonConfig
            {
                Name = "DebugFreeFly",
                Config = _freeFlyToggleConfig
            };
            InputManager.Instance.AddButton(Main.ModGuid, _freeFlyToggleButton);
            _freeFlySmoothness = Main.Instance.Config.Bind<float>(nameof(FreeFly), "Smoothness", 0f,
                new ConfigDescription("Freefly camera smoothness.", new AcceptableValueRange<float>(0f, 2f)));

            _freeFlySmoothness.SettingChanged += (sender, eventArgs) => SetFreeFlySmoothness();

            On.Player.TakeInput += PlayerTakeInputPrefix;
        }

        private bool PlayerTakeInputPrefix(On.Player.orig_TakeInput orig, Player self)
        {
            if (!_isModEnabled.Value || !ZInput.GetButtonDown(_freeFlyToggleButton.Name) || GameCamera.instance == null)
            {
                return orig(self);
            }

            ToggleFreeFly();
            return false;
        }

        private void ToggleFreeFly()
        {
            GameCamera.instance.ToggleFreeFly();
            SetFreeFlySmoothness();

            MessageHud.instance.ShowMessage(
                MessageHud.MessageType.Center, $"FreeFly camera: {(GameCamera.instance.m_freeFly ? "on" : "off")}");

            Logger.LogInfo($"Set GameCamera.instance.m_freeFly to: {GameCamera.instance.m_freeFly}");
        }

        private void SetFreeFlySmoothness()
        {
            if (GameCamera.instance != null)
            {
                GameCamera.instance.m_freeFlySmooth = _freeFlySmoothness.Value;
            }
        }
    }
}
