using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using Jotunn.Configs;
using UnityEngine;

namespace Jotunn.Managers
{
    /// <summary>
    ///    Manager for handling custom inputs registered by mods.
    /// </summary>
    public class InputManager : IManager
    {
        public enum GamepadButton
        {
            None,
            DPadUp,
            DPadDown,
            DPadLeft,
            DPadRight,
            ButtonSouth,
            ButtonEast,
            ButtonWest,
            ButtonNorth,
            LeftShoulder,
            RightShoulder,
            LeftTrigger,
            RightTrigger,
            SelectButton,
            StartButton,
            LeftStickButton,
            RightStickButton
        }

        internal static KeyCode GetGamepadKeyCode(GamepadButton @enum)
        {
            return @enum switch
            {
                GamepadButton.ButtonSouth => KeyCode.JoystickButton0,
                GamepadButton.ButtonEast => KeyCode.JoystickButton1,
                GamepadButton.ButtonWest => KeyCode.JoystickButton2,
                GamepadButton.ButtonNorth => KeyCode.JoystickButton3,
                GamepadButton.LeftShoulder => KeyCode.JoystickButton4,
                GamepadButton.RightShoulder => KeyCode.JoystickButton5,
                GamepadButton.SelectButton => KeyCode.JoystickButton6,
                GamepadButton.StartButton => KeyCode.JoystickButton7,
                GamepadButton.LeftStickButton => KeyCode.JoystickButton8,
                GamepadButton.RightStickButton => KeyCode.JoystickButton9,
                _ => KeyCode.None
            };
        }

        internal static string GetGamepadAxis(GamepadButton @enum)
        {
            return @enum switch
            {
                GamepadButton.DPadUp => "JoyAxis 7",
                GamepadButton.DPadDown => "-JoyAxis 7",
                GamepadButton.DPadLeft => "-JoyAxis 6",
                GamepadButton.DPadRight => "JoyAxis 6",
                GamepadButton.LeftTrigger => "-JoyAxis 3",
                GamepadButton.RightTrigger => "JoyAxis 3",
                _ => string.Empty
            };
        }
        
        internal static string GetGamepadString(GamepadButton @enum)
        {
            return @enum switch
            {
                GamepadButton.None => string.Empty,
                GamepadButton.DPadUp => "up",
                GamepadButton.DPadDown => "down",
                GamepadButton.DPadLeft => "left",
                GamepadButton.DPadRight => "right",
                GamepadButton.ButtonNorth => "Y",
                GamepadButton.ButtonSouth => "A",
                GamepadButton.ButtonWest => "X",
                GamepadButton.ButtonEast => "B",
                GamepadButton.LeftShoulder => "LB",
                GamepadButton.RightShoulder => "RB",
                GamepadButton.LeftTrigger => "LT",
                GamepadButton.RightTrigger => "RT",
                GamepadButton.StartButton => "Start",
                GamepadButton.SelectButton => "Select",
                GamepadButton.LeftStickButton => "L",
                GamepadButton.RightStickButton => "R",
                _ => string.Empty
            };
        }

        internal static GamepadButton GetGamepadButton(string axis)
        {
            return axis switch
            {
                "JoyAxis 7" => GamepadButton.DPadUp,
                "-JoyAxis 7" => GamepadButton.DPadDown,
                "-JoyAxis 6" => GamepadButton.DPadLeft,
                "JoyAxis 6" => GamepadButton.DPadRight,
                "-JoyAxis 3" => GamepadButton.LeftTrigger,
                "JoyAxis 3" => GamepadButton.RightTrigger,
                _ => GamepadButton.None
            };
        }

        internal static GamepadButton GetGamepadButton(KeyCode key)
        {
            return key switch
            {
                KeyCode.JoystickButton0 => GamepadButton.ButtonSouth,
                KeyCode.JoystickButton1 => GamepadButton.ButtonEast,
                KeyCode.JoystickButton2 => GamepadButton.ButtonWest,
                KeyCode.JoystickButton3 => GamepadButton.ButtonNorth,
                KeyCode.JoystickButton4 => GamepadButton.LeftShoulder,
                KeyCode.JoystickButton5 => GamepadButton.RightShoulder,
                KeyCode.JoystickButton6 => GamepadButton.SelectButton,
                KeyCode.JoystickButton7 => GamepadButton.StartButton,
                KeyCode.JoystickButton8 => GamepadButton.LeftStickButton,
                KeyCode.JoystickButton9 => GamepadButton.RightStickButton,
                _ => GamepadButton.None
            };
        }
        
        // Internal holder for all buttons added via Jotunn
        internal static Dictionary<string, ButtonConfig> Buttons = new Dictionary<string, ButtonConfig>();

        internal static Dictionary<ConfigEntryBase, ButtonConfig> ButtonToConfigDict = new Dictionary<ConfigEntryBase, ButtonConfig>();

        private static InputManager _instance;
        /// <summary>
        ///     Singleton instance
        /// </summary>
        public static InputManager Instance
        {
            get
            {
                if (_instance == null) _instance = new InputManager();
                return _instance;
            }
        }

        /// <summary>
        ///     Initialize the manager
        /// </summary>
        public void Init()
        {
            // Dont init on a dedicated server
            if (!GUIManager.IsHeadless())
            {
                On.ZInput.Load += RegisterCustomInputs;
                On.ZInput.GetButtonDown += ZInput_GetButtonDown;
                On.ZInput.GetButton += ZInput_GetButton;
                On.ZInput.GetButtonUp += ZInput_GetButtonUp;
            }
        }

        /// <summary>
        ///     Add a Button to Valheim
        /// </summary>
        /// <param name="modGuid">Mod GUID</param>
        /// <param name="buttonConfig">Button config</param>
        public void AddButton(string modGuid, ButtonConfig buttonConfig)
        {
            if (buttonConfig == null)
            {
                throw new ArgumentNullException(nameof(buttonConfig));
            }

            if (string.IsNullOrEmpty(modGuid))
            {
                throw new ArgumentException($"{nameof(modGuid)} can not be empty or null", nameof(modGuid));
            }

            if (buttonConfig.Config == null && buttonConfig.Key == KeyCode.None &&
                buttonConfig.ShortcutConfig == null && buttonConfig.Shortcut.MainKey == KeyCode.None &&
                string.IsNullOrEmpty(buttonConfig.Axis))
            {
                throw new ArgumentException($"{nameof(buttonConfig)} needs either Axis, Key, Shortcut or a Config set.", nameof(buttonConfig));
            }

            if (Buttons.ContainsKey(buttonConfig.Name + "!" + modGuid))
            {
                Logger.LogWarning($"Cannot have duplicate button: {buttonConfig.Name} (Mod {modGuid})");
                return;
            }

            if (buttonConfig.Key != KeyCode.None && buttonConfig.Shortcut.MainKey != KeyCode.None)
            {
                Logger.LogWarning($"Cannot have both a Key and Shortcut in button config {buttonConfig.Name} (Mod {modGuid})");
                return;
            }

            if (buttonConfig.Config != null && buttonConfig.ShortcutConfig != null)
            {
                Logger.LogWarning($"Cannot have both a Key and Shortcut config in button config {buttonConfig.Name} (Mod {modGuid})");
                return;
            }

            if (buttonConfig.Config != null)
            {
                ButtonToConfigDict.Add(buttonConfig.Config, buttonConfig);
            }

            if (buttonConfig.ShortcutConfig != null)
            {
                ButtonToConfigDict.Add(buttonConfig.ShortcutConfig, buttonConfig);
            }

            if (buttonConfig.GamepadConfig != null)
            {
                ButtonToConfigDict.Add(buttonConfig.GamepadConfig, buttonConfig);
            }

            buttonConfig.Name += "!" + modGuid;
            Buttons.Add(buttonConfig.Name, buttonConfig);
        }

        private void RegisterCustomInputs(On.ZInput.orig_Load orig, ZInput self)
        {
            orig(self);

            if (Buttons.Any())
            {
                Logger.LogInfo($"Registering {Buttons.Count} custom inputs");

                foreach (var pair in Buttons)
                {
                    var btn = pair.Value;

                    if (!string.IsNullOrEmpty(btn.Axis))
                    {
                        self.AddButton(btn.Name, btn.Axis, btn.Inverted, btn.RepeatDelay, btn.RepeatInterval);
                    }
                    else if (btn.Key != KeyCode.None)
                    {
                        self.AddButton(btn.Name, btn.Key, btn.RepeatDelay, btn.RepeatInterval);
                    }
                    else if (btn.Shortcut.MainKey != KeyCode.None)
                    {
                        self.AddButton(btn.Name, btn.Shortcut.MainKey, btn.RepeatDelay, btn.RepeatInterval);
                    }

                    if (btn.GamepadButton != GamepadButton.None)
                    {
                        KeyCode keyCode = GetGamepadKeyCode(btn.GamepadButton);
                        string axis = GetGamepadAxis(btn.GamepadButton);

                        if (keyCode != KeyCode.None)
                        {
                            self.AddButton($"Joy!{btn.Name}", keyCode, btn.RepeatDelay, btn.RepeatInterval);
                        }

                        if (!string.IsNullOrEmpty(axis))
                        {
                            bool invert = axis.StartsWith("-");
                            self.AddButton($"Joy!{btn.Name}", axis.TrimStart('-'), invert, btn.RepeatDelay, btn.RepeatInterval);
                        }
                    }

                    Logger.LogDebug($"Registered input {pair.Key}");
                }
            }
        }

        private bool ZInput_GetButtonDown(On.ZInput.orig_GetButtonDown orig, string name)
        {
            if (!orig(name) && !orig($"Joy!{name}"))
            {
                return false;
            }

            if (!Buttons.TryGetValue(name, out var button))
            {
                return true;
            }

            if (button.Shortcut.MainKey != KeyCode.None && !button.Shortcut.IsDown())
            {
                return false;
            }

            return TakeInput(button);
        }

        private bool ZInput_GetButton(On.ZInput.orig_GetButton orig, string name)
        {
            if (!orig(name))
            {
                return false;
            }

            if (!Buttons.TryGetValue(name, out var button))
            {
                return true;
            }

            if (button.Shortcut.MainKey != KeyCode.None && !button.Shortcut.IsPressed())
            {
                return false;
            }

            return TakeInput(button);
        }

        private bool ZInput_GetButtonUp(On.ZInput.orig_GetButtonUp orig, string name)
        {
            if (!orig(name))
            {
                return false;
            }

            if (!Buttons.TryGetValue(name, out var button))
            {
                return true;
            }

            if (button.Shortcut.MainKey != KeyCode.None && !button.Shortcut.IsUp())
            {
                return false;
            }

            return TakeInput(button);
        }

        private bool TakeInput(ButtonConfig button)
        {
            if (Player.m_localPlayer == null)
            {
                return true;
            }

            if (button.ActiveInGUI && !GUIManager.InputBlocked)
            {
                return true;
            }

            if (button.ActiveInCustomGUI && GUIManager.InputBlocked)
            {
                return true;
            }

            return Player.m_localPlayer.TakeInput();
        }
    }
}
