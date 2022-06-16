using System.Globalization;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace Jotunn.GUI
{
    /// <summary>
    ///     Integer binding
    /// </summary>
    internal class ConfigBoundInt : ConfigBound<int>
    {
        public override void Register()
        {
            Config.InputField.gameObject.SetActive(true);
            Config.InputField.characterValidation = InputField.CharacterValidation.Integer;

            if (Entry.Description.AcceptableValues is AcceptableValueRange<int> acceptableValueRange)
            {
                Config.Slider.gameObject.SetActive(true);
                Config.Slider.minValue = acceptableValueRange.MinValue;
                Config.Slider.maxValue = acceptableValueRange.MaxValue;
                Config.Slider.onValueChanged.AddListener(value =>
                                                             Config.InputField.SetTextWithoutNotify(((int)value)
                                                                                                    .ToString(CultureInfo.CurrentCulture)));
                Config.InputField.onValueChanged.AddListener(text =>
                {
                    if (int.TryParse(text, out var value))
                    {
                        Config.Slider.SetValueWithoutNotify(value);
                    }
                });
            }

            Config.InputField.onValueChanged.AddListener(x =>
            {
                Config.InputField.textComponent.color = IsValid() ? Color.white : Color.red;
            });
        }

        public override int GetValue()
        {
            if (!int.TryParse(Config.InputField.text, out var temp))
            {
                temp = Default;
            }

            return temp;
        }

        public override void SetValue(int value)
        {
            Config.InputField.text = value.ToString();
        }

        public override void SetEnabled(bool enabled)
        {
            Config.InputField.enabled = enabled;
            Config.Slider.enabled = enabled;
        }

        public override void SetReadOnly(bool readOnly)
        {
            Config.InputField.readOnly = readOnly;
            Config.InputField.textComponent.color = readOnly ? Color.grey : Color.white;
            Config.Slider.interactable = !readOnly;
        }
    }
}
