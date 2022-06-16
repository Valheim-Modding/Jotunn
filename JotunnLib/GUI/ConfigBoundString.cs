using UnityEngine;
using UnityEngine.UI;

namespace Jotunn.GUI
{
    /// <summary>
    ///     String binding
    /// </summary>
    internal class ConfigBoundString : ConfigBound<string>
    {
        public override void Register()
        {
            Config.InputField.gameObject.SetActive(true);
            Config.InputField.characterValidation = InputField.CharacterValidation.None;
            Config.InputField.contentType = InputField.ContentType.Standard;
        }

        public override string GetValue()
        {
            return Config.InputField.text;
        }

        public override void SetValue(string value)
        {
            Config.InputField.text = value;
        }

        public override void SetEnabled(bool enabled)
        {
            Config.InputField.enabled = enabled;
        }

        public override void SetReadOnly(bool readOnly)
        {
            Config.InputField.readOnly = readOnly;
            Config.InputField.textComponent.color = readOnly ? Color.grey : Color.white;
        }
    }
}
