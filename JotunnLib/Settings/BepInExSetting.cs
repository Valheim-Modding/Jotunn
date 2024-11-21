using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;

namespace Jotunn.Settings
{
    /// <summary>
    ///     Base class for in-game BepInEx settings
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BepInExSetting<T> : Setting<T>
    {
        public string Section { get; set; }
        public string Key { get; set; }
        public T DefaultValue { get; set; }
        public string Description { get; set; }
        public int Order { get; private set; }
        public bool AdminOnly { get; set; }

        private ConfigEntry<T> entry;

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
        ///     Bind this setting to the BepInEx configuration system
        /// </summary>
        public override void Bind()
        {
            if (entry != null)
            {
                return;
            }

            BaseUnityPlugin plugin = Chainloader.PluginInfos[SourceMod.GUID].Instance;
            entry = plugin.Config.Bind(Section, Key, DefaultValue, new ConfigDescription(Description, null, GenerateAttributes()));
            entry.SettingChanged += (sender, args) => Value = entry.Value;
            Value = entry.Value;
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

        protected virtual ConfigurationManagerAttributes GenerateAttributes()
        {
            return new ConfigurationManagerAttributes
            {
                IsAdminOnly = AdminOnly,
                Order = Order,
            };
        }
    }
}
