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
        ///     Associated <see cref="SpawnSystem.SpawnData"/>
        /// </summary>
        public SpawnSystem.SpawnData SpawnData { get; }

        /// <summary>
        ///     Indicator if references from <see cref="Entities.Mock{T}"/>s will be replaced at runtime.
        /// </summary>
        public bool FixReference { get; set; }

        /// <summary>
        ///     Custom creature from a prefab with a <see cref="CreatureConfig"/> attached.
        /// </summary>
        /// <param name="creaturePrefab">The prefab of this custom creature.</param>
        /// <param name="fixReference">If true references for <see cref="Entities.Mock{T}"/> objects get resolved at runtime by Jötunn.</param>
        /// <param name="creatureConfig">The <see cref="CreatureConfig"/> for this custom creature.</param>
        public CustomCreature(GameObject creaturePrefab, bool fixReference, CreatureConfig creatureConfig)
        {
            Prefab = creaturePrefab;
            SpawnData = creatureConfig.GetSpawnData();
            SpawnData.m_prefab = creaturePrefab;
            SpawnData.m_name = creaturePrefab.name;
            FixReference = fixReference;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Prefab.name;
        }
    }
}
