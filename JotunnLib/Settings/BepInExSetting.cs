using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;

namespace Jotunn.Settings
{
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
