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
        ///     Array of <see cref="DropConfig">DropConfigs</see> to use for this creature's <see cref="CharacterDrop"/>.
        ///     A <see cref="CharacterDrop"/> component will automatically be added if not present.
        ///     The drop table of existing components will be replaced.
        /// </summary>
        public DropConfig[] DropConfigs = Array.Empty<DropConfig>();

        /// <summary>
        ///     Array of <see cref="SpawnConfig">SpawnConfigs</see> used for world spawns of your custom creature.
        ///     Leave empty if you don't want your creature to spawn in the world automatically.
        /// </summary>
        public SpawnConfig[] SpawnConfigs = Array.Empty<SpawnConfig>();

        /// <summary>
        ///     Converts the <see cref="DropConfig">DropConfigs</see> to Valheim style <see cref="CharacterDrop.Drop"/> array.
        /// </summary>
        /// <returns>The Valheim <see cref="CharacterDrop.Drop"/> array</returns>
        public CharacterDrop.Drop[] GetDrops()
        {
            CharacterDrop.Drop[] drops = new CharacterDrop.Drop[DropConfigs.Length];

            for (int i = 0; i < drops.Length; i++)
            {
                drops[i] = DropConfigs[i].GetDrop();
            }

            return drops;
        }

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
