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
        public GameObject Prefab { get; private set; }
        public ZoneSystem.ZoneVegetation Vegetation { get; private set; }
        public string Name { get; internal set; }

        public CustomVegetation(GameObject prefab, VegetationConfig config)
        {
            Prefab = prefab;
            Name = prefab.name;
            Vegetation = config.ToVegetation();
            Vegetation.m_prefab = prefab;
        }
    }
}
