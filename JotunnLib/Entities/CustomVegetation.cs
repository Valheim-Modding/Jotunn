using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Jotunn.Entities
{
    public class CustomVegetation : CustomEntity
    {
        public GameObject Prefab { get; set; }
        public Heightmap.Biome Biome { get; set; }
        public Heightmap.BiomeArea BiomeArea { get; set; } = Heightmap.BiomeArea.Everything;  
        public bool BlockCheck { get; set; }

        internal ZoneSystem.ZoneVegetation ToVegetation()
        {
            return new ZoneSystem.ZoneVegetation
            {
                m_biome = Biome,
                m_biomeArea = BiomeArea,
                m_enable = true,
                m_prefab = Prefab,
                m_blockCheck = BlockCheck,
                
            };
        }
    }
}
