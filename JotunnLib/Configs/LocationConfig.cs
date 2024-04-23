using System;
using Jotunn.Entities;
using Jotunn.Managers;

namespace Jotunn.Configs
{
    /// <summary>
    ///     Configuration class for adding custom locations.<br />
    ///     Use this in a constructor of <see cref="CustomLocation"/> and 
    /// </summary>
    public class LocationConfig
    {
        /// <summary>
        ///     Associated <see cref="Location"/> component
        /// </summary>
        public Location Location { get; internal set; }
        /// <summary>
        ///     Biome to spawn in, multiple Biomes can be allowed with <see cref="ZoneManager.AnyBiomeOf"/>
        /// </summary>
        public Heightmap.Biome Biome { get; set; }
        /// <summary>
        ///     BiomeArea to spawn in, defaults to Everything
        /// </summary>
        public Heightmap.BiomeArea BiomeArea { get; set; } = Heightmap.BiomeArea.Everything;
        /// <summary>
        ///     Enable to place this kind of location first, and make it twice as likely that the objects will all be placed (random samples increase from 100,000 to 200,000) 
        /// </summary>
        public bool Priotized { get; set; }
        /// <summary>
        ///     Upper limit on how many of these locations will be placed in the world
        /// </summary>
        public int Quantity { get; set; }
        /// <summary>
        ///     Unused in Valheim, but available in case that changes in the future
        /// </summary>
        [Obsolete("This property is unused by Valheim.")]
        public float ChanceToSpawn { get; set; } = 10f;
        /// <summary>
        ///     Radius of the location. Terrain delta is calculated within this circle.
        /// </summary>
        public float ExteriorRadius { get; set; } = 10f;
        /// <summary>
        ///     Attempt to place in the central zone first
        /// </summary>
        public bool CenterFirst { get; set; }
        /// <summary>
        ///     Enable to check forest thresholds against the forest fractal.
        /// </summary>
        public bool InForest { get; set; }
        /// <summary>
        ///     Minimum value of the forest fractal:
        ///         0 - 1: inside the forest
        ///         1: forest edge
        ///         1 - infinity: outside the forest
        /// </summary>
        public float ForestTresholdMin { get; set; }
        /// <summary>
        ///     Maximum value of the forest fractal:
        ///         0 - 1: inside the forest
        ///         1: forest edge
        ///         1 - infinity: outside the forest
        /// </summary>
        public float ForestTrasholdMax { get; set; } = 1f;
        /// <summary>
        ///     Enable to make this location unique, it will not be replaced when locations change
        /// </summary>
        public bool Unique { get; set; }
        /// <summary>
        ///     Minimal altitude of the location
        /// </summary>
        public float MinAltitude { get; set; } = -1000f;
        /// <summary>
        ///     Maximum altitude of the location
        /// </summary>
        public float MaxAltitude { get; set; } = 1000f;
        /// <summary>
        ///     Minimum distance from the center of the map of the location
        /// </summary>
        public float MinDistance { get; set; }
        /// <summary>
        ///     Maximum distance from the center of the map of the location
        /// </summary>
        public float MaxDistance { get; set; }
        /// <summary>
        ///     Group of the location. Used with <see cref="MinDistanceFromSimilar"/>
        /// </summary>
        public string Group { get; set; } = string.Empty;
        /// <summary>
        ///     Minimum distance to a similar location, either the same location or a location with the same <see cref="Group"/>
        /// </summary>
        public float MinDistanceFromSimilar { get; set; }
        /// <summary>
        ///     Minimum terrain delta (difference between min and max height) in the circle defined by <see cref="ExteriorRadius"/>
        /// </summary>
        public float MinTerrainDelta { get; set; }
        /// <summary>
        ///     Maximum terrain delta (difference between min and max height) in the circle defined by <see cref="ExteriorRadius"/>
        /// </summary>
        public float MaxTerrainDelta { get; set; } = 2f;
        /// <summary>
        ///     Rotate towards the average slope of the terrain in the circle defined by <see cref="ExteriorRadius"/>
        /// </summary>
        public bool SlopeRotation { get; set; }
        /// <summary>
        ///     Enable to activate interior handling
        /// </summary>
        public bool HasInterior { get; set; }
        /// <summary>
        ///     Radius of the interior attached to the location
        /// </summary>
        public float InteriorRadius { get; set; }
        /// <summary>
        ///     Environment string used by the interior
        /// </summary>
        public string InteriorEnvironment { get; set; }
        /// <summary>
        ///     Randomize location rotation when placing
        /// </summary>
        public bool RandomRotation { get; set; } = true;
        /// <summary>
        ///     Place at water level
        /// </summary>
        public bool SnapToWater { get; set; }
        /// <summary>
        ///     Enable if the location places an icon to push the location icons
        /// </summary>
        public bool IconPlaced { get; set; }
        /// <summary>
        ///     Always show the associated icon on the minimap
        /// </summary>
        public bool IconAlways { get; set; }
        /// <summary>
        ///     Enable to forbid Vegetation from spawning inside the circle defined by <see cref="ExteriorRadius"/>
        /// </summary>
        public bool ClearArea { get; set; }
        /// <summary>
        ///     Create a new <see cref="LocationConfig"/>
        /// </summary>
        public LocationConfig() { }

        /// <summary>
        ///     Create a copy of the <see cref="ZoneSystem.ZoneLocation"/>
        /// </summary>
        /// <param name="zoneLocation">ZoneLocation to copy</param>
        public LocationConfig(ZoneSystem.ZoneLocation zoneLocation)
        {
            zoneLocation.m_prefab.Load();
            var location = zoneLocation.m_prefab.Asset.GetComponent<Location>();

            Biome = zoneLocation.m_biome;
            BiomeArea = zoneLocation.m_biomeArea;
            Priotized = zoneLocation.m_prioritized;
            Quantity = zoneLocation.m_quantity;
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
            HasInterior = location && location.m_hasInterior;
            InteriorRadius = zoneLocation.m_interiorRadius;
            InteriorEnvironment = location?.m_interiorEnvironment;
            SlopeRotation = zoneLocation.m_slopeRotation;
            RandomRotation = zoneLocation.m_randomRotation;
            SnapToWater = zoneLocation.m_snapToWater;
            IconPlaced = zoneLocation.m_iconPlaced;
            IconAlways = zoneLocation.m_iconAlways;
            ClearArea = location && location.m_clearArea;

            zoneLocation.m_prefab.Release();
        }

        /// <summary>
        ///     Converts the LocationConfig to a Valheim style <see cref="ZoneSystem.ZoneLocation"/>.
        /// </summary>
        /// <returns>The Valheim <see cref="ZoneSystem.ZoneLocation"/></returns>
        public ZoneSystem.ZoneLocation GetZoneLocation()
        {
            return new ZoneSystem.ZoneLocation
            {
                m_biome = Biome,
                m_biomeArea = BiomeArea,
                m_quantity = Quantity,
                m_prioritized = Priotized,
                m_interiorRadius = InteriorRadius,
                m_exteriorRadius = ExteriorRadius,
                m_clearArea = ClearArea,
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
                m_slopeRotation = SlopeRotation,
                m_randomRotation = RandomRotation,
                m_snapToWater = SnapToWater,
                m_iconPlaced = IconPlaced,
                m_iconAlways = IconAlways
            };
        }
    }
}
