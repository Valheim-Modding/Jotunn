using System.Collections.Generic;
using System.Linq;
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
        ///     The creature prefab.
        /// </summary>
        public GameObject Prefab { get; }

        /// <summary>
        ///     Associated list of <see cref="SpawnSystem.SpawnData"/> of the creature.
        /// </summary>
        public List<SpawnSystem.SpawnData> Spawns { get; }

        /// <summary>
        ///     Indicator if references from <see cref="Entities.Mock{T}"/>s will be replaced at runtime.
        /// </summary>
        public bool FixReference { get; set; }

        /// <summary>
        ///     Custom creature from a prefab with a <see cref="SpawnConfig"/> attached.
        /// </summary>
        /// <param name="creaturePrefab">The prefab of this custom creature.</param>
        /// <param name="fixReference">If true references for <see cref="Entities.Mock{T}"/> objects get resolved at runtime by Jötunn.</param>
        /// <param name="creatureConfig">The <see cref="CreatureConfig"/> for this custom creature.</param>
        public CustomCreature(GameObject creaturePrefab, bool fixReference, CreatureConfig creatureConfig)
        {
            Prefab = creaturePrefab;
            if (creaturePrefab.TryGetComponent<Character>(out var character))
            {
                if (!string.IsNullOrEmpty(creatureConfig.Name))
                {
                    character.m_name = creatureConfig.Name;
                }

                if (string.IsNullOrEmpty(character.m_name))
                {
                    character.m_name = creaturePrefab.name;
                }
            }
            Spawns = creatureConfig.GetSpawns().ToList();
            foreach (var spawnData in Spawns)
            {
                spawnData.m_prefab = creaturePrefab;
            }
            FixReference = fixReference;
        }
        
        /// <summary>
        ///     Checks if a custom creature is valid (i.e. has a prefab, a <see cref="Character"/> component and a <see cref="BaseAI"/> component).
        /// </summary>
        /// <returns>true if all criteria is met</returns>
        public bool IsValid()
        {
            bool valid = true;

            if (!Prefab)
            {
                Logger.LogError($"CustomCreature {this} has no prefab");
                valid = false;
            }
            if (!Prefab.GetComponent<Character>())
            {
                Logger.LogError($"CustomCreature {this} has no Character component");
                valid = false;
            }
            if (!Prefab.GetComponent<BaseAI>())
            {
                Logger.LogError($"CustomCreature {this} has no BaseAI component");
                valid = false;
            }

            return valid;
        }
        
        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj.GetHashCode() == GetHashCode();
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Prefab.name.GetStableHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Prefab.name;
        }
    }
}
