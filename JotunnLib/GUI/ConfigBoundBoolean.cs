
namespace Jotunn.GUI
{
    /// <summary>
    ///     Boolean Binding
    /// </summary>
    internal class ConfigBoundBoolean : ConfigBound<bool>
    {
        public override void Register()
        {
            Config.Toggle.gameObject.SetActive(true);
        }

        public override bool GetValue()
        {
            return Config.Toggle.isOn;
        }

        public override void SetValue(bool value)
        {
            Config.Toggle.isOn = value;
        }

        public override void SetEnabled(bool enabled)
        {
            Config.Toggle.enabled = enabled;
        }

        public override void SetReadOnly(bool readOnly)
        {
            Config.Toggle.interactable = !readOnly;
        }
    }
}
