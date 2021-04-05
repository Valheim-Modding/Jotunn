using System.Collections.Generic;
using System.Linq;
using BepInEx;

namespace JotunnLib.Utils
{
    public class BepInExUtils
    {
        /// <summary>
        ///     Get a dictionary of loaded plugins which depend on JotunnLib
        /// </summary>
        /// <returns></returns>
        internal static Dictionary<string, BaseUnityPlugin> GetDependentPlugins(bool includeJotunnLib = false)
        {
            var result = new Dictionary<string, BaseUnityPlugin>();

            var plugins = BepInEx.Bootstrap.Chainloader.PluginInfos.Select(x => x.Value.Instance).ToArray();

            foreach (var plugin in plugins)
            {
                if (includeJotunnLib && plugin.Info.Metadata.GUID == Main.ModGuid)
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