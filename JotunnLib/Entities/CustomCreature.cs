using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        public List<SpawnSystem.SpawnData> Spawns { get; } = new List<SpawnSystem.SpawnData>();

        /// <summary>
        ///     Indicator if references from <see cref="Entities.Mock{T}">mocks</see> will be replaced at runtime.
        /// </summary>
        public bool FixReference { get; set; }

        /// <summary>
        ///     Indicator if references from configs should get replaced
        /// </summary>
        internal bool FixConfig { get; set; }

        /// <summary>
        ///     Internal flag for the cumulative level effects hook. Value is set in the config.
        /// </summary>
        internal bool UseCumulativeLevelEffects { get; set; }

        /// <summary>
        ///     Custom creature from a prefab.
        /// </summary>
        /// <param name="creaturePrefab">The prefab of this custom creature.</param>
        /// <param name="fixReference">If true references for <see cref="Entities.Mock{T}"/> objects get resolved at runtime by Jötunn.</param>
        public CustomCreature(GameObject creaturePrefab, bool fixReference) : base(Assembly.GetCallingAssembly())
        {
            Prefab = creaturePrefab;
            FixReference = fixReference;
        }

        /// <summary>
        ///     Custom creature from a prefab with a <see cref="CreatureConfig"/> attached.
        /// </summary>
        /// <param name="creaturePrefab">The prefab of this custom creature.</param>
        /// <param name="fixReference">If true references for <see cref="Entities.Mock{T}"/> objects get resolved at runtime by Jötunn.</param>
        /// <param name="creatureConfig">The <see cref="CreatureConfig"/> for this custom creature.</param>
        public CustomCreature(GameObject creaturePrefab, bool fixReference, CreatureConfig creatureConfig) : base(Assembly.GetCallingAssembly())
        {
            Prefab = creaturePrefab;
            ApplyCreatureConfig(creatureConfig);
            FixReference = fixReference;
        }

        /// <summary>
        ///     Custom creature created as a copy of a vanilla Valheim creature.<br />
        ///     SpawnData is not cloned, you will have to add <see cref="SpawnConfig">SpawnConfigs</see>
        ///     to your <see cref="CreatureConfig"/> if you want to spawn the cloned creature automatically.
        /// </summary>
        /// <param name="name">The new name of the creature after cloning.</param>
        /// <param name="basePrefabName">The name of the base prefab the custom creature is cloned from.</param>
        /// <param name="creatureConfig">The <see cref="CreatureConfig"/> for this custom creature.</param>
        public CustomCreature(string name, string basePrefabName, CreatureConfig creatureConfig) : base(Assembly.GetCallingAssembly())
        {
            var vanilla = CreatureManager.Instance.GetCreaturePrefab(basePrefabName);
            if (vanilla)
            {
                Prefab = PrefabManager.Instance.CreateClonedPrefab(name, vanilla);
                creatureConfig.Name = name;
                ApplyCreatureConfig(creatureConfig);
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
                typeof(Character),
                typeof(BaseAI),
                typeof(CapsuleCollider),
                typeof(Rigidbody),
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

        /// <summary>
        ///     Helper method to determine if a prefab with a given name is a custom creature created with Jötunn.
        /// </summary>
        /// <param name="prefabName">Name of the prefab to test.</param>
        /// <returns>true if the prefab is added as a custom creature to the <see cref="CreatureManager"/>.</returns>
        public static bool IsCustomCreature(string prefabName)
        {
            return CreatureManager.Instance.Creatures.Any(x => x.Prefab.name == prefabName);
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

        private void ApplyCreatureConfig(CreatureConfig creatureConfig)
        {
            creatureConfig.Apply(Prefab);

            FixConfig = creatureConfig.DropConfigs.Any() || creatureConfig.Consumables.Any();
            UseCumulativeLevelEffects = creatureConfig.UseCumulativeLevelEffects;

            Spawns.AddRange(creatureConfig.GetSpawns());
            foreach (var spawnData in Spawns)
            {
                spawnData.m_prefab = Prefab;
            }
        }
    }
}
