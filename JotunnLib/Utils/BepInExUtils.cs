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
        internal static Dictionary<string, BaseUnityPlugin> GetDependentPlugins()
        {
            var result = new Dictionary<string, BaseUnityPlugin>();

            var plugins = UnityEngine.Object.FindObjectsOfType(typeof(BaseUnityPlugin)).Cast<BaseUnityPlugin>().ToArray();

            foreach (var plugin in plugins)
            {
                foreach (var attrib in plugin.GetType().GetCustomAttributes(typeof(BepInDependency), false).Cast<BepInDependency>())
                {
                    if (attrib.DependencyGUID == Main.ModGuid)
                    {
                        result.Add(plugin.Info.Metadata.GUID, plugin);
                    }
                }
            }

            return result;
        }
    }
}