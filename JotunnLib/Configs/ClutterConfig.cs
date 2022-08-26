using System;
using Jotunn.Entities;
using Jotunn.Managers;

namespace Jotunn.Configs
{
    /// <summary>
    ///     Configuration class for adding custom clutter.<br />
    ///     Use this in a constructor of <see cref="CustomClutter"/>
    /// </summary>
    public class ClutterConfig
    {
        /// <summary>
        ///     Whether this clutter gets spawned in the world. Defaults to <c>true</c>.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        ///     Biome to spawn in, multiple Biomes can be allowed with <see cref="ZoneManager.AnyBiomeOf"/>.<br />
        ///     Default to all biomes.
        /// </summary>
        public Heightmap.Biome Biome { get; set; } = (Heightmap.Biome)(-1);

        /// <summary>
        ///     Whether this clutter has an <see cref="InstanceRenderer"/> attached that should be used.
        /// </summary>
        public bool Instanced { get; set; }

        /// <summary>
        ///     The amount of displayed clutter prefabs per patch.
        ///     For high values an <see cref="InstanceRenderer"/> should be used to lower the amount of overall prefabs.
        /// </summary>
        public int Amount { get; set; } = 10;

        /// <summary>
        ///     Whether this clutter should be shown on unmodified ground.
        /// </summary>
        public bool OnUncleared { get; set; } = true;

        /// <summary>
        ///     Whether this clutter should be shown on modified ground.
        /// </summary>
        public bool OnCleared { get; set; }

        /// <summary>
        ///     Minimum random size of the prefab, only active when <see cref="Instanced"/> is used.
        /// </summary>
        public float ScaleMin { get; set; } = 1f;

        /// <summary>
        ///     Maximum random size of the prefab, only active when <see cref="Instanced"/> is used.
        /// </summary>
        public float ScaleMax { get; set; } = 1f;

        /// <summary>
        ///     Maximum terrain tilt in degrees this clutter will be placed on.
        /// </summary>
        public float MaxTilt { get; set; } = 25f;

        /// <summary>
        ///     Minimum terrain height this clutter will be placed on.
        /// </summary>
        public float MinAlt { get; set; }

        /// <summary>
        ///     Maximum terrain height this clutter will be placed on.
        /// </summary>
        public float MaxAlt { get; set; } = 1000f;

        /// <summary>
        ///     Whether the y position will always be at water level.
        ///     Used before <see cref="RandomOffset"/>
        /// </summary>
        public bool SnapToWater { get; set; }

        /// <summary>
        ///     Random y offset of every individual prefab.
        ///     Calculated after <see cref="SnapToWater"/>.
        /// </summary>
        public float RandomOffset { get; set; }

        /// <summary>
        ///     Whether this clutter will be rotated with the underlying terrain.
        ///     Otherwise it will always point straight up.
        /// </summary>
        public bool TerrainTilt { get; set; }

        /// <summary>
        ///     Whether the clutter should check for ocean height.
        /// </summary>
        public bool OceanDepthCheck { get; set; }

        /// <summary>
        ///     Minimum ocean depth that is needed to place this clutter.
        ///     Needs <see cref="OceanDepthCheck"/> to be <c>true</c>.
        /// </summary>
        public float MinOceanDepth { get; set; }

        /// <summary>
        ///     Maximum ocean depth to place this clutter.
        ///     Needs <see cref="OceanDepthCheck"/> to be <c>true</c>.
        /// </summary>
        public float MaxOceanDepth { get; set; }

        /// <summary>
        ///     Whether the clutter should check for forest thresholds.
        /// </summary>
        public bool InForest { get; set; }

        /// <summary>
        ///     Minimum value of the forest fractal:<br/>
        ///         0 - 1: inside the forest<br/>
        ///         1: forest edge<br/>
        ///         1 - infinity: outside the forest
        /// </summary>
        public float ForestThresholdMin { get; set; }

        /// <summary>
        ///     Maximum value of the forest fractal:<br/>
        ///         0 - 1: inside the forest<br/>
        ///         1: forest edge<br/>
        ///         1 - infinity: outside the forest
        /// </summary>
        public float ForestTresholdMax { get; set; } = 1f;

        /// <summary>
        ///     Size of a noise map used to determine if the clutter should be placed.
        ///     Set to 0 to disable and place it everywhere.
        /// </summary>
        public float FractalScale { get; set; }

        /// <summary>
        ///     Offset of the noise map.
        /// </summary>
        public float FractalOffset { get; set; }

        /// <summary>
        ///     Minimum value of the noise map that is needed to place the clutter.
        /// </summary>
        public float FractalThresholdMin { get; set; } = 0.5f;

        /// <summary>
        ///     Maximum value of the noise map to place the clutter.
        /// </summary>
        public float FractalThresholdMax { get; set; } = 1f;

        /// <summary>
        ///     Create a new <see cref="ClutterConfig"/>
        /// </summary>
        public ClutterConfig() {}

        /// <summary>
        ///     Create a copy of the <see cref="ClutterSystem.Clutter"/>
        /// </summary>
        /// <param name="clutter"></param>
        public ClutterConfig(ClutterSystem.Clutter clutter)
        {
            Enabled = clutter.m_enabled;
            Biome = clutter.m_biome;
            Instanced = clutter.m_instanced;
            Amount = clutter.m_amount;
            OnUncleared = clutter.m_onUncleared;
            OnCleared = clutter.m_onCleared;
            ScaleMin = clutter.m_scaleMin;
            ScaleMax = clutter.m_scaleMax;
            MaxTilt = clutter.m_maxTilt;
            MinAlt = clutter.m_minAlt;
            MaxAlt = clutter.m_maxAlt;
            SnapToWater = clutter.m_snapToWater;
            RandomOffset = clutter.m_randomOffset;
            TerrainTilt = clutter.m_terrainTilt;
            OceanDepthCheck = Math.Abs(clutter.m_minOceanDepth - clutter.m_maxOceanDepth) > 0.001f;
            MinOceanDepth = clutter.m_minOceanDepth;
            MaxOceanDepth = clutter.m_maxOceanDepth;
            InForest = clutter.m_inForest;
            ForestThresholdMin = clutter.m_forestTresholdMin;
            ForestTresholdMax = clutter.m_forestTresholdMax;
            FractalScale = clutter.m_fractalScale;
            FractalOffset = clutter.m_fractalOffset;
            FractalThresholdMin = clutter.m_fractalTresholdMin;
            FractalThresholdMax = clutter.m_fractalTresholdMax;
        }

        internal ClutterSystem.Clutter ToClutter()
        {
            return new ClutterSystem.Clutter()
            {
                m_enabled = Enabled,
                m_biome = Biome,
                m_instanced = Instanced,
                m_amount = Amount,
                m_onUncleared = OnUncleared,
                m_onCleared = OnCleared,
                m_scaleMin = ScaleMin,
                m_scaleMax = ScaleMax,
                m_maxTilt = MaxTilt,
                m_minAlt = MinAlt,
                m_maxAlt = MaxAlt,
                m_snapToWater = SnapToWater,
                m_randomOffset = RandomOffset,
                m_terrainTilt = TerrainTilt,
                m_minOceanDepth = OceanDepthCheck ? MinOceanDepth : 0,
                m_maxOceanDepth = OceanDepthCheck ? MaxOceanDepth : 0,
                m_inForest = InForest,
                m_forestTresholdMin = ForestThresholdMin,
                m_forestTresholdMax = ForestTresholdMax,
                m_fractalScale = FractalScale,
                m_fractalOffset = FractalOffset,
                m_fractalTresholdMin = FractalThresholdMin,
                m_fractalTresholdMax = FractalThresholdMax,
            };
        }
    }
}
