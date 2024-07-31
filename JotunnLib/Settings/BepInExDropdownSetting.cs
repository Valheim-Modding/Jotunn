using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using Jotunn.Utils;
using UnityEngine;

namespace Jotunn.Settings
{
    public class BepInExDropdownSetting<T> : BepInExSetting<T>
    {
        private List<T> values;

        private static Dictionary<BepInEx.Configuration.ConfigEntryBase, ComboBox> comboboxes = new Dictionary<BepInEx.Configuration.ConfigEntryBase, ComboBox>();

        private static GUIStyle dropDownStyle;
        private static GUIStyle listStyle;

        private static PropertyInfo SettingWindowRect { get; }

        static BepInExDropdownSetting()
        {
            var configManagerType = AccessTools.TypeByName("ConfigurationManager.ConfigurationManager, ConfigurationManager");
            SettingWindowRect = AccessTools.Property(configManagerType, "SettingWindowRect");
        }

        public BepInExDropdownSetting(BepInPlugin sourceMod, string section, string key, T defaultValue, IEnumerable<T> values, string description, int order, bool adminOnly = true) : base(sourceMod, section, key, defaultValue, description, order, adminOnly)
        {
            this.values = new List<T>(values);
        }

        protected override ConfigurationManagerAttributes GenerateAttributes()
        {
            ConfigurationManagerAttributes attributes = base.GenerateAttributes();
            attributes.CustomDrawer = Drawer;
            attributes.autoCompleteList = values;
            return attributes;
        }

        protected virtual void Drawer(BepInEx.Configuration.ConfigEntryBase entry)
        {
            if (dropDownStyle == null)
            {
                dropDownStyle = new GUIStyle(UnityEngine.GUI.skin.button);
                dropDownStyle.clipping = TextClipping.Overflow;
                dropDownStyle.alignment = TextAnchor.MiddleCenter;
            }

            if (listStyle == null)
            {
                listStyle = new GUIStyle(UnityEngine.GUI.skin.button);
                listStyle.clipping = TextClipping.Overflow;
            }

            entry.BoxedValue = GUILayout.TextField(entry.BoxedValue.ToString(), GUILayout.ExpandWidth(true));

            var buttonText = new GUIContent("\u25bc");
            var dispRect = GUILayoutUtility.GetRect(buttonText, dropDownStyle, GUILayout.Width(25));

            if (!comboboxes.TryGetValue(entry, out ComboBox combobox))
            {
                var attributes = entry.GetConfigurationManagerAttributes();
                var contents = ((List<T>)attributes.autoCompleteList).ConvertAll(x => new GUIContent(x.ToString())).ToArray();

                var settingWindowRect = (Rect)SettingWindowRect.GetValue(ConfigManagerUtils.Plugin);

                combobox = new ComboBox(dispRect, buttonText, contents, listStyle, dropDownStyle, settingWindowRect.yMax);
                comboboxes.Add(entry, combobox);
            }
            else
            {
                combobox.Rect = dispRect;
                combobox.ButtonContent = buttonText;
            }

            combobox.Show(index =>
            {
                var attributes = entry.GetConfigurationManagerAttributes();
                entry.BoxedValue = ((List<T>)attributes.autoCompleteList)[index];
            });
        }
    }
}
