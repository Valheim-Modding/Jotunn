using System;
using System.Reflection;
using BepInEx;
using Jotunn.Configs;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace Jotunn.Entities
{
    /// <summary>
    ///     Container class for Kitbashed prefabs, returned by <see cref="KitbashManager"/>
    /// </summary>
    public class KitbashObject
    {
        /// <summary>
        ///     Callback that is called when Kitbashes are applied
        /// </summary>
        public Action OnKitbashApplied;

        /// <summary>
        ///     The Kitbashed prefab
        /// </summary>
        public GameObject Prefab { get; internal set; }

        /// <summary>
        ///     Config for the KitbashObject
        /// </summary>
        public KitbashConfig Config { get; internal set; }

        /// <summary>
        ///     Reference to the <see cref="BepInPlugin"/> which added this kitbash object.
        /// </summary>
        public BepInPlugin SourceMod { get; } =
            BepInExUtils.GetPluginInfoFromAssembly(Assembly.GetCallingAssembly())?.Metadata;

        /// <inheritdoc/>
        public override string ToString()
        {
            if (Prefab)
            {
                return Prefab.name;
            }
            return base.ToString();
        }
    }
}
