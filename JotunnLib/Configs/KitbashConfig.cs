using System.Collections.Generic;
using Jotunn;

namespace Jotunn.Configs
{
    /// <summary>
    ///     Kitbash configuration for a prefab
    /// </summary>
    public class KitbashConfig
    {
        /// <summary>
        ///     A list of KitbashSourceConfigs to apply to the prefab
        /// </summary>
        public List<KitbashSourceConfig> KitbashSources = new List<KitbashSourceConfig>();

        /// <summary>
        ///     The layer of the prefab, all Kitbashed parts will be set to this layer
        /// </summary>
        public string Layer { get; set; }

        /// <summary>
        ///     Whether to <see cref="PrefabExtension.FixReferences(UnityEngine.GameObject)">fix references</see> on the prefab
        /// </summary>
        public bool FixReferences { get; internal set; }
    }
}
