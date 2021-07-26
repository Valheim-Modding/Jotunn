using System.Collections.Generic;
using System.Linq;
using BepInEx;

namespace Jotunn.Utils
{
    internal class BepInExUtils
    {
        /// <summary>
        ///     Cached plugin list
        /// </summary>
        private static BaseUnityPlugin[] plugins;

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

            plugins = dependent.ToArray();
        }

        /// <summary>
        ///     Get a dictionary of loaded plugins which depend on Jotunn.
        /// </summary>
        /// <returns></returns>
        internal static Dictionary<string, BaseUnityPlugin> GetDependentPlugins(bool includeJotunn = false)
        {
            var result = new Dictionary<string, BaseUnityPlugin>();

            if (plugins == null)
            {
                CacheDependentPlugins();
            }

            foreach (var plugin in plugins)
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
    }
}
