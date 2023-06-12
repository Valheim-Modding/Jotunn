using System.Reflection;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace Jotunn.Entities
{
    /// <summary>
    ///     Main interface for adding custom clutter to the game.<br />
    ///     Clutter are client side only objects scattered on the ground.<br />
    ///     All custom clutter have to be wrapped inside this class to add it to Jötunns <see cref="ZoneManager"/>.
    /// </summary>
    public class CustomClutter : CustomEntity
    {
        /// <summary>
        ///     The prefab for this custom clutter.
        /// </summary>
        public GameObject Prefab { get; }

        /// <summary>
        ///     Associated <see cref="ClutterSystem.Clutter"/> class.
        /// </summary>
        public ClutterSystem.Clutter Clutter { get; }

        /// <summary>
        ///     Name of this custom clutter.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Indicator if references from <see cref="Entities.Mock{T}"/>s will be replaced at runtime.
        /// </summary>
        public bool FixReference { get; set; }

        /// <summary>
        ///     Custom clutter from a prefab.<br />
        ///     Can fix references for mocks.
        /// </summary>
        /// <param name="prefab">The prefab for this custom clutter.</param>
        /// <param name="fixReference">If true references for <see cref="Entities.Mock{T}"/> objects get resolved at runtime by Jötunn.</param>
        /// <param name="config">The <see cref="ClutterConfig"/> for this custom vegation.</param>
        public CustomClutter(GameObject prefab, bool fixReference, ClutterConfig config) : base(Assembly.GetCallingAssembly())
        {
            Prefab = prefab;
            Name = prefab.name;
            Clutter = config.ToClutter();
            Clutter.m_prefab = prefab;
            FixReference = fixReference;
        }

        /// <summary>
        ///     Custom clutter from a prefab loaded from an <see cref="AssetBundle"/>.<br />
        ///     Can fix references for mocks.
        /// </summary>
        /// <param name="assetBundle">A preloaded <see cref="AssetBundle"/></param>
        /// <param name="assetName">Name of the prefab in the bundle.</param>
        /// <param name="fixReference">If true references for <see cref="Entities.Mock{T}"/> objects get resolved at runtime by Jötunn.</param>
        /// <param name="config">The <see cref="ClutterConfig"/> for this custom clutter.</param>
        public CustomClutter(AssetBundle assetBundle, string assetName, bool fixReference, ClutterConfig config) : base(Assembly.GetCallingAssembly())
        {
            var prefab = assetBundle.LoadAsset<GameObject>(assetName);
            if (prefab)
            {
                Prefab = prefab;
                Name = prefab.name;
                Clutter = config.ToClutter();
                Clutter.m_prefab = prefab;
                FixReference = fixReference;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Name;
        }
    }
}
