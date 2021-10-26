using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
        public static ZoneManager Instance
        {
            get
            {
                if (_instance == null) _instance = new ZoneManager();
                return _instance;
            }
        }
          

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

        private readonly Dictionary<string, CustomLocation> Locations = new Dictionary<string, CustomLocation>();
        private readonly Dictionary<string, CustomVegetation> Vegetations = new Dictionary<string, CustomVegetation>();

        /// <summary>
        ///     Initialize the manager
        /// </summary>
        public void Init()
        {

            LocationContainer = new GameObject("Locations");
            LocationContainer.transform.parent = Main.RootObject.transform;
            LocationContainer.SetActive(false);

            On.ZoneSystem.SetupLocations += ZoneSystem_SetupLocations;
            On.ZNetView.Awake += ZNetView_Awake;

        }

        private void ZoneSystem_SetupLocations(On.ZoneSystem.orig_SetupLocations orig, ZoneSystem self)
        {
            orig(self);
             
            OnVanillaLocationsAvailable.SafeInvoke();


            Logger.LogInfo("Injecting custom locations");
            foreach (CustomLocation customLocation in Locations.Values)
            {
                Logger.LogInfo($"Adding custom location {customLocation.Prefab.name} in {customLocation.ZoneLocation.m_biome}");

                var zoneLocation = customLocation.ZoneLocation;
                self.m_locations.Add(zoneLocation);

                zoneLocation.m_prefab = customLocation.Prefab;
                zoneLocation.m_hash = zoneLocation.m_prefab.name.GetStableHashCode();
                Location location = customLocation.Location;
                zoneLocation.m_location = location; 
                if (Application.isPlaying)
                {
                    ZoneSystem.PrepareNetViews(zoneLocation.m_prefab, zoneLocation.m_netViews);
                    ZoneSystem.PrepareRandomSpawns(zoneLocation.m_prefab, zoneLocation.m_randomSpawns);
                    if (!self.m_locationsByHash.ContainsKey(zoneLocation.m_hash))
                    {
                        self.m_locationsByHash.Add(zoneLocation.m_hash, zoneLocation);
                    }
                }
            }
            Logger.LogInfo("Injecting custom vegetation");
            foreach(CustomVegetation customVegetation in Vegetations.Values)
            {
                Logger.LogInfo($"Adding custom location {customVegetation.Prefab.name} in {customVegetation.Vegetation.m_biome}"); 
                self.m_vegetation.Add(customVegetation.Vegetation);
            }

        }
         
        private void ZNetView_Awake(On.ZNetView.orig_Awake orig, ZNetView self)
        {
#if DEBUG
            if (ZNetView.m_forceDisableInit || ZDOMan.instance == null)
            {
                Jotunn.Logger.LogWarning($"ZNetView of {self.name} will self-destruct");
            }
#endif
            orig(self);
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
            if(Locations.TryGetValue(name, out CustomLocation customLocation))
            {
                return customLocation.ZoneLocation;
            }

            if (ZoneSystem.instance.m_locationsByHash.TryGetValue(name.GetStableHashCode(), out ZoneLocation location))
            {
                return location;
            }
            return null;
        }

        /// <summary>
        ///     Create an empty GameObject that is disabled, so any Components in instantiated GameObjects will not start their lifecycle.
        /// </summary>
        /// <param name="name">Name of the location</param>
        /// <returns></returns>
        public GameObject CreateLocationContainer(string name)
        {
            GameObject container = new GameObject()
            {
                name = name
            };
            container.transform.SetParent(LocationContainer.transform);
            return container;
        }

        private readonly Regex copyRegex = new Regex(@" \([0-9]+\)");

        /// <summary>
        ///     Create a copy that is disabled, so any Components in instantiated GameObjects will not start their lifecycle     
        /// </summary>
        /// <param name="gameObject">Prefab to copy</param>
        /// <param name="fixLocationReferences">Replace JVLmock GameObjects with a copy of their real prefab</param>
        /// <returns></returns>
        public GameObject CreateLocationContainer(GameObject gameObject, bool fixLocationReferences = false)
        {
            var locationContainer = Object.Instantiate(gameObject, LocationContainer.transform);
            if(fixLocationReferences)
            {
                var transform = locationContainer.transform;
                for (int i = 0; i < transform.childCount; i++)
                {
                    var child = transform.GetChild(i);
                    if(!child.name.StartsWith("JVLmock_"))
                    {
                        //Allow nested component references to JVLmock
                        child.gameObject.FixReferences();
                        continue;
                    }
                    string prefabName = child.name.Substring("JVLmock_".Length);
                     
                    //Allow duplicated JVLmocks (child names must be unique)
                    Match match = copyRegex.Match(prefabName);
                    if (match.Success)
                    {
                        prefabName = prefabName.Substring(0, match.Index);
                    }
                    
                    var replacementPrefab = PrefabManager.Instance.GetPrefab(prefabName);
                    if(!replacementPrefab)
                    {
                        Logger.LogWarning($"No replacement prefab found for {prefabName}");
                    } else
                    {
                        var replacement = Object.Instantiate(replacementPrefab, child.localPosition, child.localRotation, transform);
                        replacement.transform.localScale = child.localScale;
                        Object.Destroy(child.gameObject);
                    } 
                }
            }


            return locationContainer;
        }

        /// <summary>
        ///     Create a CustomLocation that is a deep copy of the original.
        ///     Changes will not affect the original. The CustomLocation is already registered to be added.
        /// </summary>
        /// <param name="name">name of the custom location</param>
        /// <param name="baseName">name of the existing location to copy</param>
        /// <returns></returns>
        public CustomLocation CreateClonedLocation(string name, string baseName)
        {
            var baseZoneLocation = GetZoneLocation(baseName);
            var copiedPrefab = Object.Instantiate(baseZoneLocation.m_prefab, LocationContainer.transform);
            copiedPrefab.name = name;
            var clonedLocation = new CustomLocation(copiedPrefab, new Configs.LocationConfig(baseZoneLocation));
            AddCustomLocation(clonedLocation);
            return clonedLocation;
        }

        /// <summary>
        ///     Register a CustomLocation to be added to the ZoneSystem
        /// </summary>
        /// <param name="customLocation"></param>
        /// <returns></returns>
        public bool AddCustomLocation(CustomLocation customLocation)
        {
            if(Locations.TryGetValue(customLocation.Name, out CustomLocation existingLocation))
            {
                Logger.LogWarning($"Location {customLocation.Name} already exists");
                return false;
            }

            customLocation.Prefab.transform.SetParent(LocationContainer.transform);

            Locations.Add(customLocation.Name, customLocation);
            return true;
        }
          
        /// <summary>
        ///     Register a CustomVegetation to be added to the ZoneSystem
        /// </summary>
        /// <param name="customVegetation"></param>
        /// <returns></returns>
        public bool AddCustomVegetation(CustomVegetation customVegetation)
        {
            Vegetations.Add(customVegetation.Name, customVegetation);
            return true;
        }

    }
}
