using System.Collections.Generic;

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
        public float ChanceToSpawn { get; set; } = 10f;
        public float ExteriorRadius { get; set; } = 10f;
        public bool CenterFirst { get; set; }
        public float ForestTresholdMin { get; set; }
        public float ForestTrasholdMax { get; set; } = 1f;
        public bool Unique { get; set; }
        public float MinAltitude { get; set; } = -1000f;
        public float MaxAltitude { get; set; } = 1000f;
        public float MinDistance { get; set; }
        public float MaxDistance { get; set; }
        public string Group { get; private set; } = "";
        public bool InForest { get; private set; }
        public float MinTerrainDelta { get; private set; }
        public float MaxTerrainDelta { get; private set; } = 2f;
        public float MinDistanceFromSimilar { get; private set; }
        public float InteriorRadius { get; private set; } = 10f;
        public bool SlopeRotation { get; private set; }
        public bool RandomRotation { get; private set; } = true;
        public bool SnapToWater { get; private set; }
        public bool IconPlaced { get; private set; }
        public bool IconAlways { get; private set; }

        public LocationConfig() { }

        public LocationConfig(ZoneSystem.ZoneLocation zoneLocation)
        {
            Biome = zoneLocation.m_biome;
            BiomeArea = zoneLocation.m_biomeArea;
            Priotized = zoneLocation.m_prioritized;
            Quantity = zoneLocation.m_quantity;
            ChanceToSpawn = zoneLocation.m_chanceToSpawn;
            ExteriorRadius = zoneLocation.m_exteriorRadius;
            ForestTresholdMin = zoneLocation.m_forestTresholdMin;
            ForestTrasholdMax = zoneLocation.m_forestTresholdMax;
            MinDistance = zoneLocation.m_minDistance;
            MaxDistance = zoneLocation.m_maxDistance;
            MinAltitude = zoneLocation.m_minAltitude;
            MaxAltitude = zoneLocation.m_maxAltitude;
            Group = zoneLocation.m_group;
            InForest = zoneLocation.m_inForest;
            MinTerrainDelta = zoneLocation.m_minTerrainDelta;
            MaxTerrainDelta = zoneLocation.m_maxTerrainDelta;
            MinDistanceFromSimilar = zoneLocation.m_minDistanceFromSimilar;
            InteriorRadius = zoneLocation.m_interiorRadius;
            SlopeRotation = zoneLocation.m_slopeRotation;
            RandomRotation = zoneLocation.m_randomRotation;
            SnapToWater = zoneLocation.m_snapToWater;
            IconPlaced = zoneLocation.m_iconPlaced;
            IconAlways = zoneLocation.m_iconAlways;
        }

        public ZoneSystem.ZoneLocation GetZoneLocation()
        {
            return new ZoneSystem.ZoneLocation
            {
                m_biome = Biome,
                m_biomeArea = BiomeArea,
                m_quantity = Quantity,
                m_prioritized = Priotized,
                m_chanceToSpawn = ChanceToSpawn,
                m_exteriorRadius = ExteriorRadius,
                m_centerFirst = CenterFirst,
                m_forestTresholdMin = ForestTresholdMin,
                m_forestTresholdMax = ForestTrasholdMax,
                m_unique = Unique,
                m_minAltitude = MinAltitude,
                m_maxAltitude = MaxAltitude,
                m_minDistance = MinDistance,
                m_maxDistance = MaxDistance,
                m_group = Group,
                m_inForest = InForest,
                m_minTerrainDelta = MinTerrainDelta,
                m_maxTerrainDelta = MaxTerrainDelta,
                m_minDistanceFromSimilar = MinDistanceFromSimilar,
                m_interiorRadius = InteriorRadius,
                m_slopeRotation = SlopeRotation,
                m_randomRotation = RandomRotation, 
                m_snapToWater = SnapToWater,
                m_iconPlaced = IconPlaced,
                m_iconAlways = IconAlways
            };
        }

    }
}
