using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using UnityEngine;
using Object = UnityEngine.Object;
using ZoneLocation = ZoneSystem.ZoneLocation;

namespace Jotunn.Managers
{
    /// <summary>
    ///     Manager for adding custom Locations and Vegetation.
    /// </summary>
    public class ZoneManager : IManager
    {
        private static ZoneManager _instance;

        /// <summary>
        ///     The singleton instance of this manager.
        /// </summary>
        public static ZoneManager Instance => _instance ??= new ZoneManager();

        /// <summary>
        ///     Hide .ctor
        /// </summary>
        private ZoneManager() { }

        /// <summary>
        ///     Event that gets fired after the vanilla locations are in memory and available for cloning or editing.
        ///     Your code will execute every time before a new <see cref="ObjectDB"/> is copied (on every menu start).
        ///     If you want to execute just once you will need to unregister from the event after execution.
        /// </summary>
        public static event Action OnVanillaLocationsAvailable;

        /// <summary>
        ///     Container for custom locations in the DontDestroyOnLoad scene.
        /// </summary>
        internal GameObject LocationContainer;

        internal readonly Dictionary<string, CustomLocation> Locations = new Dictionary<string, CustomLocation>();
        internal readonly Dictionary<string, CustomVegetation> Vegetations = new Dictionary<string, CustomVegetation>();

        /// <summary>
        ///     Initialize the manager
        /// </summary>
        public void Init()
        {
            LocationContainer = new GameObject("Locations");
            LocationContainer.transform.parent = Main.RootObject.transform;
            LocationContainer.SetActive(false);

            Main.Harmony.PatchAll(typeof(Patches));
        }

        private static class Patches
        {
            [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.SetupLocations)), HarmonyPostfix]
            private static void ZoneSystem_SetupLocations(ZoneSystem __instance) => Instance.ZoneSystem_SetupLocations(__instance);
        }

        /// <summary>
        ///     Return a <see cref="Heightmap.Biome"/> that matches any of the provided Biomes
        /// </summary>
        /// <param name="biomes">Biomes that should match</param> 
#pragma warning disable S3265 // Non-flags enums should not be used in bitwise operations
        public static Heightmap.Biome AnyBiomeOf(params Heightmap.Biome[] biomes)
        {
            Heightmap.Biome result = Heightmap.Biome.None;
            foreach (var biome in biomes)
            {
                result |= biome;
            }
            return result;
        }

        /// <summary>
        ///     Returns a list of all <see cref="Heightmap.Biome"/> that match <paramref name="biome"/>
        /// </summary>
        /// <param name="biome"></param>
        /// <returns></returns>
        public static List<Heightmap.Biome> GetMatchingBiomes(Heightmap.Biome biome)
        {
            List<Heightmap.Biome> biomes = new List<Heightmap.Biome>();
            foreach (Heightmap.Biome area in Enum.GetValues(typeof(Heightmap.Biome)))
            {
                if (area == Heightmap.Biome.BiomesMax || (biome & area) == 0)
                {
                    continue;
                }

                biomes.Add(area);
            }
            return biomes;
        }
#pragma warning restore S3265 // Non-flags enums should not be used in bitwise operations

        /// <summary>
        ///     Create an empty GameObject that is disabled, so any Components in instantiated GameObjects will not start their lifecycle.
        /// </summary>
        /// <param name="name">Name of the location</param>
        /// <returns>Empty and hierarchy disabled GameObject</returns>
        public GameObject CreateLocationContainer(string name)
        {
            GameObject container = new GameObject
            {
                name = name
            };
            container.transform.SetParent(LocationContainer.transform);
            return container;
        }

        /// <summary>
        ///     Create a copy that is disabled, so any Components in instantiated child GameObjects will not start their lifecycle.<br />
        ///     Use this if you plan to alter your location prefab in code after importing it. <br />
        ///     Don't create a separate container if you won't alter the prefab afterwards as it creates a new instance for the container.
        /// </summary>
        /// <param name="gameObject">Instantiated and hierarchy disabled location prefab</param>
        public GameObject CreateLocationContainer(GameObject gameObject)
        {
            var container = Object.Instantiate(gameObject, LocationContainer.transform);
            container.name = gameObject.name;
            return container;
        }

        /// <summary>
        ///     Create a copy that is disabled, so any Components in instantiated GameObjects will not start their lifecycle     
        /// </summary>
        /// <param name="gameObject">Prefab to copy</param>
        /// <param name="fixLocationReferences">Replace JVLmock GameObjects with a copy of their real prefab</param>
        /// <returns></returns>
        [Obsolete("Use CreateLocationContainer(GameObject) instead and define if references should be fixed in CustomLocation")]
        public GameObject CreateLocationContainer(GameObject gameObject, bool fixLocationReferences = false)
        {
            var locationContainer = Object.Instantiate(gameObject, LocationContainer.transform);
            locationContainer.name = gameObject.name;
            if (fixLocationReferences)
            {
                locationContainer.FixReferences(true);
            }

            return locationContainer;
        }

        /// <summary>
        ///     Register a CustomLocation to be added to the ZoneSystem
        /// </summary>
        /// <param name="customLocation"></param>
        /// <returns>true if the custom location could be added to the manager</returns>
        public bool AddCustomLocation(CustomLocation customLocation)
        {
            if (Locations.ContainsKey(customLocation.Name))
            {
                Logger.LogWarning($"Location {customLocation.Name} already exists");
                return false;
            }

            customLocation.Prefab.transform.SetParent(LocationContainer.transform);

            // The root prefab needs to be active, otherwise ZNetViews are not prepared correctly
            customLocation.Prefab.SetActive(true);

            Locations.Add(customLocation.Name, customLocation);
            return true;
        }

        /// <summary>
        ///     Get a custom location by name.
        /// </summary>
        /// <param name="name">Name of the location (normally the prefab name)</param>
        /// <returns>The <see cref="CustomLocation"/> object with the given name if found</returns>
        public CustomLocation GetCustomLocation(string name)
        {
            return Locations[name];
        }

        /// <summary>
        ///     Get a ZoneLocation by its name.<br /><br />
        ///     Search hierarchy:
        ///     <list type="number">
        ///         <item>Custom Location with the exact name</item>
        ///         <item>Vanilla Location with the exact name from <see cref="ZoneSystem"/></item>
        ///     </list>
        /// </summary>
        /// <param name="name">Name of the ZoneLocation to search for.</param>
        /// <returns>The existing ZoneLocation, or null if none exists with given name</returns>
        public ZoneLocation GetZoneLocation(string name)
        {
            if (Locations.TryGetValue(name, out CustomLocation customLocation))
            {
                return customLocation.ZoneLocation;
            }

            if (ZoneSystem.instance &&
                ZoneSystem.instance.m_locationsByHash.TryGetValue(name.GetStableHashCode(), out ZoneLocation location))
            {
                return location;
            }
            return null;
        }

        /// <summary>
        ///     Create a CustomLocation that is a deep copy of the original.<br />
        ///     Changes will not affect the original. The CustomLocation is already registered in the manager.
        /// </summary>
        /// <param name="name">name of the custom location</param>
        /// <param name="baseName">name of the existing location to copy</param>
        /// <returns>A CustomLocation object with the cloned location prefab</returns>
        public CustomLocation CreateClonedLocation(string name, string baseName)
        {
            var baseZoneLocation = GetZoneLocation(baseName);
            var copiedPrefab = Object.Instantiate(baseZoneLocation.m_prefab, Vector3.zero, Quaternion.identity, LocationContainer.transform);
            copiedPrefab.name = name;
            var clonedLocation = new CustomLocation(copiedPrefab, false, new LocationConfig(baseZoneLocation));
            AddCustomLocation(clonedLocation);
            return clonedLocation;
        }

        /// <summary>
        ///     Register a CustomVegetation to be added to the ZoneSystem
        /// </summary>
        /// <param name="customVegetation"></param>
        /// <returns></returns>
        public bool AddCustomVegetation(CustomVegetation customVegetation)
        {
            if (!PrefabManager.Instance.AddPrefab(customVegetation.Prefab, customVegetation.SourceMod))
            {
                return false;
            }
            Vegetations.Add(customVegetation.Name, customVegetation);
            return true;
        }

        /// <summary>
        ///     Get a ZoneVegetation by its name.<br /><br />
        ///     Search hierarchy:
        ///     <list type="number">
        ///         <item>Custom Vegetation with the exact name</item>
        ///         <item>Vanilla Vegetation with the exact name from <see cref="ZoneSystem"/></item>
        ///     </list>
        /// </summary>
        /// <param name="name">Name of the ZoneVegetation to search for.</param>
        /// <returns>The existing ZoneVegetation, or null if none exists with given name</returns>
        public ZoneSystem.ZoneVegetation GetZoneVegetation(string name)
        {
            if (Vegetations.TryGetValue(name, out CustomVegetation customVegetation))
            {
                return customVegetation.Vegetation;
            }
            return ZoneSystem.instance.m_vegetation
                .DefaultIfEmpty(null)
                .FirstOrDefault(zv => zv.m_prefab && zv.m_prefab.name == name);
        }

        private void ZoneSystem_SetupLocations(ZoneSystem self)
        {
            InvokeOnVanillaLocationsAvailable();

            // Prepare vanilla locations again as they may have been modified by a mod
            foreach (var location in ZoneSystem.instance.m_locations)
            {
                if (!location.m_enable || !location.m_prefab)
                {
                    continue;
                }

                ZoneSystem.PrepareNetViews(location.m_prefab, location.m_netViews);
                ZoneSystem.PrepareRandomSpawns(location.m_prefab, location.m_randomSpawns);
            }

            if (Locations.Count > 0)
            {
                List<string> toDelete = new List<string>();

                Logger.LogInfo($"Injecting {Locations.Count} custom locations");
                foreach (CustomLocation customLocation in Locations.Values)
                {
                    try
                    {
                        Logger.LogDebug(
                            $"Adding custom location {customLocation} in {string.Join(", ", GetMatchingBiomes(customLocation.ZoneLocation.m_biome))}");

                        // Fix references if needed
                        if (customLocation.FixReference)
                        {
                            customLocation.Prefab.FixReferences(true);
                            customLocation.FixReference = false;
                        }

                        var zoneLocation = customLocation.ZoneLocation;
                        self.m_locations.Add(zoneLocation);

                        ZoneSystem.PrepareNetViews(zoneLocation.m_prefab, zoneLocation.m_netViews);

                        foreach (var znet in zoneLocation.m_netViews)
                        {
                            if (!ZNetScene.instance.m_namedPrefabs.ContainsKey(znet.GetPrefabName().GetStableHashCode()))
                            {
                                var prefab = Object.Instantiate(znet.gameObject,
                                    PrefabManager.Instance.PrefabContainer.transform);
                                prefab.name = znet.GetPrefabName();
                                PrefabManager.Instance.AddPrefab(prefab);
                                PrefabManager.Instance.RegisterToZNetScene(prefab);
                            }
                        }

                        ZoneSystem.PrepareRandomSpawns(zoneLocation.m_prefab, zoneLocation.m_randomSpawns);

                        foreach (var znet in zoneLocation.m_randomSpawns.SelectMany(x => x.m_childNetViews))
                        {
                            if (!ZNetScene.instance.m_namedPrefabs.ContainsKey(znet.GetPrefabName().GetStableHashCode()))
                            {
                                var prefab = Object.Instantiate(znet.gameObject,
                                    PrefabManager.Instance.PrefabContainer.transform);
                                prefab.name = znet.GetPrefabName();
                                PrefabManager.Instance.AddPrefab(prefab);
                                PrefabManager.Instance.RegisterToZNetScene(prefab);
                            }
                        }

                        if (!self.m_locationsByHash.ContainsKey(zoneLocation.m_hash))
                        {
                            self.m_locationsByHash.Add(zoneLocation.m_hash, zoneLocation);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Exception caught while adding location: {ex}");
                        toDelete.Add(customLocation.Name);
                    }
                }

                foreach (var name in toDelete)
                {
                    Locations.Remove(name);
                }
            }

            if (Vegetations.Count > 0)
            {
                List<string> toDelete = new List<string>();

                Logger.LogInfo($"Injecting {Vegetations.Count} custom vegetation");
                foreach (CustomVegetation customVegetation in Vegetations.Values)
                {
                    try
                    {
                        Logger.LogDebug(
                            $"Adding custom vegetation {customVegetation} in {string.Join(", ", GetMatchingBiomes(customVegetation.Vegetation.m_biome))}");
                        
                        // Fix references if needed
                        if (customVegetation.FixReference)
                        {
                            customVegetation.Prefab.FixReferences(true);
                            customVegetation.FixReference = false;
                        }

                        self.m_vegetation.Add(customVegetation.Vegetation);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Exception caught while adding vegetation: {ex}");
                        toDelete.Add(customVegetation.Name);
                    }
                }

                foreach (var name in toDelete)
                {
                    Vegetations.Remove(name);
                }
            }
        }

        /// <summary>
        ///     Safely invoke OnVanillaLocationsAvailable
        /// </summary>
        private static void InvokeOnVanillaLocationsAvailable()
        {
            OnVanillaLocationsAvailable?.SafeInvoke();
        }
    }
}
