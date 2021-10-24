using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jotunn.Configs
{
    public class LocationConfig
    {
        public bool ClearArea;

        public Location Location { get; internal set; }
        public Heightmap.Biome Biome { get; set; }
        public Heightmap.BiomeArea BiomeArea { get; set; } = Heightmap.BiomeArea.Everything;
        public bool Priotized { get; set; }
        public int Quantity { get; set; }
        public float ChanceToSpawn { get; set; }
        public float ExteriorRadius { get; set; }


        public ZoneSystem.ZoneLocation GetZoneLocation()
        {
            return new ZoneSystem.ZoneLocation
            {
                m_enable = true,
                m_biome = Biome, 
                m_prioritized = Priotized,
                m_quantity = Quantity,
                m_biomeArea = BiomeArea,
                m_chanceToSpawn = ChanceToSpawn,
                m_exteriorRadius = ExteriorRadius, 
            };
        }

    }
}
