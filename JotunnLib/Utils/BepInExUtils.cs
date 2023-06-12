using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;

namespace Jotunn.Utils
{
    /// <summary>
    ///     Helper methods to access BepInEx plugin information
    /// </summary>
    public static class BepInExUtils
    {
        /// <summary>
        ///     Cached plugin list
        /// </summary>
        private static BaseUnityPlugin[] Plugins;

        private static Dictionary<PluginInfo, string> PluginInfoTypeNameCache { get; } = new Dictionary<PluginInfo, string>();
        private static Dictionary<Assembly, PluginInfo> AssemblyToPluginInfoCache { get; } = new Dictionary<Assembly, PluginInfo>();
        private static Dictionary<Type, PluginInfo> TypeToPluginInfoCache { get; } = new Dictionary<Type, PluginInfo>();

        /// <summary>
        ///     Cache loaded plugins which depend on Jotunn.
        /// </summary>
        /// <returns></returns>
        private static BaseUnityPlugin[] CacheDependentPlugins()
        {
            var dependent = new List<BaseUnityPlugin>();

            foreach (var plugin in GetLoadedPlugins())
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

            return dependent.ToArray();
        }

        /// <summary>
        ///     Get a dictionary of loaded plugins which depend on Jotunn.
        /// </summary>
        /// <returns>Dictionary of plugin GUID and <see cref="BaseUnityPlugin"/></returns>
        public static Dictionary<string, BaseUnityPlugin> GetDependentPlugins(bool includeJotunn = false)
        {
            if (Plugins == null)
            {
                if (ReflectionHelper.GetPrivateField<bool>(typeof(BepInEx.Bootstrap.Chainloader), "_loaded"))
                {
                    Plugins = CacheDependentPlugins();
                }
                else
                {
                    return new Dictionary<string, BaseUnityPlugin>();
                }
            }

            return Plugins
                   .Where(plugin => includeJotunn || plugin.Info.Metadata.GUID != Main.ModGuid)
                   .ToDictionary(plugin => plugin.Info.Metadata.GUID);
        }

        /// <summary>
        ///     Get a dictionary of all plugins loaded by BepInEx
        /// </summary>
        /// <returns>Dictionary of plugin GUID and <see cref="BaseUnityPlugin"/></returns>
        public static Dictionary<string, BaseUnityPlugin> GetPlugins(bool includeJotunn = false)
        {
            return GetLoadedPlugins()
                   .Where(plugin => includeJotunn || plugin.Info.Metadata.GUID != Main.ModGuid)
                   .ToDictionary(plugin => plugin.Info.Metadata.GUID);
        }

        /// <summary>
        ///     Get <see cref="PluginInfo"/> from a <see cref="Type"/>
        /// </summary>
        /// <param name="type"><see cref="Type"/> of the plugin main class</param>
        /// <returns></returns>
        public static PluginInfo GetPluginInfoFromType(Type type)
        {
            if (TypeToPluginInfoCache.TryGetValue(type, out var pluginInfo))
            {
                return pluginInfo;
            }

            foreach (var info in BepInEx.Bootstrap.Chainloader.PluginInfos.Values)
            {
                var typeName = ReflectionHelper.GetPrivateProperty<string>(info, "TypeName");
                if (typeName.Equals(type.FullName))
                {
                    TypeToPluginInfoCache[type] = info;
                    return info;
                }
            }

            return null;
        }

        private static string GetPluginInfoTypeName(PluginInfo info)
        {
            if (PluginInfoTypeNameCache.TryGetValue(info, out var typeName))
            {
                return typeName;
            }

            typeName = ReflectionHelper.GetPrivateProperty<string>(info, "TypeName");
            PluginInfoTypeNameCache.Add(info, typeName);
            return typeName;
        }

        /// <summary>
        ///     Get <see cref="PluginInfo"/> from an <see cref="Assembly"/>
        /// </summary>
        /// <param name="assembly"><see cref="Assembly"/> of the plugin</param>
        /// <returns></returns>
        public static PluginInfo GetPluginInfoFromAssembly(Assembly assembly)
        {
            if (AssemblyToPluginInfoCache.TryGetValue(assembly, out var pluginInfo))
            {
                return pluginInfo;
            }

            foreach (var info in BepInEx.Bootstrap.Chainloader.PluginInfos.Values)
            {
                if (assembly.GetType(GetPluginInfoTypeName(info)) != null)
                {
                    AssemblyToPluginInfoCache[assembly] = info;
                    return info;
                }
            }

            AssemblyToPluginInfoCache[assembly] = null;
            return null;
        }

        /// <summary>
        ///     Get <see cref="PluginInfo"/> from a path, also matches subfolder paths
        /// </summary>
        /// <param name="fileInfo"><see cref="FileInfo"/> object of the plugin path</param>
        /// <returns></returns>
        public static PluginInfo GetPluginInfoFromPath(FileInfo fileInfo) =>
            BepInEx.Bootstrap.Chainloader.PluginInfos.Values
                .Where(pi => pi.Location != null)
                .FirstOrDefault(pi =>
                    fileInfo.DirectoryName != null &&
                    fileInfo.DirectoryName.Contains(new FileInfo(pi.Location).DirectoryName) &&
                    new FileInfo(pi.Location).DirectoryName != BepInEx.Paths.PluginPath);

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

        private static IEnumerable<BaseUnityPlugin> GetLoadedPlugins()
        {
            return BepInEx.Bootstrap.Chainloader.PluginInfos
                          .Where(x => x.Value != null && x.Value.Instance != null)
                          .Select(x => x.Value.Instance);
        }
    }
}
