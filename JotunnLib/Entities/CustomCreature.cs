using System.Collections.Generic;
using System.Linq;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace Jotunn.Entities
{
    /// <summary>
    ///     Main interface for adding custom creatures to the game.<br />
    ///     All custom creatures have to be wrapped inside this class to add it to Jötunns <see cref="CreatureManager"/>.
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
        ///     Indicator if references from configs should get replaced
        /// </summary>
        internal bool FixConfig { get; set; }

        /// <summary>
        ///     Custom creature from a prefab with a <see cref="SpawnConfig"/> attached.
        /// </summary>
        /// <param name="creaturePrefab">The prefab of this custom creature.</param>
        /// <param name="fixReference">If true references for <see cref="Entities.Mock{T}"/> objects get resolved at runtime by Jötunn.</param>
        /// <param name="creatureConfig">The <see cref="CreatureConfig"/> for this custom creature.</param>
        public CustomCreature(GameObject creaturePrefab, bool fixReference, CreatureConfig creatureConfig)
        {
            Prefab = creaturePrefab;
            creatureConfig.Apply(creaturePrefab);

            if (creatureConfig.DropConfigs.Any())
            {
                FixConfig = true;
            }

            Spawns = creatureConfig.GetSpawns().ToList();
            foreach (var spawnData in Spawns)
            {
                spawnData.m_prefab = creaturePrefab;
            }

            FixReference = fixReference;
        }

        /// <summary>
        ///     Custom creature created as a copy of a vanilla Valheim creature.<br />
        ///     SpawnData is not cloned, you will have to add <see cref="SpawnConfig">SpawnConfigs</see> to your <see cref="CreatureConfig"/>
        ///     if you want to spawn the cloned creature automatically.
        /// </summary>
        /// <param name="name">The new name of the creature after cloning.</param>
        /// <param name="basePrefabName">The name of the base prefab the custom creature is cloned from.</param>
        /// <param name="creatureConfig">The <see cref="CreatureConfig"/> for this custom creature.</param>
        public CustomCreature(string name, string basePrefabName, CreatureConfig creatureConfig)
        {
            var creaturePrefab = PrefabManager.Instance.CreateClonedPrefab(name, basePrefabName);
            if (creaturePrefab)
            {
                Prefab = creaturePrefab;
                creatureConfig.Name = name;
                creatureConfig.Apply(creaturePrefab);

                if (creatureConfig.DropConfigs.Any())
                {
                    FixConfig = true;
                }

                Spawns = creatureConfig.GetSpawns().ToList();
                foreach (var spawnData in Spawns)
                {
                    spawnData.m_prefab = creaturePrefab;
                }
            }
        }

        /// <summary>
        ///     Checks if a custom creature is valid (i.e. has a prefab and all required components).
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

            var required = new[]
            {
                typeof(Character), typeof(BaseAI), typeof(CapsuleCollider), typeof(Rigidbody),
                typeof(ZSyncAnimation)
            };
            foreach (var type in required)
            {
                if (!Prefab.GetComponent(type))
                {
                    Logger.LogError($"CustomCreature {this} has no {type} component");
                    valid = false;
                    break;
                }
            }
            if (!Prefab.GetComponentInChildren<Animator>())
            {
                Logger.LogError($"CustomCreature {this} has no Animator component");
                valid = false;
            }
            if (!Prefab.GetComponentInChildren<CharacterAnimEvent>())
            {
                Logger.LogError($"CustomCreature {this} has no CharacterAnimEvent component");
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
