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
    public static class ConfigEntryBaseExtension
    {
        public static bool IsVisible(this ConfigEntryBase ceb)
        {
            var cma = ceb.Description.Tags.FirstOrDefault(x => x is ConfigurationManagerAttributes) as ConfigurationManagerAttributes;
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

            var buttonConfig = InputManager.ButtonToConfigDict[configurationEntry];
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

            if (!InputManager.ButtonToConfigDict.Keys.Contains(configurationEntry))
            {
                return null;
            }

            var buttonConfig = InputManager.ButtonToConfigDict[configurationEntry];

            return buttonConfig.Name.Split('!')[0];
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

            return InputManager.ButtonToConfigDict[configurationEntry];
        }
    }
}
