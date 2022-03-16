using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
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
        ///     Unity "character" layer ID. 
        /// </summary>
        public static int CharacterLayer = LayerMask.NameToLayer("character");

        /// <summary>
        ///     Event that gets fired after the vanilla creatures are in memory and available for cloning.
        ///     Your code will execute every time before a new <see cref="ObjectDB"/> is copied (on every menu start).
        ///     If you want to execute just once you will need to unregister from the event after execution.
        /// </summary>
        public static event Action OnVanillaCreaturesAvailable;

        /// <summary>
        ///     Event that gets fired after registering all custom creatures to <see cref="ZNetScene"/>.
        ///     Your code will execute every time a new ZNetScene is created (on every game start).
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
            SpawnListContainer = new GameObject("Creatures");
            SpawnListContainer.transform.parent = Main.RootObject.transform;
            SpawnListContainer.SetActive(false);
            SpawnList = SpawnListContainer.AddComponent<SpawnSystemList>();

            Main.Harmony.PatchAll(typeof(Patches));
        }

        private static class Patches
        {
            [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB)), HarmonyPrefix]
            private static void InvokeOnVanillaCreaturesAvailable() => Instance.InvokeOnVanillaCreaturesAvailable();

            [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake)), HarmonyPostfix]
            private static void FixReferences(ZNetScene __instance) => Instance.FixReferences(__instance);

            [HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.Awake)), HarmonyPrefix]
            private static void AddSpawnListToSpawnSystem(SpawnSystem __instance) => Instance.AddSpawnListToSpawnSystem(__instance);

            [HarmonyPatch(typeof(LevelEffects), nameof(LevelEffects.SetupLevelVisualization)), HarmonyPostfix]
            private static void EnableCumulativeLevelEffects(LevelEffects __instance, int level) => Instance.EnableCumulativeLevelEffects(__instance, level);
        }

        /// <summary>
        ///     Add a <see cref="CustomCreature"/> to the game.<br />
        ///     Checks if the custom creature is valid and unique and adds it to the list of custom creatures.
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

            // Add prefab to the PrefabManager
            if (!PrefabManager.Instance.AddPrefab(customCreature.Prefab, customCreature.SourceMod))
            {
                return false;
            }

            // Set the correct Layer if not set
            if (customCreature.Prefab.layer != CharacterLayer)
            {
                customCreature.Prefab.layer = CharacterLayer;

                // Also set the first level of child GOs
                foreach (Transform child in customCreature.Prefab.transform)
                {
                    child.gameObject.layer = CharacterLayer;
                }
            }

            // Move prefab to our own container
            customCreature.Prefab.transform.SetParent(SpawnListContainer.transform, false);

            // Add custom creature to CreatureManager
            Creatures.Add(customCreature);

            // Add spawners to Jötunn's own spawner list
            SpawnList.m_spawners.AddRange(customCreature.Spawns);
            
            return true;
        }

        /// <summary>
        ///     Get a custom creature by its name.
        /// </summary>
        /// <param name="creatureName">Name of the custom creature to search.</param>
        /// <returns>The <see cref="CustomCreature"/> if found.</returns>
        public CustomCreature GetCreature(string creatureName)
        {
            return Creatures.FirstOrDefault(x => x.Prefab.name.Equals(creatureName));
        }

        /// <summary>
        ///     Get a custom or vanilla creature prefab by its name.
        /// </summary>
        /// <param name="creatureName">Name of the creature to search.</param>
        /// <returns>The prefab of the creature if found.</returns>
        public GameObject GetCreaturePrefab(string creatureName)
        {
            var custom = GetCreature(creatureName);
            if (custom != null)
            {
                return custom.Prefab;
            }

            var vanilla = PrefabManager.Cache.GetPrefab<Character>(creatureName);
            if (vanilla != null)
            {
                return vanilla.gameObject;
            }

            return null;
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
        ///     Remove a custom creature by its ref. Removes the custom recipe, too.
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

        /// <summary>
        ///     Safely invoke the <see cref="OnVanillaCreaturesAvailable"/> event
        /// </summary>
        /// 
        private void InvokeOnVanillaCreaturesAvailable()
        {
            OnVanillaCreaturesAvailable?.SafeInvoke();
        }

        /// <summary>
        ///     Resolve mocks of all custom creatures if necessary.
        /// </summary>
        private void FixReferences(ZNetScene self)
        {
            if (Creatures.Any())
            {
                Logger.LogInfo($"Adding {Creatures.Count} custom creatures");

                List<CustomCreature> toDelete = new List<CustomCreature>();

                foreach (var customCreature in Creatures)
                {
                    try
                    {
                        // Always try to fix the physics material component of the capsule collider
                        customCreature.Prefab.GetComponent<CapsuleCollider>()?.FixReferences();

                        // Fix other mock references
                        if (customCreature.FixReference | customCreature.FixConfig)
                        {
                            customCreature.Prefab.FixReferences(customCreature.FixReference);
                            customCreature.FixReference = false;
                            customCreature.FixConfig = false;
                        }

                        Logger.LogDebug($"Added creature {customCreature} | Spawns: {customCreature.Spawns.Count}");
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

                // Invoke event that prefabs have been registered
                InvokeOnCreaturesRegistered();
            }
        }

        /// <summary>
        ///     Safely invoke the <see cref="OnCreaturesRegistered"/> event.
        /// </summary>
        private void InvokeOnCreaturesRegistered()
        {
            OnCreaturesRegistered?.SafeInvoke();
        }

        /// <summary>
        ///     Add the internal <see cref="SpawnSystemList"/> to the awoken spawner if not already added.
        /// </summary>
        private void AddSpawnListToSpawnSystem(SpawnSystem self)
        {
            if (!self.m_spawnLists.Contains(SpawnList))
            {
                self.m_spawnLists.Add(SpawnList);
            }
        }

        /// <summary>
        ///     Enable cumulative level effects for custom creatures requesting it. Thx ASP for the code.
        /// </summary>
        private void EnableCumulativeLevelEffects(LevelEffects self, int level)
        {
            if (level <= 2)
            {
                return;
            }

            if (!Creatures.Any(x => x.Prefab.name == self.m_character.m_nview.GetPrefabName() && x.UseCumulativeLevelEffects))
            {
                return;
            }
            
            for (int index = level - 2; index >= 0; --index)
            {
                if (index >= self.m_levelSetups.Count)
                {
                    continue;
                }

                var levelSetup = self.m_levelSetups[index];

                if (levelSetup.m_enableObject)
                {
                    Logger.LogDebug($"Enabling {(level - 1)} star equipment: '{levelSetup.m_enableObject.name}'");
                    levelSetup.m_enableObject.SetActive(true);
                }
            }
        }
    }
}
