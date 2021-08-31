using System.Collections.Generic;
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

            if (Plugins == null)
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
        ///     Get a dependent plugin by its <see cref="Assembly"/>.
        /// </summary>
        /// <param name="assembly">Assembly that plugin was loaded from</param>
        /// <returns><see cref="BaseUnityPlugin"/> information for that plugin</returns>
        public static BaseUnityPlugin GetDependentPlugin(Assembly assembly)
        {
            if (Plugins == null)
            {
                CacheDependentPlugins();
            }

            return Plugins.FirstOrDefault(plugin => plugin.GetType().Assembly == assembly);
        }
    }
}
