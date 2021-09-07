using System;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace Jotunn.Entities
{
    /// <summary>
    ///     Container class for Kitbashed prefabs, returned by <see cref="KitbashManager"/>
    /// </summary>
    public class KitbashObject : CustomEntity
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

        /// <inheritdoc/>
        public override string ToString()
        {
            return Prefab ? Prefab.name : base.ToString();
        }
    }
}
