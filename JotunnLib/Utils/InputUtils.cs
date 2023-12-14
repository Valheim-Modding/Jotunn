using System.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;
using static Jotunn.Managers.InputManager;

namespace Jotunn.Utils
{
    /// <summary>
    ///     Utility class for converting inputs to different formats
    /// </summary>
    internal static class InputUtils
    {
        /// <summary>
        ///     Translates a Unity <see cref="KeyCode"/> KeyCode to a InputSystem <see cref="Key"/>
        /// </summary>
        /// <param name="key"></param>
        /// <returns>The matching Key or Key.None, if not assignable</returns>
        public static Key KeyCodeToKey(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.None: return Key.None;
                case KeyCode.Backspace: return Key.Backspace;
                case KeyCode.Delete: return Key.Delete;
                case KeyCode.Tab: return Key.Tab;
                case KeyCode.Return: return Key.Enter;
                case KeyCode.Pause: return Key.Pause;
                case KeyCode.Escape: return Key.Escape;
                case KeyCode.Space: return Key.Space;
                case KeyCode.Keypad0: return Key.Numpad0;
                case KeyCode.Keypad1: return Key.Numpad1;
                case KeyCode.Keypad2: return Key.Numpad2;
                case KeyCode.Keypad3: return Key.Numpad3;
                case KeyCode.Keypad4: return Key.Numpad4;
                case KeyCode.Keypad5: return Key.Numpad5;
                case KeyCode.Keypad6: return Key.Numpad6;
                case KeyCode.Keypad7: return Key.Numpad7;
                case KeyCode.Keypad8: return Key.Numpad8;
                case KeyCode.Keypad9: return Key.Numpad9;
                case KeyCode.KeypadPeriod: return Key.NumpadPeriod;
                case KeyCode.KeypadDivide: return Key.NumpadDivide;
                case KeyCode.KeypadMultiply: return Key.NumpadMultiply;
                case KeyCode.KeypadMinus: return Key.NumpadMinus;
                case KeyCode.KeypadPlus: return Key.NumpadPlus;
                case KeyCode.KeypadEnter: return Key.NumpadEnter;
                case KeyCode.KeypadEquals: return Key.NumpadEquals;
                case KeyCode.UpArrow: return Key.UpArrow;
                case KeyCode.DownArrow: return Key.DownArrow;
                case KeyCode.RightArrow: return Key.RightArrow;
                case KeyCode.LeftArrow: return Key.LeftArrow;
                case KeyCode.Insert: return Key.Insert;
                case KeyCode.Home: return Key.Home;
                case KeyCode.End: return Key.End;
                case KeyCode.PageUp: return Key.PageUp;
                case KeyCode.PageDown: return Key.PageDown;
                case KeyCode.F1: return Key.F1;
                case KeyCode.F2: return Key.F2;
                case KeyCode.F3: return Key.F3;
                case KeyCode.F4: return Key.F4;
                case KeyCode.F5: return Key.F5;
                case KeyCode.F6: return Key.F6;
                case KeyCode.F7: return Key.F7;
                case KeyCode.F8: return Key.F8;
                case KeyCode.F9: return Key.F9;
                case KeyCode.F10: return Key.F10;
                case KeyCode.F11: return Key.F11;
                case KeyCode.F12: return Key.F12;
                case KeyCode.Alpha0: return Key.Digit0;
                case KeyCode.Alpha1: return Key.Digit1;
                case KeyCode.Alpha2: return Key.Digit2;
                case KeyCode.Alpha3: return Key.Digit3;
                case KeyCode.Alpha4: return Key.Digit4;
                case KeyCode.Alpha5: return Key.Digit5;
                case KeyCode.Alpha6: return Key.Digit6;
                case KeyCode.Alpha7: return Key.Digit7;
                case KeyCode.Alpha8: return Key.Digit8;
                case KeyCode.Alpha9: return Key.Digit9;
                case KeyCode.A: return Key.A;
                case KeyCode.B: return Key.B;
                case KeyCode.C: return Key.C;
                case KeyCode.D: return Key.D;
                case KeyCode.E: return Key.E;
                case KeyCode.F: return Key.F;
                case KeyCode.G: return Key.G;
                case KeyCode.H: return Key.H;
                case KeyCode.I: return Key.I;
                case KeyCode.J: return Key.J;
                case KeyCode.K: return Key.K;
                case KeyCode.L: return Key.L;
                case KeyCode.M: return Key.M;
                case KeyCode.N: return Key.N;
                case KeyCode.O: return Key.O;
                case KeyCode.P: return Key.P;
                case KeyCode.Q: return Key.Q;
                case KeyCode.R: return Key.R;
                case KeyCode.S: return Key.S;
                case KeyCode.T: return Key.T;
                case KeyCode.U: return Key.U;
                case KeyCode.V: return Key.V;
                case KeyCode.W: return Key.W;
                case KeyCode.X: return Key.X;
                case KeyCode.Y: return Key.Y;
                case KeyCode.Z: return Key.Z;
                case KeyCode.CapsLock: return Key.CapsLock;
                case KeyCode.ScrollLock: return Key.ScrollLock;
                case KeyCode.RightShift: return Key.RightShift;
                case KeyCode.LeftShift: return Key.LeftShift;
                case KeyCode.RightControl: return Key.RightCtrl;
                case KeyCode.LeftControl: return Key.LeftCtrl;
                case KeyCode.RightAlt: return Key.RightAlt;
                case KeyCode.LeftAlt: return Key.LeftAlt;
                case KeyCode.LeftCommand: return Key.LeftCommand;
                case KeyCode.LeftWindows: return Key.LeftWindows;
                case KeyCode.RightCommand: return Key.RightCommand;
                case KeyCode.RightWindows: return Key.RightWindows;
                case KeyCode.AltGr: return Key.AltGr;

                default:
                    Logger.LogWarning($"Key {key} not found in the new input system");
                    return Key.None;
            }
        }

        /// <summary>
        ///     Tries to convert a <see cref="KeyCode"/> KeyCode to a InputSystem <see cref="UnityEngine.InputSystem.LowLevel.MouseButton"/>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="mouseButton"></param>
        /// <returns></returns>
        public static bool TryKeyCodeToMouseButton(KeyCode key, out UnityEngine.InputSystem.LowLevel.MouseButton mouseButton)
        {
            switch (key)
            {
                case KeyCode.Mouse0:
                    mouseButton = UnityEngine.InputSystem.LowLevel.MouseButton.Left;
                    return true;
                case KeyCode.Mouse1:
                    mouseButton = UnityEngine.InputSystem.LowLevel.MouseButton.Right;
                    return true;
                case KeyCode.Mouse2:
                    mouseButton = UnityEngine.InputSystem.LowLevel.MouseButton.Middle;
                    return true;
                case KeyCode.Mouse3:
                    mouseButton = UnityEngine.InputSystem.LowLevel.MouseButton.Forward;
                    return true;
                case KeyCode.Mouse4:
                    mouseButton = UnityEngine.InputSystem.LowLevel.MouseButton.Back;
                    return true;
                default:
                    mouseButton = UnityEngine.InputSystem.LowLevel.MouseButton.Left;
                    return false;
            }
        }

        /// <summary>
        ///     Translates a <see cref="GamepadButton"/> to its <see cref="KeyCode"/> value
        /// </summary>
        public static KeyCode GetGamepadKeyCode(GamepadButton @enum)
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

        /// <summary>
        ///     Translates a <see cref="GamepadButton"/> to its axis string value
        /// </summary>
        public static GamepadInput GetGamepadInput(GamepadButton @enum)
        {
            return @enum switch
            {
                GamepadButton.DPadUp => GamepadInput.DPadUp,
                GamepadButton.DPadDown => GamepadInput.DPadDown,
                GamepadButton.DPadLeft => GamepadInput.DPadLeft,
                GamepadButton.DPadRight => GamepadInput.DPadRight,
                GamepadButton.LeftTrigger => GamepadInput.TriggerL,
                GamepadButton.RightTrigger => GamepadInput.TriggerR,
                _ => GamepadInput.None
            };
        }

        /// <summary>
        ///     Translates a <see cref="GamepadButton"/> to its printable string value
        /// </summary>
        public static string GetGamepadString(GamepadButton @enum)
        {
            return @enum switch
            {
                GamepadButton.None => string.Empty,
                GamepadButton.DPadLeft => "<",
                GamepadButton.DPadUp => ">",
                GamepadButton.DPadRight => ">",
                GamepadButton.DPadDown => "<",
                GamepadButton.ButtonNorth => "Y",
                GamepadButton.ButtonSouth => "A",
                GamepadButton.ButtonWest => "X",
                GamepadButton.ButtonEast => "B",
                GamepadButton.LeftShoulder => "LB",
                GamepadButton.RightShoulder => "RB",
                GamepadButton.LeftTrigger => "LT",
                GamepadButton.RightTrigger => "RT",
                GamepadButton.StartButton => "Menu",
                GamepadButton.SelectButton => "Back",
                GamepadButton.LeftStickButton => "L",
                GamepadButton.RightStickButton => "R",
                _ => string.Empty
            };
        }

        /// <summary>
        ///     Translates an axis string to its <see cref="GamepadButton"/> value
        /// </summary>
        public static GamepadButton GetGamepadButton(GamepadInput input)
        {
            return input switch
            {
                GamepadInput.DPadUp => GamepadButton.DPadUp,
                GamepadInput.DPadDown => GamepadButton.DPadDown,
                GamepadInput.DPadLeft => GamepadButton.DPadLeft,
                GamepadInput.DPadRight => GamepadButton.DPadRight,
                GamepadInput.TriggerL => GamepadButton.LeftTrigger,
                GamepadInput.TriggerR => GamepadButton.RightTrigger,
                _ => GamepadButton.None
            };
        }

        /// <summary>
        ///     Translate an axis string to its <see cref="GamepadButton"/> value
        /// </summary>
        public static GamepadButton GetGamepadButton(string axis)
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

        /// <summary>
        ///     Translates a <see cref="KeyCode"/> to its <see cref="GamepadButton"/> value
        /// </summary>
        public static GamepadButton GetGamepadButton(KeyCode key)
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
    }
}
