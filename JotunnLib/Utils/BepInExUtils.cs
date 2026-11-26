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
        ///     Assemblies without a registered plugin, with the number of registered plugins at the time.
        ///     A miss is only trusted while that number is unchanged: BepInEx loads a plugin's assembly before it
        ///     registers the plugin, so code running in between (static initializers, preloader hooks) gets a miss
        ///     that stops being true one step later.
        /// </summary>
        private static Dictionary<Assembly, int> AssemblyMissCache { get; } = new Dictionary<Assembly, int>();

        /// <summary>
        ///     Assemblies already checked for an unregistered plugin when falling back to Jotunn, so it is warned about once.
        /// </summary>
        private static HashSet<Assembly> FallbackCheckedAssemblies { get; } = new HashSet<Assembly>();

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
        /// <returns>The plugin's <see cref="PluginInfo"/>, or null if no registered plugin lives in that assembly (yet)</returns>
        public static PluginInfo GetPluginInfoFromAssembly(Assembly assembly)
        {
            if (AssemblyToPluginInfoCache.TryGetValue(assembly, out var pluginInfo))
            {
                return pluginInfo;
            }

            int registeredPlugins = BepInEx.Bootstrap.Chainloader.PluginInfos.Count;
            if (AssemblyMissCache.TryGetValue(assembly, out int registeredAtMiss) && registeredAtMiss == registeredPlugins)
            {
                return null;
            }

            foreach (var info in BepInEx.Bootstrap.Chainloader.PluginInfos.Values)
            {
                if (assembly.GetType(GetPluginInfoTypeName(info)) != null)
                {
                    AssemblyToPluginInfoCache[assembly] = info;
                    AssemblyMissCache.Remove(assembly);
                    return info;
                }
            }

            AssemblyMissCache[assembly] = registeredPlugins;
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
                   FallbackToJotunn(callingType);
        }

        private static BepInPlugin FallbackToJotunn(Type callingType)
        {
            // A mod calling Jotunn before BepInEx registered its plugin is silently attributed to Jotunn, RPC names
            // included, which breaks networking between machines that disagree. Say so once, to the mod's author.
            Assembly assembly = callingType?.Assembly;
            if (assembly != null && FallbackCheckedAssemblies.Add(assembly))
            {
                Type pluginType = GetDeclaredPluginType(assembly);
                if (pluginType != null)
                {
                    string caller = callingType == pluginType
                        ? $"Plugin {pluginType.FullName} used Jotunn before BepInEx registered it"
                        : $"{callingType.FullName} used Jotunn before BepInEx registered its plugin {pluginType.FullName}";
                    Logger.LogWarning($"{caller}, so what it adds now is attributed to Jotunn instead of that mod. This is usually " +
                                      "a static field initializer on the plugin class calling a Jotunn manager; please move that call into Awake.");
                }
            }

            return Main.Instance.Info.Metadata;
        }

        private static Type GetDeclaredPluginType(Assembly assembly)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types;
            }
            catch (Exception)
            {
                return null;
            }

            return types.FirstOrDefault(type => type != null && type.IsDefined(typeof(BepInPlugin), false));
        }

        private static IEnumerable<BaseUnityPlugin> GetLoadedPlugins()
        {
            return BepInEx.Bootstrap.Chainloader.PluginInfos
                          .Where(x => x.Value != null && x.Value.Instance != null)
                          .Select(x => x.Value.Instance);
        }
    }
}
