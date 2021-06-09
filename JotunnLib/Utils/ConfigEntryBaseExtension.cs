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

namespace Jotunn.Utils
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
            var cma = configurationEntry.Description.Tags.FirstOrDefault(x => x is ConfigurationManagerAttributes) as ConfigurationManagerAttributes;
            if (cma != null)
            {
                // if configuration manager attribute is set, check if browsable is not false
                return cma.Browsable != false;
            }

            // no configuration manager attribute?
            return true;
        }

        /// <summary>
        ///     Check, if button is bound to a configuration entry
        /// </summary>
        /// <param name="configurationEntry"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool IsButtonBound(this ConfigEntryBase configurationEntry)
        {
            if (configurationEntry == null)
            {
                throw new ArgumentNullException(nameof(configurationEntry));
            }

            if (configurationEntry.SettingType != typeof(KeyCode))
            {
                return false;
            }

            InputManager.ButtonToConfigDict.TryGetValue(configurationEntry, out var buttonConfig);
            if (buttonConfig != null)
            {
                return !string.IsNullOrEmpty(buttonConfig.Name);
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

            if (configurationEntry.SettingType != typeof(KeyCode))
            {
                return null;
            }

            InputManager.ButtonToConfigDict.TryGetValue(configurationEntry, out var buttonConfig);
            if (buttonConfig != null)
            {
                return buttonConfig.Name.Split('!')[0];
            }

            return configurationEntry.Definition.Key;
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

            if (configurationEntry.SettingType != typeof(KeyCode))
            {
                return null;
            }
            
            InputManager.ButtonToConfigDict.TryGetValue(configurationEntry, out var buttonConfig);

            return buttonConfig;
        }
    }
}
