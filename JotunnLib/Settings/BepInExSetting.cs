using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;

namespace Jotunn.Settings
{
    /// <summary>
    ///     Base class for in-game BepInEx settings
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TSerialized"></typeparam>
    public abstract class BepInExSetting<T, TSerialized> : Setting<T>
    {
        /// <summary>
        ///     Section/category/group of the setting. Settings are grouped by this.
        /// </summary>
        public string Section { get; set; }

        /// <summary>
        ///     Name of the setting
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        ///     Value of the setting if the setting was not created yet
        /// </summary>
        public T DefaultValue { get; set; }

        /// <summary>
        ///     Text describing the function of the setting and any notes or warnings
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        ///     Order of the setting on the settings list relative to other settings in a category. 0 by default, higher number is higher on the list
        /// </summary>
        public int Order { get; private set; }

        /// <summary>
        ///     Whether the config is only writable by admins and gets overwritten on connecting clients
        /// </summary>
        public bool AdminOnly { get; set; }

        protected ConfigEntry<TSerialized> entry;

        /// <summary>
        ///     Creates a new BepInExSetting object.<br />
        ///     Does not create the setting in the BepInEx configuration system yet, <see cref="Bind"/> must be called.
        /// </summary>
        /// <param name="sourceMod"></param>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <param name="description"></param>
        /// <param name="order"></param>
        /// <param name="adminOnly"></param>
        public BepInExSetting(BepInPlugin sourceMod, string section, string key, T defaultValue, string description, int order, bool adminOnly = true) : base(sourceMod)
        {
            Section = section;
            Key = key;
            DefaultValue = defaultValue;
            Description = description;
            Order = order;
            AdminOnly = adminOnly;
        }

        /// <summary>
        ///     Serialize the value to the type passed to the <see cref="ConfigEntry{T}"/>
        /// </summary>
        /// <param name="value">value to serialize</param>
        /// <returns></returns>
        public abstract TSerialized Serialize(T value);

        /// <summary>
        ///     Deserialize the value from the type passed to the <see cref="ConfigEntry{T}"/>
        /// </summary>
        /// <param name="value">value to deserialize</param>
        /// <returns></returns>
        public abstract T Deserialize(TSerialized value);

        /// <summary>
        ///     Bind this setting to the BepInEx configuration system
        /// </summary>
        public override void Bind()
        {
            if (entry != null)
            {
                return;
            }

            BaseUnityPlugin plugin = Chainloader.PluginInfos[SourceMod.GUID].Instance;
            entry = plugin.Config.Bind(Section, Key, Serialize(DefaultValue), new ConfigDescription(Description, null, GenerateAttributes()));
            entry.SettingChanged += (sender, args) => Value = Deserialize(entry.Value);
            Value = Deserialize(entry.Value);
        }

        /// <summary>
        ///     Removes this setting from the BepInEx configuration system
        /// </summary>
        public override void Unbind()
        {
            if (entry == null)
            {
                return;
            }

            BaseUnityPlugin plugin = Chainloader.PluginInfos[SourceMod.GUID].Instance;
            plugin.Config.Remove(entry.Definition);
            entry = null;

            if (plugin.Config.SaveOnConfigSet)
            {
                plugin.Config.Save();
            }

            Value = DefaultValue;
        }

        /// <summary>
        ///     Generates <see cref="ConfigurationManagerAttributes"/> for this setting
        /// </summary>
        /// <returns></returns>
        protected virtual ConfigurationManagerAttributes GenerateAttributes()
        {
            return new ConfigurationManagerAttributes
            {
                IsAdminOnly = AdminOnly,
                Order = Order,
            };
        }
    }

    /// <summary>
    ///     Base class for in-game BepInEx settings
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BepInExSetting<T> : BepInExSetting<T, T>
    {
        /// <inheritdoc/>
        public BepInExSetting(BepInPlugin sourceMod, string section, string key, T defaultValue, string description, int order, bool adminOnly = true) : base(sourceMod, section, key, defaultValue, description, order, adminOnly)
        {
        }

        /// <inheritdoc/>
        public override T Serialize(T value) => value;

        /// <inheritdoc/>
        public override T Deserialize(T value) => value;
    }
}
