using System.Collections.Generic;
using Jotunn.Entities;
using Jotunn.Managers;

namespace Jotunn.Configs
{
    /// <summary>
    ///     Configuration class for adding custom creatures.<br />
    ///     Use this in a constructor of <see cref="CustomCreature"/> 
    /// </summary>
    public class CreatureConfig
    {
        /// <summary>
        ///     The unique name for your creature, may be tokenized
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        ///     Should this creature be loaded into the world spawn lists, defaults to true
        /// </summary>
        public bool WorldSpawnEnabled { get; set; } = true;

        /// <summary>
        ///     Biome to spawn in, multiple Biomes can be allowed with <see cref="ZoneManager.AnyBiomeOf"/>
        /// </summary>
        public Heightmap.Biome Biome { get; set; }

        /// <summary>
        ///     BiomeArea to spawn in, defaults to Everything
        /// </summary>
        public Heightmap.BiomeArea BiomeArea { get; set; } = Heightmap.BiomeArea.Everything;

        /// <summary>
        ///     Total number of instances, defaults to 1
        /// </summary>
        public int MaxSpawned { get; set; } = 1;

        /// <summary>
        ///     Spawning interval, defaults to 4
        /// </summary>
        public float SpawnInterval { get; set; } = 4f;

        /// <summary>
        ///     Spawn chance each spawn interval, defaults to 100
        /// </summary>
        public float SpawnChance { get; set; } = 100f;

        /// <summary>
        ///     Minimum distance to another instance, defaults to 10f
        /// </summary>
        public float SpawnDistance { get; set; } = 10f;

        /// <summary>
        ///     Minimun spawn range (0 = use global setting)
        /// </summary>
        public float MinSpawnRadius { get; set; }

        /// <summary>
        ///     Maximum spawn range
        /// </summary>
        public float MaxSpawnRadius { get; set; }

        /// <summary>
        ///     Only spawn if this key is set, see Jotunn.Utils.GameConstants.GlobalKey for constant values
        /// </summary>
        public string RequiredGlobalKey { get; set; }

        /// <summary>
        ///     Only spawn if this environment is active, see Jotunn.Utils.GameConstants.Weather for constant values
        /// </summary>
        public List<string> RequiredEnvironments { get; set; }

        /// <summary>
        ///     Minimum group size that can spawn, defaults to 1
        /// </summary>
        public int MinGroupSize { get; set; } = 1;

        /// <summary>
        ///     Maximum group size that can spawn, defaults to 1
        /// </summary>
        public int MaxGroupSize { get; set; } = 1;

        /// <summary>
        ///     Radius for the group to spawn in, defaults to 3
        /// </summary>
        public float GroupRadius { get; set; } = 3f;

        /// <summary>
        ///     Creature can spawn at day, defaults to true
        /// </summary>
        public bool SpawnAtDay { get; set; } = true;

        /// <summary>
        ///     Creature can spawn at night, defaults to true
        /// </summary>
        public bool SpawnAtNight { get; set; } = true;

        /// <summary>
        ///     The minimum altitude for the creature to spawn, defaults to -1000
        /// </summary>
        public float MinAltitude { get; set; } = -1000f;

        /// <summary>
        ///     The maximum altitude for the creature to spawn, defaults to 1000
        /// </summary>
        public float MaxAltitude { get; set; } = 1000f;

        /// <summary>
        ///     The minimum tilt for the creature to spawn
        /// </summary>
        public float MinTilt { get; set; }

        /// <summary>
        ///     The maximum altitude for the creature to spawn, defaults to 35
        /// </summary>
        public float MaxTilt { get; set; } = 35f;

        /// <summary>
        ///     Spawn can happen in forest areas, defaults to true
        /// </summary>
        public bool SpawnInForest { get; set; } = true;

        /// <summary>
        ///     Spawn can happen outside forest areas, defaults to true
        /// </summary>
        public bool SpawnOutsideForest { get; set; } = true;

        /// <summary>
        ///     The minimum ocean depth for the creature to spawn
        /// </summary>
        public float MinOceanDepth { get; set; }

        /// <summary>
        ///     The maximum ocean depth for the creature to spawn
        /// </summary>
        public float MaxOceanDepth { get; set; }

        /// <summary>
        ///     Set true to let the AI hunt the player on spawn
        /// </summary>
        public bool HuntPlayer { get; set; }

        /// <summary>
        ///     Offset to the ground the creature spawns on, defaults to 0.5f
        /// </summary>
        public float GroundOffset { get; set; } = 0.5f;

        /// <summary>
        ///     Minimum level the creature spawns with, defaults to 1
        /// </summary>
        public int MinLevel { get; set; } = 1;

        /// <summary>
        ///     Maximum level the creature spawns with, defaults to 1
        /// </summary>
        public int MaxLevel { get; set; } = 1;

        /// <summary>
        ///     Converts the CreatureConfig to a Valheim style <see cref="SpawnSystem.SpawnData"/> without name and prefab set.
        /// </summary>
        /// <returns>The Valheim <see cref="SpawnSystem.SpawnData"/></returns>
        public SpawnSystem.SpawnData GetSpawnData()
        {
            return new SpawnSystem.SpawnData
            {
                m_enabled = WorldSpawnEnabled,
                m_biome = Biome,
                m_biomeArea = BiomeArea,
                m_maxSpawned = MaxSpawned,
                m_spawnInterval = SpawnInterval,
                m_spawnChance = SpawnChance,
                m_spawnDistance = SpawnDistance,
                m_spawnRadiusMin = MinSpawnRadius,
                m_spawnRadiusMax = MaxSpawnRadius,
                m_requiredGlobalKey = RequiredGlobalKey,
                m_requiredEnvironments = RequiredEnvironments,
                m_groupSizeMin = MinGroupSize,
                m_groupSizeMax = MaxGroupSize,
                m_spawnAtNight = SpawnAtNight,
                m_spawnAtDay = SpawnAtDay,
                m_minAltitude = MinAltitude,
                m_maxAltitude = MaxAltitude,
                m_minTilt = MinTilt,
                m_maxTilt = MaxTilt,
                m_inForest = SpawnInForest,
                m_outsideForest = SpawnOutsideForest,
                m_minOceanDepth = MinOceanDepth,
                m_maxOceanDepth = MaxOceanDepth,
                m_huntPlayer = HuntPlayer,
                m_groundOffset = GroundOffset,
                m_minLevel = MinLevel,
                m_maxLevel = MaxLevel
            };
        }
    }
}
