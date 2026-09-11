using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using Jotunn.Utils;
using UnityEngine;

namespace Jotunn.Settings
{
    /// <summary>
    ///     A setting that allows the user to select a value from a dropdown list
    /// </summary>
    /// <typeparam name="T">The type of the values in the dropdown</typeparam>
    public class BepInExDropdownSetting<T> : BepInExSetting<T>
    {
        private List<T> values;

        private static Dictionary<ConfigEntryBase, ComboBox> comboboxes = new Dictionary<ConfigEntryBase, ComboBox>();

        private static GUIStyle dropDownStyle;
        private static GUIStyle listStyle;

        /// <summary>
        ///     Creates a new BepInExSetting object.<br />
        ///     Does not create the setting in the BepInEx configuration system yet, Bind must be called.
        /// </summary>
        /// <param name="sourceMod">The mod that adds this setting</param>
        /// <param name="section">Section/category/group of the setting. Settings are grouped by this</param>
        /// <param name="key">Name of the setting</param>
        /// <param name="defaultValue">Value of the setting if the setting was not created yet</param>
        /// <param name="values">List of values to choose from in the dropdown</param>
        /// <param name="description">Text describing the function of the setting and any notes or warnings</param>
        /// <param name="order">Order of the setting on the settings list relative to other settings in a category. 0 by default, higher number is higher on the list</param>
        /// <param name="adminOnly">Whether the config is only writable by admins and gets overwritten on connecting clients</param>
        public BepInExDropdownSetting(BepInPlugin sourceMod, string section, string key, T defaultValue, IEnumerable<T> values, string description, int order, bool adminOnly = true) : base(sourceMod, section, key, defaultValue, description, order, adminOnly)
        {
            this.values = new List<T>(values);
        }

        /// <summary>
        ///     Generates the attributes for the setting.<br />
        ///     This is used to create the custom drawer for the setting in the configuration manager.
        /// </summary>
        /// <returns></returns>
        protected override ConfigurationManagerAttributes GenerateAttributes()
        {
            ConfigurationManagerAttributes attributes = base.GenerateAttributes();
            attributes.CustomDrawer = Drawer;
            attributes.autoCompleteList = values;
            return attributes;
        }

        /// <summary>
        ///     Custom drawer for the setting in the configuration manager
        /// </summary>
        /// <param name="entry">The configuration entry to draw</param>
        protected virtual void Drawer(ConfigEntryBase entry)
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

            bool readOnly = entry.GetConfigurationManagerAttributes()?.ReadOnly ?? false;

            string newValue = GUILayout.TextField(entry.BoxedValue.ToString(), GUILayout.ExpandWidth(true));
            if (!readOnly)
            {
                entry.BoxedValue = newValue;
            }

            var buttonText = new GUIContent("\u25bc");
            var dispRect = GUILayoutUtility.GetRect(buttonText, dropDownStyle, GUILayout.Width(25));

            if (!comboboxes.TryGetValue(entry, out ComboBox combobox))
            {
                var attributes = entry.GetConfigurationManagerAttributes();
                var contents = ((List<T>)attributes.autoCompleteList).ConvertAll(x => new GUIContent(x.ToString())).ToArray();
                var settingWindowRect = ConfigManagerUtils.SettingWindowRect;

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
                if (!readOnly)
                {
                    entry.BoxedValue = ((List<T>)attributes.autoCompleteList)[index];
                }
            });
        }
    }
}
