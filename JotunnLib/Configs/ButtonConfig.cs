using BepInEx.Configuration;
using UnityEngine;
using static Jotunn.Managers.InputManager;

namespace Jotunn.Configs
{
    /// <summary>
    ///     Configuration class for adding custom inputs and custom key hints.<br />
    ///     See <a href="https://docs.unity3d.com/2019.4/Documentation/ScriptReference/Input.html" />
    ///     for more information on Unity Input handling.
    /// </summary>
    public class ButtonConfig
    {
        /// <summary>
        ///     Name of the config. Use this to react to the button press bound by this config.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Axis string this config should be bound to.<br />
        ///     Use special Axis "Mouse ScrollWheel" to display the scroll icon as the key hint.
        /// </summary>
        public string Axis { get; set; } = string.Empty;

        /// <summary>
        ///     Private store for the Key property
        /// </summary>
        private KeyCode _key = KeyCode.None;

        /// <summary>
        ///     Unity KeyCode this config should be bound to.
        /// </summary>
        public KeyCode Key
        {
            get
            {
                if (_key != KeyCode.None)
                {
                    return _key;
                }

                return Config?.Value ?? KeyCode.None;
            }
            set
            {
                GamepadButton gamepadButton = GetGamepadButton(value);

                if (gamepadButton != GamepadButton.None)
                {
                    Logger.LogWarning($"ButtonConfig: Key {value} is a GamepadButton, setting GamepadButton instead");
                    GamepadButton = gamepadButton;
                }
                else
                {
                    _key = value;
                }
            }
        }

        /// <summary>
        ///     BepInEx configuration entry of a KeyCode that should be used.
        ///     Overrides the <see cref="Key"/> value of this config.
        /// </summary>
        public ConfigEntry<KeyCode> Config { get; set; }

        /// <summary>
        ///     Private store for the shortcut
        /// </summary>
        private KeyboardShortcut _shortcut = KeyboardShortcut.Empty;

        /// <summary>
        ///     BepInEx KeyboardShortcut this config should be bound to.
        /// </summary>
        public KeyboardShortcut Shortcut
        {
            get
            {
                if (_shortcut.MainKey != KeyCode.None)
                {
                    return _shortcut;
                }

                return ShortcutConfig?.Value ?? KeyboardShortcut.Empty;
            }
            set
            {
                GamepadButton gamepadButton = GetGamepadButton(value.MainKey);

                if (gamepadButton != GamepadButton.None)
                {
                    Logger.LogWarning($"ButtonConfig: Shortcut {value.MainKey} is a GamepadButton, setting GamepadButton instead");
                    GamepadButton = gamepadButton;
                }
                else
                {
                    _shortcut = value;
                }
            }
        }

        /// <summary>
        ///     BepInEx configuration entry of a KeyCode that should be used.
        ///     Overrides the <see cref="Shortcut"/> value of this config.
        /// </summary>
        public ConfigEntry<KeyboardShortcut> ShortcutConfig { get; set; }

        /// <summary>
        ///     Private store for the GamepadButton property
        /// </summary>
        private GamepadButton _gamepadButton = GamepadButton.None;

        /// <summary>
        ///     GamepadButton this config should be bound to for gamepads.
        /// </summary>
        public GamepadButton GamepadButton
        {
            get
            {
                if (_gamepadButton != GamepadButton.None)
                {
                    return _gamepadButton;
                }

                return GamepadConfig?.Value ?? GamepadButton.None;
            }
            set
            {
                _gamepadButton = value;
            }
        }

        /// <summary>
        ///     BepInEx configuration entry of a GamepadButton that should be used.
        ///     Overrides the <see cref="GamepadButton"/> value of this config.
        /// </summary>
        public ConfigEntry<GamepadButton> GamepadConfig { get; set; }

        /// <summary>
        ///     Should the Axis value be inverted?
        /// </summary>
        public bool Inverted { get; set; } = false;

        /// <summary>
        ///     Delay until a constantly pressed key is considered "pressed" again.
        /// </summary>
        public float RepeatDelay { get; set; } = 0.0f;

        /// <summary>
        ///     Interval in which the check timer for the repeat delay is decremented.
        /// </summary>
        public float RepeatInterval { get; set; } = 0.0f;
        
        /// <summary>
        ///     Key hint text, overrides HintToken when set
        /// </summary>
        public string Hint { get; set; } = null;

        /// <summary>
        ///     Token for translating the key hint text.
        /// </summary>
        public string HintToken { get; set; } = null;

        /// <summary>
        ///     Should this button react on key presses when a Valheim GUI is open? Defaults to <c>false</c>.
        /// </summary>
        public bool ActiveInGUI { get; set; } = false;

        /// <summary>
        ///     Should this button react on key presses when a custom GUI is open and requested to block input? Defaults to <c>false</c>.
        /// </summary>
        public bool ActiveInCustomGUI { get; set; } = false;

        /// <summary>
        ///     Should this button block all other inputs using the same key or button? Defaults to <c>false</c>.<br/>
        ///     <b>Warning:</b> If set to <c>true</c>, all other input using the same key or axis is reset when queried via ZInput.
        ///     Make sure to gate your usage properly.
        /// </summary>
        public bool BlockOtherInputs { get; set; } = false;

        /// <summary>
        ///     Internal flag if this button config is backed by any BepInEx ConfigEntry
        /// </summary>
        internal bool IsConfigBacked => Config != null || ShortcutConfig != null || GamepadConfig != null;
    }
}
