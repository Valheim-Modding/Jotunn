using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace Jotunn.Entities
{
    /// <summary>
    ///     Main interface for adding custom creatures to the game.<br />
    ///     All custom creatures have to be wrapped inside this class to add it to Jötunns <see cref="ZoneManager"/>.
    /// </summary>
    public class CustomCreature : CustomEntity
    {
        /// <summary>
        ///     The creature prefab
        /// </summary>
        public GameObject Prefab { get; }

        /// <summary>
        ///     Name of this custom creature
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Associated <see cref="SpawnSystem.SpawnData"/> component
        /// </summary>
        public SpawnSystem.SpawnData SpawnData { get; }

        /// <summary>
        ///     Indicator if references from <see cref="Entities.Mock{T}"/>s will be replaced at runtime.
        /// </summary>
        public bool FixReference { get; set; }

        /// <summary>
        ///     Custom location from a prefab with a <see cref="LocationConfig"/> attached.
        /// </summary>
        /// <param name="creaturePrefab">The exterior prefab for this custom location.</param>
        /// <param name="fixReference">If true references for <see cref="Entities.Mock{T}"/> objects get resolved at runtime by Jötunn.</param>
        /// <param name="creatureConfig">The <see cref="LocationConfig"/> for this custom location.</param>
        public CustomCreature(GameObject creaturePrefab, bool fixReference, CreatureConfig creatureConfig)
        {
            Prefab = creaturePrefab;
            Name = creaturePrefab.name;
            SpawnData = creatureConfig.GetSpawnData();
            SpawnData.m_prefab = creaturePrefab;
            SpawnData.m_name = creaturePrefab.name;
            FixReference = fixReference;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Name;
        }
    }
}
