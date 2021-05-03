using System.Collections.Generic;
using System.Linq;
using BepInEx;

namespace Jotunn.Utils
{
    internal class BepInExUtils
    {
        /// <summary>
        ///     Get a dictionary of loaded plugins which depend on Jotunn.
        /// </summary>
        /// <returns></returns>
        internal static Dictionary<string, BaseUnityPlugin> GetDependentPlugins(bool includeJotunn = false)
        {
            var result = new Dictionary<string, BaseUnityPlugin>();

            var plugins = BepInEx.Bootstrap.Chainloader.PluginInfos.Where(x => x.Value != null && x.Value.Instance != null).Select(x => x.Value.Instance).ToArray();

            foreach (var plugin in plugins)
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

                if (includeJotunn && plugin.Info.Metadata.GUID == Main.ModGuid)
                {
                    result.Add(plugin.Info.Metadata.GUID, plugin);
                    continue;
                }

                foreach (var dependencyAttribute in plugin.GetType().GetCustomAttributes(typeof(BepInDependency), false).Cast<BepInDependency>())
                {
                    if (dependencyAttribute.DependencyGUID == Main.ModGuid)
                    {
                        result.Add(plugin.Info.Metadata.GUID, plugin);
                    }
                }
            }

            return result;
        }
    }
}
