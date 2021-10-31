using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace Jotunn.Entities
{
    /// <summary>
    ///     Main interface for adding custom vegetation to the game.<br />
    ///     All custom vegetation have to be wrapped inside this class to add it to Jötunns <see cref="ZoneManager"/>.
    /// </summary>
    public class CustomVegetation : CustomEntity
    {

        /// <summary>
        ///     The prefab for this custom vegetation.
        /// </summary>
        public GameObject Prefab { get; private set; }
        /// <summary>
        ///     Associated <see cref="ZoneSystem.ZoneVegetation"/> component
        /// </summary>
        public ZoneSystem.ZoneVegetation Vegetation { get; private set; }
        /// <summary>
        ///     Name of this custom vegetation
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        ///     Custom vegetation from a prefab.<br />
        /// </summary>
        /// <param name="prefab">The prefab for this custom vegetation.</param>
        /// <param name="config">The vegetation config for this custom vegation.</param> 
        public CustomVegetation(GameObject prefab, VegetationConfig config)
        {
            Prefab = prefab;
            Name = prefab.name;
            Vegetation = config.ToVegetation();
            Vegetation.m_prefab = prefab;
        }
    }
}
