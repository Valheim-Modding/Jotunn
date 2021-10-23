using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Jotunn.Entities
{
    public class CustomLocation: CustomEntity
    { 
        public GameObject Prefab { get; set; } 
        public Location Location { get; internal set; }
        public Heightmap.Biome Biome { get; set; }
        public Heightmap.BiomeArea BiomeArea { get; set; } = Heightmap.BiomeArea.Everything;
        public bool Priotized { get; set; }
        public int Quantity { get; set; }
        public float ChanceToSpawn { get; set; }
        public float ExteriorRadius { get; set; }

        public ZoneSystem.ZoneLocation ToZoneLocation()
        {
            List<ZNetView> netViews = new List<ZNetView>();
            var transform = Prefab.transform;
            for (int i = 0; i < transform.childCount; i++)
            {
                if(transform.GetChild(i).TryGetComponent(out ZNetView zNetView)) {
                    netViews.Add(zNetView);
                }
            }

            return new ZoneSystem.ZoneLocation
            {
                m_enable = true,
                m_biome = Biome,
                m_prefabName = Prefab.name,
                m_prefab = Prefab,
                m_prioritized = Priotized,
                m_quantity = Quantity,
                m_biomeArea = BiomeArea,
                m_chanceToSpawn = ChanceToSpawn,
                m_exteriorRadius = ExteriorRadius,
                m_netViews = netViews    
            };
        } 
         
    }
}
