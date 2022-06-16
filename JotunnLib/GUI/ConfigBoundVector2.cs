using System.Globalization;
using UnityEngine;

namespace Jotunn.GUI
{
    /// <summary>
    ///     Vector2 binding
    /// </summary>
    internal class ConfigBoundVector2 : ConfigBound<Vector2>
    {
        public override void Register()
        {
            Config.Vector2InputX.transform.parent.gameObject.SetActive(true);
        }

        public override Vector2 GetValue()
        {
            if (!(float.TryParse(Config.Vector2InputX.text, NumberStyles.Number,
                                 CultureInfo.CurrentCulture.NumberFormat, out var tempX) &&
                  float.TryParse(Config.Vector2InputY.text, NumberStyles.Number,
                                 CultureInfo.CurrentCulture.NumberFormat, out var tempY)))
            {
                return Default;
            }

            return new Vector2(tempX, tempY);
        }

        public override void SetValue(Vector2 value)
        {
            Config.Vector2InputX.text = value.x.ToString("F1");
            Config.Vector2InputY.text = value.y.ToString("F1");
        }

        public override void SetEnabled(bool enabled)
        {
            Config.Vector2InputX.enabled = enabled;
            Config.Vector2InputY.enabled = enabled;
        }

        public override void SetReadOnly(bool readOnly)
        {
            Config.Vector2InputX.readOnly = readOnly;
            Config.Vector2InputX.textComponent.color = readOnly ? Color.grey : Color.white;
            Config.Vector2InputY.readOnly = readOnly;
            Config.Vector2InputY.textComponent.color = readOnly ? Color.grey : Color.white;
        }
    }
}
