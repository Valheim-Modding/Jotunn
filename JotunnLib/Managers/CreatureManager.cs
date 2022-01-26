using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Entities;
using UnityEngine;

namespace Jotunn.Managers
{
    /// <summary>
    ///    Manager for handling all custom data added to the game related to creatures.
    /// </summary>
    public class CreatureManager : IManager
    {
        private static CreatureManager _instance;

        /// <summary>
        ///     The singleton instance of this manager.
        /// </summary>
        public static CreatureManager Instance => _instance ??= new CreatureManager();

        /// <summary>
        ///     Hide .ctor
        /// </summary>
        private CreatureManager() { }

        /// <summary>
        ///     Event that gets fired after all Creatures were added to the ObjectDB.
        ///     Your code will execute every time a new ObjectDB is created (on every game start).
        ///     If you want to execute just once you will need to unregister from the event after execution.
        /// </summary>
        public static event Action OnCreaturesRegistered;

        /// <summary>
        ///     Internal lists of all custom entities added
        /// </summary>
        internal readonly List<CustomCreature> Creatures = new List<CustomCreature>();

        /// <summary>
        ///     Container for Jötunn's SpawnSystemList in the DontDestroyOnLoad scene.
        /// </summary>
        internal GameObject SpawnListContainer;

        /// <summary>
        ///     Reference to the SpawnList component of the container.
        /// </summary>
        internal SpawnSystemList SpawnList;

        /// <summary>
        ///     Creates the spawner container and registers all hooks.
        /// </summary>
        public void Init()
        {
            SpawnListContainer = new GameObject("Spawns");
            SpawnListContainer.transform.parent = Main.RootObject.transform;
            SpawnListContainer.SetActive(false);
            SpawnList = SpawnListContainer.AddComponent<SpawnSystemList>();

            On.ZNetScene.Awake += ResolveMocks;
            On.SpawnSystem.Awake += AddSpawnListToSpawnSystem;
        }

        /// <summary>
        ///     Add a <see cref="CustomCreature"/> to the game.<br />
        ///     Checks if the custom creature is valid and unique and adds it to the list of custom creatures.<br />
        ///     Also adds the prefab of the custom ´creature to the <see cref="PrefabManager"/>.
        /// </summary>
        /// <param name="customCreature">The custom Creature to add.</param>
        /// <returns>true if the custom Creature was added to the manager.</returns>
        public bool AddCreature(CustomCreature customCreature)
        {
            if (!customCreature.IsValid())
            {
                Logger.LogWarning($"Custom creature {customCreature} is not valid");
                return false;
            }
            if (Creatures.Contains(customCreature))
            {
                Logger.LogWarning($"Custom creature {customCreature} already added");
                return false;
            }

            // Add prefab to PrefabManager
            PrefabManager.Instance.AddPrefab(customCreature.Prefab, customCreature.SourceMod);

            // Add custom Creature to CreatureManager
            Creatures.Add(customCreature);

            // Add spawners to Jötunn's own spawner list
            SpawnList.m_spawners.AddRange(customCreature.Spawns);

            return true;
        }

        /// <summary>
        ///     Get a custom Creature by its name.
        /// </summary>
        /// <param name="creatureName">Name of the creature to search.</param>
        /// <returns></returns>
        public CustomCreature GetCreature(string creatureName)
        {
            return Creatures.FirstOrDefault(x => x.Prefab.name.Equals(creatureName));
        }

        /// <summary>
        ///     Remove a custom creature by its name.
        /// </summary>
        /// <param name="creatureName">Name of the creature to remove.</param>
        public void RemoveCreature(string creatureName)
        {
            var creature = GetCreature(creatureName);
            if (creature == null)
            {
                Logger.LogWarning($"Could not remove Creature {creatureName}: Not found");
                return;
            }

            RemoveCreature(creature);
        }

        /// <summary>
        ///     Remove a custom Creature by its ref. Removes the custom recipe, too.
        /// </summary>
        /// <param name="creature"><see cref="CustomCreature"/> to remove.</param>
        public void RemoveCreature(CustomCreature creature)
        {
            Creatures.Remove(creature);

            if (creature.Prefab)
            {
                PrefabManager.Instance.RemovePrefab(creature.Prefab.name);
            }
        }
        
        private void ResolveMocks(On.ZNetScene.orig_Awake orig, ZNetScene self)
        {
            orig(self);

            if (Creatures.Any())
            {
                Logger.LogInfo($"Adding {Creatures.Count} custom creatures to the ZNetScene");

                List<CustomCreature> toDelete = new List<CustomCreature>();

                foreach (var customCreature in Creatures)
                {
                    try
                    {
                        if (customCreature.FixReference)
                        {
                            customCreature.Prefab.FixReferences(true);
                            customCreature.FixReference = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Error caught while adding creature {customCreature}: {ex}");
                        toDelete.Add(customCreature);
                    }
                }

                // Delete custom creatures with errors
                foreach (var creature in toDelete)
                {
                    if (creature.Prefab)
                    {
                        PrefabManager.Instance.DestroyPrefab(creature.Prefab.name);
                    }
                    RemoveCreature(creature);
                }
            }
        }

        private void AddSpawnListToSpawnSystem(On.SpawnSystem.orig_Awake orig, SpawnSystem self)
        {
            if (!self.m_spawnLists.Contains(SpawnList))
            {
                self.m_spawnLists.Add(SpawnList);
            }
            
            orig(self);
        }
    }
}
