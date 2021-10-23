using System;
using System.Collections.Generic;
using Jotunn.Entities;
using UnityEngine;
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
            OnVanillaLocationsAvailable.SafeInvoke();

            Logger.LogInfo("Injecting custom locations");
            foreach (CustomLocation customLocation in customLocations)
            {
                Logger.LogInfo($"Adding custom location {customLocation.Prefab.name} in {customLocation.Biome}");

                var zoneLocation = customLocation.ToZoneLocation();
                self.m_locations.Add(zoneLocation);

                zoneLocation.m_prefab = customLocation.Prefab;
                zoneLocation.m_hash = zoneLocation.m_prefab.name.GetStableHashCode();
                Location location = customLocation.Location;
                zoneLocation.m_location = location;
                zoneLocation.m_interiorRadius = (location.m_hasInterior ? location.m_interiorRadius : 0f);
                zoneLocation.m_exteriorRadius = location.m_exteriorRadius;
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
                Logger.LogInfo($"Adding custom location {customVegetation.Prefab.name} in {customVegetation.Biome}"); 
                self.m_vegetation.Add(customVegetation.ToVegetation());
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

        public CustomLocation CreateLocationContainer(string name)
        {
            GameObject container = new GameObject()
            {
                name = name
            };
            container.transform.SetParent(LocationContainer.transform);
            return new CustomLocation
            {
                Prefab = container,
                Location = container.AddComponent<Location>()
            };
        }

        public bool AddCustomLocation(CustomLocation customLocation)
        {
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
