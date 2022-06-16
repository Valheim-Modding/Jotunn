using System;
using System.Linq;
using Jotunn.Managers;
using UnityEngine;

namespace Jotunn.GUI
{
    /// <summary>
    ///     GamepadButton binding
    /// </summary>
    internal class ConfigBoundGamepadButton : ConfigBound<InputManager.GamepadButton>
    {
        public override void Register()
        {
            Config.Dropdown.gameObject.SetActive(true);
            Config.Dropdown.GetComponent<RectTransform>().anchoredPosition -= new Vector2(0f, 35f);
            Config.Dropdown.AddOptions(Enum.GetNames(typeof(InputManager.GamepadButton)).ToList());
        }

        public override InputManager.GamepadButton GetValue()
        {
            if (Enum.TryParse<InputManager.GamepadButton>(Config.Dropdown.options[Config.Dropdown.value].text, out var ret))
            {
                return ret;
            }

            return InputManager.GamepadButton.None;
        }

        public override void SetValue(InputManager.GamepadButton value)
        {
            Config.Dropdown.value = Config.Dropdown.options
                                          .IndexOf(Config.Dropdown.options.FirstOrDefault(x =>
                                                                                              x.text.Equals(Enum.GetName(typeof(InputManager.GamepadButton), value))));
            Config.Dropdown.RefreshShownValue();
        }

        public void Start()
        {
            var buttonName = $"Joy!{Entry.GetBoundButtonName()}";
            Config.Dropdown.onValueChanged.AddListener(index =>
            {
                if (Enum.TryParse<InputManager.GamepadButton>(Config.Dropdown.options[index].text, out var btn) &&
                    ZInput.instance.m_buttons.TryGetValue(buttonName, out var def))
                {
                    KeyCode keyCode = InputManager.GetGamepadKeyCode(btn);
                    string axis = InputManager.GetGamepadAxis(btn);

                    if (!string.IsNullOrEmpty(axis))
                    {
                        def.m_key = KeyCode.None;
                        bool invert = axis.StartsWith("-");
                        def.m_axis = axis.TrimStart('-');
                        def.m_inverted = invert;
                    }
                    else
                    {
                        def.m_axis = null;
                        def.m_key = keyCode;
                    }
                }
            });
        }

        public override void SetEnabled(bool enabled)
        {
            Config.Dropdown.enabled = enabled;
        }

        public override void SetReadOnly(bool readOnly)
        {
            Config.Dropdown.interactable = !readOnly;
            Config.Dropdown.itemText.color = readOnly ? Color.grey : Color.white;
        }
    }
}
