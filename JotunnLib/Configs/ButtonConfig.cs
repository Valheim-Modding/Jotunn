using BepInEx.Configuration;
using Jotunn.Managers;
using UnityEngine;

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
                _key = value;
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
                _shortcut = value;
            }
        }

        /// <summary>
        ///     BepInEx configuration entry of a KeyCode that should be used.
        ///     Overrides the <see cref="Shortcut"/> value of this config.
        /// </summary>
        public ConfigEntry<KeyboardShortcut> ShortcutConfig { get; set; }

        /// <summary>
        ///     Private store for the Gamepad property
        /// </summary>
        private InputManager.GamepadButton _gamepad = InputManager.GamepadButton.None;

        /// <summary>
        ///     Unity KeyCode this config should be bound to for gamepads.
        /// </summary>
        public InputManager.GamepadButton Gamepad
        {
            get
            {
                if (_gamepad != InputManager.GamepadButton.None)
                {
                    return _gamepad;
                }

                return GamepadConfig?.Value ?? InputManager.GamepadButton.None;
            }
            set
            {
                _gamepad = value;
            }
        }

        /// <summary>
        ///     BepInEx configuration entry of a KeyCode that should be used.
        ///     Overrides the <see cref="Gamepad"/> value of this config.
        /// </summary>
        public ConfigEntry<InputManager.GamepadButton> GamepadConfig { get; set; }

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
    }
}
