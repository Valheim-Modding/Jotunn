using System;
using System.Globalization;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace Jotunn.GUI
{
    internal class ConfigBoundColor : ConfigBound<Color>
    {
        public override void Register()
        {
            Config.ColorInput.transform.parent.gameObject.SetActive(true);

            Config.ColorInput.onEndEdit.AddListener(SetButtonColor);
            Config.ColorInput.characterValidation = InputField.CharacterValidation.None;
            Config.ColorInput.contentType = InputField.ContentType.Alphanumeric;

            Config.ColorButton.onClick.AddListener(ShowColorPicker);
        }

        public override Color GetValue()
        {
            var col = Config.ColorInput.text;
            try
            {
                return ColorFromString(col);
            }
            catch (Exception e)
            {
                Logger.LogWarning(e);
                Logger.LogWarning($"Using default value ({(Color)Entry.DefaultValue}) instead.");
                return (Color)Entry.DefaultValue;
            }
        }

        public override void SetValue(Color value)
        {
            Config.ColorInput.text = StringFromColor(value);
            Config.ColorButton.targetGraphic.color = value;
        }

        public override void SetEnabled(bool enabled)
        {
            Config.ColorInput.enabled = enabled;
            Config.ColorButton.enabled = enabled;
            if (enabled)
            {
                Config.ColorInput.onEndEdit.AddListener(SetButtonColor);
                Config.ColorButton.onClick.AddListener(ShowColorPicker);
            }
            else
            {
                Config.ColorInput.onEndEdit.RemoveAllListeners();
                Config.ColorButton.onClick.RemoveAllListeners();
            }
        }

        public override void SetReadOnly(bool readOnly)
        {
            Config.ColorInput.readOnly = readOnly;
            Config.ColorInput.textComponent.color = readOnly ? Color.grey : Color.white;
            Config.ColorButton.interactable = !readOnly;
        }

        private void SetButtonColor(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            Config.ColorButton.targetGraphic.color = ColorFromString(value);
        }

        private void ShowColorPicker()
        {
            if (!ColorPicker.done)
            {
                ColorPicker.Cancel();
            }

            GUIManager.Instance.CreateColorPicker(
                                                  new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                                  GetValue(), Entry.Definition.Key, SetValue, (c) => Config.ColorButton.targetGraphic.color = c,
                                                  true);
        }

        private string StringFromColor(Color col)
        {
            var r = (int)(col.r * 255f);
            var g = (int)(col.g * 255f);
            var b = (int)(col.b * 255f);
            var a = (int)(col.a * 255f);

            return $"{r:x2}{g:x2}{b:x2}{a:x2}".ToUpper();
        }

        private Color ColorFromString(string str)
        {
            if (long.TryParse(str.Trim().ToLower(), NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out var fromHex))
            {
                var r = (int)(fromHex >> 24);
                var g = (int)(fromHex >> 16 & 0xff);
                var b = (int)(fromHex >> 8 & 0xff);
                var a = (int)(fromHex & 0xff);
                var result = new Color(r / 255f, g / 255f, b / 255f, a / 255f);
                return result;
            }

            throw new ArgumentException($"'{str}' is no valid color value");
        }
    }
}
