using System.Globalization;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace Jotunn.GUI
{
    /// <summary>
    ///     Double binding
    /// </summary>
    internal class ConfigBoundDouble : ConfigBound<double>
    {
        public override void Register()
        {
            Config.InputField.gameObject.SetActive(true);
            Config.InputField.characterValidation = InputField.CharacterValidation.Decimal;

            if (Entry.Description.AcceptableValues is AcceptableValueRange<double> acceptableValueRange)
            {
                Config.Slider.gameObject.SetActive(true);
                Config.Slider.minValue = (float)acceptableValueRange.MinValue;
                Config.Slider.maxValue = (float)acceptableValueRange.MaxValue;
                var step = Mathf.Clamp(Config.Slider.minValue / Config.Slider.maxValue, 0.1f, 1f);
                Config.Slider.onValueChanged.AddListener(value =>
                                                             Config.InputField.SetTextWithoutNotify((Mathf.Round(value / step) * step)
                                                                                                    .ToString("F3", CultureInfo.CurrentCulture)));
                Config.InputField.onValueChanged.AddListener(text =>
                {
                    if (double.TryParse(text, out var value))
                    {
                        Config.Slider.SetValueWithoutNotify((float)value);
                    }
                });
            }

            Config.InputField.onValueChanged.AddListener(x =>
            {
                Config.InputField.textComponent.color = IsValid() ? Color.white : Color.red;
            });
        }

        public override double GetValue()
        {
            if (!double.TryParse(Config.InputField.text, NumberStyles.Number,
                                 CultureInfo.CurrentCulture.NumberFormat, out var temp))
            {
                temp = Default;
            }

            return temp;
        }

        public override void SetValue(double value)
        {
            Config.InputField.text = value.ToString("F3");
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
