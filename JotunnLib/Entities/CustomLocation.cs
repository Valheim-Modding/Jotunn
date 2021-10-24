using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jotunn.Configs;
using UnityEngine;

namespace Jotunn.Entities
{
    public class CustomLocation: CustomEntity
    { 
        public GameObject Prefab { get; set; }
        public ZoneSystem.ZoneLocation ZoneLocation { get; private set; }
        public Location Location { get; private set; }

        public CustomLocation(GameObject prefab, LocationConfig config)
        {
            Prefab = prefab;

            ZoneLocation = config.GetZoneLocation();
            ZoneLocation.m_prefab = prefab;
            ZoneLocation.m_prefabName = prefab.name;

            List<ZNetView> netViews = new List<ZNetView>();
            var transform = prefab.transform;
            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).TryGetComponent(out ZNetView zNetView))
                {
                    netViews.Add(zNetView);
                }
            }

            ZoneLocation.m_netViews = netViews;

            Location = prefab.AddComponent<Location>();
            Location.m_exteriorRadius = config.ExteriorRadius;
            Location.m_clearArea = config.ClearArea;
        } 
    }
}
