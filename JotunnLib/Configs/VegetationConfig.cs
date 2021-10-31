using Jotunn.Entities;

namespace Jotunn.Configs
{
    /// <summary>
    ///     Configuration class for adding custom vegetation.<br />
    ///     Use this in a constructor of <see cref="CustomVegetation"/> and 
    /// </summary>
    public class VegetationConfig
    {
        /// <summary>
        ///     Biome to spawn in, can be bitwise or'ed toghether to allow multiple Biomes (Heightmap.Biome.Meadows | Heightmap.Biome.Forest)
        /// </summary>
        public Heightmap.Biome Biome { get; set; }
        /// <summary>
        ///     BiomeArea to spawn in, defaults to Everything
        /// </summary>
        public Heightmap.BiomeArea BiomeArea { get; set; } = Heightmap.BiomeArea.Everything;
        /// <summary>
        ///     Do a check before placing that there is nothing already from layers: "Default", "static_solid", "Default_small", "piece"
        /// </summary>
        public bool BlockCheck { get; set; } = true;
        /// <summary>
        ///     Unlike what the name suggets, Valheim will attempt 50 times the normal amount of placements, almost guaranteeing that everything will be placed
        /// </summary>
        public bool ForcePlacement { get; set; }
        /// <summary>
        ///     Minimum amount per zone
        /// </summary>
        public float Min { get; set; }
        /// <summary>
        ///     Values between 0 - 1 are used as a chance to place
        ///     Values above 1 are used as integer amount of maximum per zone
        /// </summary>
        public float Max { get; set; } = 10;
        /// <summary>
        ///     Minimal altitude of the vegetation
        /// </summary>
        public float MinAltitude { get; set; } = -1000;
        /// <summary>
        ///     Maximum altitude of the vegetation
        /// </summary>
        public float MaxAltitude { get; set; } = 1000;
        /// <summary>
        ///     Minimum ocean depth
        /// </summary>
        public float MinOceanDepth { get; set; }
        /// <summary>
        ///     Maximum ocean depth
        /// </summary>
        public float MaxOceanDepth { get; set; }
        /// <summary>
        ///     Minimum terrain delta (difference between min and max height) in the circle defined by <see cref="TerrainDeltaRadius"/>
        /// </summary>
        public float MinTerrainDelta { get; set; }
        /// <summary>
        ///     Maximum terrain delta (difference between min and max height) in the circle defined by <see cref="TerrainDeltaRadius"/>
        /// </summary>
        public float MaxTerrainDelta { get; set; } = 2;
        /// <summary>
        ///     Minimum tilt in degrees
        /// </summary>
        public float MinTilt { get; set; }
        /// <summary>
        ///     Maximum tilt in degrees
        /// </summary>
        public float MaxTilt { get; set; } = 90;
        /// <summary>
        ///     Enable to check forest thresholds against the forest fractal.
        /// </summary>
        public bool InForest { get; set; }
        /// <summary>
        ///     Minimum value of the forest fractal:
        ///         0: outside the forest
        ///         1: inside forest
        /// </summary>
        public float ForestThresholdMin { get; set; }
        /// <summary>
        ///     Maximum value of the forest fractal:
        ///         0: outside the forest
        ///         1: inside forest
        /// </summary>
        public float ForestThresholdMax { get; set; } = 1;
        /// <summary>
        ///     Size of the circle used to determine terrain delta
        /// </summary>
        public float TerrainDeltaRadius { get; set; }
        /// <summary>
        ///     Minimum scale of placed instances
        /// </summary>
        public float ScaleMin { get; set; } = 1;
        /// <summary>
        ///     Maximum scale of place instances
        /// </summary>
        public float ScaleMax { get; set; } = 1;
        /// <summary>
        ///     Minimum amount in group
        /// </summary>
        public int GroupSizeMin { get; set; } = 1;
        /// <summary>
        ///     Maximum amount in group
        /// </summary>
        public int GroupSizeMax { get; set; } = 1;
        /// <summary>
        ///     Radius of group
        /// </summary>
        public float GroupRadius { get; set; }
        /// <summary>
        ///     Placement offset, use negatives to bury underground
        /// </summary>
        public float GroundOffset { get; set; }
        /// <summary>
        ///     Create a new <see cref="VegetationConfig"/>
        /// </summary>
        public VegetationConfig() { }
        /// <summary>
        ///     Create a copy of the <see cref="ZoneSystem.ZoneVegetation"/>
        /// </summary>
        /// <param name="zoneVegetation">ZoneVegetation to copy</param>
        public VegetationConfig(ZoneSystem.ZoneVegetation zoneVegetation)
        {
            Biome = zoneVegetation.m_biome;
            BiomeArea = zoneVegetation.m_biomeArea;
            Min = zoneVegetation.m_min;
            Max = zoneVegetation.m_max;
            BlockCheck = zoneVegetation.m_blockCheck;
            ForcePlacement = zoneVegetation.m_forcePlacement;
            MinAltitude = zoneVegetation.m_minAltitude;
            MaxAltitude = zoneVegetation.m_maxAltitude;
            MinOceanDepth = zoneVegetation.m_minOceanDepth;
            MaxOceanDepth = zoneVegetation.m_maxOceanDepth;
            MinTerrainDelta = zoneVegetation.m_minTerrainDelta;
            MaxTerrainDelta = zoneVegetation.m_maxTerrainDelta;
            TerrainDeltaRadius = zoneVegetation.m_terrainDeltaRadius;
            MinTilt = zoneVegetation.m_minTilt;
            MaxTilt = zoneVegetation.m_maxTilt;
            InForest = zoneVegetation.m_inForest;
            ForestThresholdMin = zoneVegetation.m_forestTresholdMin;
            ForestThresholdMax = zoneVegetation.m_forestTresholdMax;
            ScaleMin = zoneVegetation.m_scaleMin;
            ScaleMax = zoneVegetation.m_scaleMax;
            GroupSizeMin = zoneVegetation.m_groupSizeMin;
            GroupSizeMax = zoneVegetation.m_groupSizeMax;
            GroupRadius = zoneVegetation.m_groupRadius;
            GroundOffset = zoneVegetation.m_groundOffset;
        }

        internal ZoneSystem.ZoneVegetation ToVegetation()
        {
            return new ZoneSystem.ZoneVegetation
            {
                m_biome = Biome,
                m_biomeArea = BiomeArea,
                m_enable = true,
                m_min = Min,
                m_max = Max,
                m_blockCheck = BlockCheck,
                m_forcePlacement = ForcePlacement,
                m_minAltitude = MinAltitude,
                m_maxAltitude = MaxAltitude,
                m_minOceanDepth = MinOceanDepth,
                m_maxOceanDepth = MaxOceanDepth,
                m_minTerrainDelta = MinTerrainDelta,
                m_maxTerrainDelta = MaxTerrainDelta,
                m_terrainDeltaRadius = TerrainDeltaRadius,
                m_minTilt = MinTilt,
                m_maxTilt = MaxTilt,
                m_inForest = InForest,
                m_forestTresholdMin = ForestThresholdMin,
                m_forestTresholdMax = ForestThresholdMax,
                m_scaleMin = ScaleMin,
                m_scaleMax = ScaleMax,
                m_groupSizeMin = GroupSizeMin,
                m_groupSizeMax = GroupSizeMax,
                m_groupRadius = GroupRadius,
                m_groundOffset = GroundOffset
            };
        }
    }
}
