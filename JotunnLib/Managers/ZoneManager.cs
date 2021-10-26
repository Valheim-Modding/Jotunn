using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Jotunn.Entities;
using UnityEngine;
using Object = UnityEngine.Object;
using ZoneLocation = ZoneSystem.ZoneLocation;

namespace Jotunn.Managers
{
    public class ZoneManager : IManager
    {
        private static ZoneManager _instance;
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

        private readonly List<CustomLocation> customLocations = new List<CustomLocation>();
        private readonly List<CustomVegetation> customVegetations = new List<CustomVegetation>();

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

            DebugVanillaLocations(self);


            OnVanillaLocationsAvailable.SafeInvoke();


            Logger.LogInfo("Injecting custom locations");
            foreach (CustomLocation customLocation in customLocations)
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
            foreach(CustomVegetation customVegetation in customVegetations)
            {
                Logger.LogInfo($"Adding custom location {customVegetation.Prefab.name} in {customVegetation.Vegetation.m_biome}"); 
                self.m_vegetation.Add(customVegetation.Vegetation);
            }

        }

        private void DebugVanillaLocations(ZoneSystem self)
        {
            HashSet<string> groups = new HashSet<string>();
            foreach (ZoneLocation zoneLocation in self.m_locations)
            {
                if(zoneLocation.m_group != null && zoneLocation.m_group != "")
                {
                    groups.Add(zoneLocation.m_group);
                }
            }
            foreach(string group in groups)
            {
                Logger.LogInfo($"Available group {group}");
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

        public ZoneLocation GetZoneLocation(string name)
        {
            if (ZoneSystem.instance.m_locationsByHash.TryGetValue(name.GetStableHashCode(), out ZoneLocation location))
            {
                return location;
            }
            return null;
        }

        public GameObject CreateLocationContainer(string name)
        {
            GameObject container = new GameObject()
            {
                name = name
            };
            container.transform.SetParent(LocationContainer.transform);
            return container;
        }

        private Regex copyRegex = new Regex(@" \([0-9]+\)");

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
                        continue;
                    }
                    string prefabName = child.name.Substring("JVLmock_".Length);
                     
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

        public CustomLocation CreateClonedLocation(string name, string baseName)
        {
            var baseZoneLocation = GetZoneLocation(baseName);
            var copiedPrefab = Object.Instantiate(baseZoneLocation.m_prefab, LocationContainer.transform);
            copiedPrefab.name = name;
            var clonedLocation = new CustomLocation(copiedPrefab, new Configs.LocationConfig(baseZoneLocation));
            AddCustomLocation(clonedLocation);
            return clonedLocation;
        }

        public bool AddCustomLocation(CustomLocation customLocation)
        {
            customLocation.Prefab.transform.SetParent(LocationContainer.transform);

            customLocations.Add(customLocation);
            return true;
        }
          

        public bool AddCustomVegetation(CustomVegetation customVegetation)
        {
            customVegetations.Add(customVegetation);
            return true;
        }

    }
}
