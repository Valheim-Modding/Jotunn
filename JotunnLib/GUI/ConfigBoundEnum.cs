using System;
using System.Linq;
using UnityEngine;

namespace Jotunn.GUI
{
    /// <summary>
    ///     GamepadButton binding
    /// </summary>
    internal class ConfigBoundEnum : ConfigBound<Enum>
    {
        public override void Register()
        {
            Config.Dropdown.gameObject.SetActive(true);
            Config.Dropdown.AddOptions(Enum.GetNames(Entry.SettingType).ToList());
        }

        public override Enum GetValue()
        {
            return (Enum)Enum.Parse(Entry.SettingType, Config.Dropdown.options[Config.Dropdown.value].text);
        }

        public override void SetValue(Enum value)
        {
            Config.Dropdown.value = Config.Dropdown.options
                                          .IndexOf(Config.Dropdown.options.FirstOrDefault(x =>
                                                                                              x.text.Equals(Enum.GetName(Entry.SettingType, value))));
            Config.Dropdown.RefreshShownValue();
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
