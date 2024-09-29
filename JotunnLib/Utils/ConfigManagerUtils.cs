using System;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;

namespace Jotunn.Utils
{
    /// <summary>
    ///     Utility class for the BepInEx ConfigurationManager plugin, without requiring a hard dependency
    /// </summary>
    public static class ConfigManagerUtils
    {
        /// <summary>
        ///     The ConfigurationManager plugin instance if installed, otherwise null
        /// </summary>
        public static BaseUnityPlugin Plugin { get; private set; }

        private static PropertyInfo displayingWindowInfo;
        private static MethodInfo buildSettingListMethodInfo;
        private static PropertyInfo settingWindowRect;

        static ConfigManagerUtils()
        {
            if (Chainloader.PluginInfos.TryGetValue("com.bepis.bepinex.configurationmanager", out var configManagerInfo) && configManagerInfo.Instance)
            {
                Plugin = configManagerInfo.Instance;
                displayingWindowInfo = AccessTools.Property(Plugin.GetType(), "DisplayingWindow");
                buildSettingListMethodInfo = AccessTools.Method(Plugin.GetType(), "BuildSettingList");
                settingWindowRect = AccessTools.Property(Plugin.GetType(), "SettingWindowRect");
            }
            else if (Chainloader.PluginInfos.TryGetValue("_shudnal.ConfigurationManager", out configManagerInfo) && configManagerInfo.Instance)
            {
                Plugin = configManagerInfo.Instance;
                displayingWindowInfo = AccessTools.Property(Plugin.GetType(), "DisplayingWindow");
                buildSettingListMethodInfo = AccessTools.Method(Plugin.GetType(), "BuildSettingList");
            }
        }

        /// <summary>
        ///     Is the config manager main window displayed on screen<br />
        ///     Safe to use even if ConfigurationManager is not installed.
        /// </summary>
        public static bool DisplayingWindow
        {
            get => Plugin && (bool)displayingWindowInfo.GetValue(Plugin);
            set => displayingWindowInfo?.SetValue(Plugin, value);
        }

        /// <summary>
        ///     The current rect of the config manager window<br />
        ///     Safe to use even if ConfigurationManager is not installed, will return <see cref="Rect.zero">Rect.zero</see> if not installed.
        /// </summary>
        public static Rect SettingWindowRect
        {
            get => Plugin ? (Rect)settingWindowRect.GetValue(Plugin) : Rect.zero;
        }

        /// <summary>
        ///     Rebuild the setting list. Use to update the config manager window if config settings were removed or added while it was open.<br />
        ///     Safe to call even if ConfigurationManager is not installed.
        /// </summary>
        public static void BuildSettingList()
        {
            if (Plugin)
            {
                buildSettingListMethodInfo.Invoke(Plugin, null);
            }
        }
    }
}
