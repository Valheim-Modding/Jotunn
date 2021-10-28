using System.Collections.Generic;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace Jotunn.Entities
{

    /// <summary>
    ///     Main interface for adding custom locations to the game.<br />
    ///     All custom locations have to be wrapped inside this class to add it to Jötunns <see cref="ZoneManager"/>.
    /// </summary>
    public class CustomLocation : CustomEntity
    {
        /// <summary>
        ///     The exterior prefab for this custom location.
        /// </summary>
        public GameObject Prefab { get; private set; }

        public ZoneSystem.ZoneLocation ZoneLocation { get; private set; }
        public Location Location { get; private set; }
        public string Name { get; private set; }

        public CustomLocation(GameObject exteriorLocation, LocationConfig config) : this(exteriorLocation, null, config) { }

        public CustomLocation(GameObject exteriorLocation, GameObject interiorPrefab, LocationConfig config)
        {
            Prefab = exteriorLocation;
            Name = exteriorLocation.name;

            ZoneLocation = config.GetZoneLocation();
            ZoneLocation.m_prefab = exteriorLocation;
            ZoneLocation.m_prefabName = exteriorLocation.name;

            List<ZNetView> netViews = new List<ZNetView>();
            var transform = exteriorLocation.transform;
            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).TryGetComponent(out ZNetView zNetView))
                {
                    netViews.Add(zNetView);
                }
            }

            ZoneLocation.m_netViews = netViews;

            if (exteriorLocation.TryGetComponent<Location>(out var location))
            {
                Location = location;
            }
            else
            {
                Location = exteriorLocation.AddComponent<Location>();
                Location.m_hasInterior = config.InteriorRadius > 0f;
                Location.m_exteriorRadius = config.ExteriorRadius;
                Location.m_clearArea = config.ClearArea;
                Location.m_interiorPrefab = interiorPrefab;
            }
        }
    }
}
