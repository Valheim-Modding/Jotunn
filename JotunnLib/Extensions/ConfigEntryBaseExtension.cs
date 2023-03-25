// JotunnLib
// a Valheim mod
// 
// File:    ConfigEntryBaseExtension.cs
// Project: JotunnLib

using System;
using System.Linq;
using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace Jotunn
{
    /// <summary>
    ///     Extends <see cref="ConfigEntryBase"/> with convenience functions.
    /// </summary>
    public static class ConfigEntryBaseExtension
    {
        /// <summary>
        ///     Check, if this config entry is "visible"
        /// </summary>
        /// <param name="configurationEntry"></param>
        /// <returns></returns>
        public static bool IsVisible(this ConfigEntryBase configurationEntry)
        {
            ConfigurationManagerAttributes attributes = new ConfigurationManagerAttributes();
            attributes.SetFromAttributes(configurationEntry.Description?.Tags);
            return attributes.Browsable != false;
        }

        /// <summary>
        ///     Check, if this config entry is "syncable"
        /// </summary>
        /// <param name="configurationEntry"></param>
        /// <returns></returns>
        public static bool IsSyncable(this ConfigEntryBase configurationEntry)
        {
            var cma = configurationEntry.Description.Tags.FirstOrDefault(x => x is ConfigurationManagerAttributes) as ConfigurationManagerAttributes;
            if (cma != null)
            {
                return cma.IsAdminOnly;
            }
            return false;
        }
        
        /// <summary>
        ///     Get bound button's name
        /// </summary>
        /// <param name="configurationEntry"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string GetBoundButtonName(this ConfigEntryBase configurationEntry)
        {
            if (configurationEntry == null)
            {
                throw new ArgumentNullException(nameof(configurationEntry));
            }

            if (configurationEntry.SettingType != typeof(KeyCode) &&
                configurationEntry.SettingType != typeof(KeyboardShortcut) &&
                configurationEntry.SettingType != typeof(InputManager.GamepadButton))
            {
                return null;
            }

            return InputManager.ButtonToConfigDict.TryGetValue(configurationEntry, out var buttonConfig)
                ? buttonConfig.Name
                : null;
        }


        /// <summary>
        ///     Get bound button config
        /// </summary>
        /// <param name="configurationEntry"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static ButtonConfig GetButtonConfig(this ConfigEntryBase configurationEntry)
        {
            if (configurationEntry == null)
            {
                throw new ArgumentNullException(nameof(configurationEntry));
            }

            if (configurationEntry.SettingType != typeof(KeyCode) &&
                configurationEntry.SettingType != typeof(KeyboardShortcut) &&
                configurationEntry.SettingType != typeof(InputManager.GamepadButton))
            {
                return null;
            }
            
            InputManager.ButtonToConfigDict.TryGetValue(configurationEntry, out var buttonConfig);

            return buttonConfig;
        }

        /// <summary>
        ///     Get the local value of an admin config
        /// </summary>
        /// <param name="configurationEntry"></param>
        /// <returns></returns>
        internal static object GetLocalValue(this ConfigEntryBase configurationEntry)
        {
            var cma = configurationEntry.Description.Tags.FirstOrDefault(x => x is ConfigurationManagerAttributes) as ConfigurationManagerAttributes;
            if (cma != null)
            {
                return cma.LocalValue;
            }
            return null;
        }

        /// <summary>
        ///     Set the local value of an admin config
        /// </summary>
        /// <param name="configurationEntry"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static void SetLocalValue(this ConfigEntryBase configurationEntry, object value)
        {
            var cma = configurationEntry.Description.Tags.FirstOrDefault(x => x is ConfigurationManagerAttributes) as ConfigurationManagerAttributes;
            if (cma != null)
            {
                cma.LocalValue = value; 
            }
        }

        internal static ConfigurationManagerAttributes GetConfigurationManagerAttributes(this ConfigEntryBase configEntry)
        {
            return (ConfigurationManagerAttributes)configEntry.Description.Tags.FirstOrDefault(x => x is ConfigurationManagerAttributes);
        }
    }
}
