using System;
using Jotunn.Entities;

namespace Jotunn.Configs
{
    /// <summary>
    ///     Configuration class for adding custom creature spawns.<br />
    ///     Use this in a constructor of <see cref="CustomCreature"/> 
    /// </summary>
    public class CreatureConfig
    {
        /// <summary>
        ///     The unique name for your custom creature. May be tokenized.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        ///     Array of <see cref="SpawnConfig">SpawnConfigs</see> used for world spawns of your custom creature.
        /// </summary>
        public SpawnConfig[] SpawnConfigs = Array.Empty<SpawnConfig>();

        /// <summary>
        ///     Converts the <see cref="SpawnConfig">SpawnConfigs</see> to Valheim style <see cref="SpawnSystem.SpawnData"/> array.
        /// </summary>
        /// <returns>The Valheim <see cref="SpawnSystem.SpawnData"/> array</returns>
        public SpawnSystem.SpawnData[] GetSpawns()
        {
            SpawnSystem.SpawnData[] spawns = new SpawnSystem.SpawnData[SpawnConfigs.Length];

            for (int i = 0; i < spawns.Length; i++)
            {
                spawns[i] = SpawnConfigs[i].GetSpawnData();
            }

            return spawns;
        }
    }
}
