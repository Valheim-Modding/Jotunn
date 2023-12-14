using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Utils;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace Jotunn.Managers
{
    /// <summary>
    ///    Manager for handling custom inputs registered by mods.
    /// </summary>
    public class InputManager : IManager
    {
        /// <summary>
        ///     Abstraction for gamepad buttons and axes used as inputs
        /// </summary>
        public enum GamepadButton
        {
            /// <summary>
            ///     No gamepad button, internally treated as null
            /// </summary>
            None,
            /// <summary>
            ///     Up direction on the directional pad
            /// </summary>
            DPadUp,
            /// <summary>
            ///     Down direction on the directional pad
            /// </summary>
            DPadDown,
            /// <summary>
            ///     Left direction on the directional pad
            /// </summary>
            DPadLeft,
            /// <summary>
            ///     Right direction on the directional pad
            /// </summary>
            DPadRight,
            /// <summary>
            ///     Southern button on the gamepad (A on XBox-like)
            /// </summary>
            ButtonSouth,
            /// <summary>
            ///     Eastern button on the gamepad (B on XBox-like)
            /// </summary>
            ButtonEast,
            /// <summary>
            ///     Western button on the gamepad (X on XBox-like)
            /// </summary>
            ButtonWest,
            /// <summary>
            ///     Nothern button on the gamepad (Y on XBox-like)
            /// </summary>
            ButtonNorth,
            /// <summary>
            ///     Left shoulder button
            /// </summary>
            LeftShoulder,
            /// <summary>
            ///     Right shoulder button
            /// </summary>
            RightShoulder,
            /// <summary>
            ///     Left trigger
            /// </summary>
            LeftTrigger,
            /// <summary>
            ///     Right trigger
            /// </summary>
            RightTrigger,
            /// <summary>
            ///     Left special button (Back on XBox-like)
            /// </summary>
            SelectButton,
            /// <summary>
            ///     Right special button (Menu on XBox-like)
            /// </summary>
            StartButton,
            /// <summary>
            ///     Left Joystick press
            /// </summary>
            LeftStickButton,
            /// <summary>
            ///     Right Joystick press
            /// </summary>
            RightStickButton
        }

        /// <summary>
        ///     Translates a <see cref="GamepadButton"/> to its <see cref="KeyCode"/> value
        /// </summary>
        public static KeyCode GetGamepadKeyCode(GamepadButton @enum)
        {
            return InputUtils.GetGamepadKeyCode(@enum);
        }

        /// <summary>
        ///     Translate a <see cref="GamepadButton"/> to its axis string value
        /// </summary>
        public static GamepadInput GetGamepadInput(GamepadButton @enum)
        {
            return InputUtils.GetGamepadInput(@enum);
        }

        /// <summary>
        ///     Translates a <see cref="GamepadButton"/> to its printable string value
        /// </summary>
        public static string GetGamepadString(GamepadButton @enum)
        {
            return InputUtils.GetGamepadString(@enum);
        }

        /// <summary>
        ///     Translates an axis string to its <see cref="GamepadButton"/> value
        /// </summary>
        public static GamepadButton GetGamepadButton(GamepadInput input)
        {
            return InputUtils.GetGamepadButton(input);
        }

        /// <summary>
        ///     Translate an axis string to its <see cref="GamepadButton"/> value
        /// </summary>
        public static GamepadButton GetGamepadButton(string axis)
        {
            return InputUtils.GetGamepadButton(axis);
        }

        /// <summary>
        ///     Translate a <see cref="KeyCode"/> to its <see cref="GamepadButton"/> value
        /// </summary>
        public static GamepadButton GetGamepadButton(KeyCode key)
        {
            return InputUtils.GetGamepadButton(key);
        }

        // Internal holder for all buttons added via Jotunn
        internal static Dictionary<string, ButtonConfig> Buttons = new Dictionary<string, ButtonConfig>();

        internal static Dictionary<ConfigEntryBase, ButtonConfig> ButtonToConfigDict = new Dictionary<ConfigEntryBase, ButtonConfig>();

        private static InputManager _instance;
        /// <summary>
        ///     Singleton instance
        /// </summary>
        public static InputManager Instance => _instance ??= new InputManager();

        /// <summary>
        ///     Hide .ctor
        /// </summary>
        private InputManager() { }

        static InputManager()
        {
            ((IManager)Instance).Init();
        }

        /// <summary>
        ///     Initialize the manager
        /// </summary>
        void IManager.Init()
        {
            Main.LogInit("InputManager");

            // Dont init on a dedicated server
            if (!GUIUtils.IsHeadless)
            {
                Main.Harmony.PatchAll(typeof(Patches));
            }
        }

        private static class Patches
        {
            [HarmonyPatch(typeof(ZInput), nameof(ZInput.Load)), HarmonyPostfix]
            private static void RegisterCustomInputs(ZInput __instance) => Instance.RegisterCustomInputs(__instance);

            [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetButtonDown)), HarmonyPostfix]
            private static void ZInput_GetButtonDown(string name, ref bool __result) => __result = Instance.ZInput_GetButtonDown(name);

            [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetButton)), HarmonyPostfix]
            private static void ZInput_GetButton(string name, ref bool __result) => __result = Instance.ZInput_GetButton(name);

            [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetButtonUp)), HarmonyPostfix]
            private static void ZInput_GetButtonUp(string name, ref bool __result) => __result = Instance.ZInput_GetButtonUp(name);

            [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetButtonDown)), HarmonyReversePatch]
            public static bool ZInput_GetButtonDown_Original(string name) => throw new NotImplementedException("It's a stub");

            [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetButton)), HarmonyReversePatch]
            public static bool ZInput_GetButton_Original(string name) => throw new NotImplementedException("It's a stub");

            [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetButtonUp)), HarmonyReversePatch]
            public static bool ZInput_GetButtonUp_Original(string name) => throw new NotImplementedException("It's a stub");
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
                buttonConfig.GamepadConfig == null && buttonConfig.GamepadButton == GamepadButton.None
                && string.IsNullOrEmpty(buttonConfig.Axis))
            {
                throw new ArgumentException($"{nameof(buttonConfig)} needs either Axis, Key, Gamepad, Shortcut or a Config set.", nameof(buttonConfig));
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

        private void RegisterCustomInputs(ZInput self)
        {
            if (Buttons.Any())
            {
                Logger.LogInfo($"Registering {Buttons.Count} custom inputs");

                foreach (var pair in Buttons)
                {
                    var btn = pair.Value;

                    if (!string.IsNullOrEmpty(btn.Axis))
                    {
                        self.AddButton(btn.Name, GetGamepadInput(GetGamepadButton(btn.Axis)), btn.RepeatDelay, btn.RepeatInterval);
                    }
                    else if (btn.Key != KeyCode.None)
                    {
                        if (InputUtils.TryKeyCodeToMouseButton(btn.Key, out MouseButton mouseButton))
                        {
                            self.AddButton(btn.Name, mouseButton, btn.RepeatDelay, btn.RepeatInterval);
                        }
                        else
                        {
                            self.AddButton(btn.Name, InputUtils.KeyCodeToKey(btn.Key), btn.RepeatDelay, btn.RepeatInterval);
                        }
                    }
                    else if (btn.Shortcut.MainKey != KeyCode.None)
                    {
                        if (InputUtils.TryKeyCodeToMouseButton(btn.Shortcut.MainKey, out MouseButton mouseButton))
                        {
                            self.AddButton(btn.Name, mouseButton, btn.RepeatDelay, btn.RepeatInterval);
                        }
                        else
                        {
                            self.AddButton(btn.Name, InputUtils.KeyCodeToKey(btn.Shortcut.MainKey), btn.RepeatDelay, btn.RepeatInterval);
                        }
                    }

                    if (btn.GamepadButton != GamepadButton.None)
                    {
                        var joyBtnName = $"Joy!{btn.Name}";
                        GamepadInput input = GetGamepadInput(btn.GamepadButton);

                        if (input != GamepadInput.None)
                        {
                            self.AddButton(joyBtnName, input, btn.RepeatDelay, btn.RepeatInterval);
                        }
                    }

                    Logger.LogDebug($"Registered input {pair.Key}");
                }
            }
        }

        private bool ZInput_GetButtonDown(string name)
        {
            if (!Patches.ZInput_GetButtonDown_Original(name) && !Patches.ZInput_GetButtonDown_Original($"Joy!{name}"))
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

        private bool ZInput_GetButton(string name)
        {
            if (!Patches.ZInput_GetButton_Original(name) && !Patches.ZInput_GetButton_Original($"Joy!{name}"))
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

        private bool ZInput_GetButtonUp(string name)
        {
            if (!Patches.ZInput_GetButtonUp_Original(name) && !Patches.ZInput_GetButtonUp_Original($"Joy!{name}"))
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

            if (button.BlockOtherInputs)
            {
                foreach (var btn in ZInput.instance.m_buttons.Where(x =>
                    x.Value.m_key == InputUtils.KeyCodeToKey(button.Key) ||
                    x.Value.m_key == InputUtils.KeyCodeToKey(GetGamepadKeyCode(button.GamepadButton)) ||
                    x.Value.m_gamepadInput == GetGamepadInput(button.GamepadButton)))
                {
                    ZInput.ResetButtonStatus(btn.Key);
                    btn.Value.m_pressed = false;
                    btn.Value.m_pressedFixed = false;
                }
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
