using BepInEx.Configuration;
using UnityEngine;

namespace Jotunn.GUI
{
    /// <summary>
    ///     Generic abstract version of the config binding class
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal abstract class ConfigBound<T> : MonoBehaviour, IConfigBound
    {
        public ModSettingConfig Config { get; set; }

        public string ModGUID { get; set; }

        public ConfigEntryBase Entry { get; set; }

        public AcceptableValueBase Clamp { get; set; }

        public ConfigurationManagerAttributes Attributes { get; set; }

        public T Default { get; set; }

        public T Value
        {
            get => GetValue();
            set => SetValue(value);
        }

        public abstract T GetValue();
        public abstract void SetValue(T value);

        public void Read()
        {
            Value = (T)Entry.BoxedValue;
        }

        public void Write()
        {
            Entry.BoxedValue = Value;
        }

        public void SetData(string modGuid, ConfigEntryBase entry)
        {
            Config = gameObject.GetComponent<ModSettingConfig>();

            ModGUID = modGuid;
            Entry = entry;

            Register();

            Value = (T)Entry.BoxedValue;
            Clamp = Entry.Description.AcceptableValues;
            Attributes = new ConfigurationManagerAttributes();
            Attributes.SetFromAttributes(Entry.Description?.Tags);

            SetReadOnly(Attributes.ReadOnly == true);
            SetEnabled(!Attributes.IsAdminOnly || Attributes.IsUnlocked);
            Default = (T)Entry.DefaultValue;
        }

        public abstract void Register();

        public abstract void SetEnabled(bool enabled);

        public abstract void SetReadOnly(bool readOnly);

        public void Reset()
        {
            SetValue(Default);
        }

        // Wrap AcceptableValueBase's IsValid
        public bool IsValid()
        {
            if (Clamp != null)
            {
                return Clamp.IsValid(Value);
            }

            return true;
        }
    }
}
