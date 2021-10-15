using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;

namespace Jotunn.Utils
{
    internal class BepInExUtils
    {
        /// <summary>
        ///     Cached plugin list
        /// </summary>
        private static BaseUnityPlugin[] Plugins;

        /// <summary>
        ///     Cache loaded plugins which depend on Jotunn.
        /// </summary>
        /// <returns></returns>
        private static void CacheDependentPlugins()
        {
            var dependent = new List<BaseUnityPlugin>();
            var loaded = BepInEx.Bootstrap.Chainloader.PluginInfos.Where(x => x.Value != null && x.Value.Instance != null).Select(x => x.Value.Instance);

            foreach (var plugin in loaded)
            {
                if (plugin.Info == null)
                {
                    Logger.LogWarning($"Plugin without Info found: {plugin.GetType().Assembly.FullName}");
                    continue;
                }
                if (plugin.Info.Metadata == null)
                {
                    Logger.LogWarning($"Plugin without Metadata found: {plugin.GetType().Assembly.FullName}");
                    continue;
                }

                if (plugin.Info.Metadata.GUID == Main.ModGuid)
                {
                    dependent.Add(plugin);
                    continue;
                }

                foreach (var dependencyAttribute in plugin.GetType().GetCustomAttributes(typeof(BepInDependency), false).Cast<BepInDependency>())
                {
                    if (dependencyAttribute.DependencyGUID == Main.ModGuid)
                    {
                        dependent.Add(plugin);
                    }
                }
            }

            Plugins = dependent.ToArray();
        }

        /// <summary>
        ///     Get a dictionary of loaded plugins which depend on Jotunn.
        /// </summary>
        /// <returns>Dictionary of plugin GUID and <see cref="BaseUnityPlugin"/></returns>
        public static Dictionary<string, BaseUnityPlugin> GetDependentPlugins(bool includeJotunn = false)
        {
            var result = new Dictionary<string, BaseUnityPlugin>();

            if (Plugins == null && ReflectionHelper.GetPrivateField<bool>(typeof(BepInEx.Bootstrap.Chainloader), "_loaded"))
            {
                CacheDependentPlugins();
            }

            foreach (var plugin in Plugins)
            {
                if (plugin.Info.Metadata.GUID == Main.ModGuid)
                {
                    if (includeJotunn)
                    {
                        result.Add(plugin.Info.Metadata.GUID, plugin);
                    }
                    continue;
                }

                result.Add(plugin.Info.Metadata.GUID, plugin);
            }

            return result;
        }

        /// <summary>
        ///     Get <see cref="PluginInfo"/> from a <see cref="Type"/>
        /// </summary>
        /// <param name="type"><see cref="Type"/> of the plugin main class</param>
        /// <returns></returns>
        public static PluginInfo GetPluginInfoFromType(Type type)
        {
            foreach (var info in BepInEx.Bootstrap.Chainloader.PluginInfos.Values)
            {
                var typeName = ReflectionHelper.GetPrivateProperty<string>(info, "TypeName");
                if (typeName.Equals(type.FullName))
                {
                    return info;
                }
            }

            return null;
        }

        /// <summary>
        ///     Get <see cref="PluginInfo"/> from an <see cref="Assembly"/>
        /// </summary>
        /// <param name="assembly"><see cref="Assembly"/> of the plugin</param>
        /// <returns></returns>
        public static PluginInfo GetPluginInfoFromAssembly(Assembly assembly)
        {
            foreach (var info in BepInEx.Bootstrap.Chainloader.PluginInfos.Values)
            {
                var typeName = ReflectionHelper.GetPrivateProperty<string>(info, "TypeName");
                if (assembly.GetType(typeName) != null)
                {
                    return info;
                }
            }

            return null;
        }

        /// <summary>
        ///     Get <see cref="PluginInfo"/> from a path, also matches subfolder paths
        /// </summary>
        /// <param name="fileInfo"><see cref="FileInfo"/> object of the plugin path</param>
        /// <returns></returns>
        public static PluginInfo GetPluginInfoFromPath(FileInfo fileInfo) =>
            BepInEx.Bootstrap.Chainloader.PluginInfos.Values.FirstOrDefault(pi =>
                fileInfo.DirectoryName != null && pi.Location.Contains(fileInfo.DirectoryName));

        /// <summary>
        ///     Get metadata information from the current calling mod
        /// </summary>
        /// <returns></returns>
        public static BepInPlugin GetSourceModMetadata()
        {
            Type callingType = ReflectionHelper.GetCallingType();

            return GetPluginInfoFromType(callingType)?.Metadata ??
                   GetPluginInfoFromAssembly(callingType.Assembly)?.Metadata ??
                   Main.Instance.Info.Metadata;
        }
    }
}
