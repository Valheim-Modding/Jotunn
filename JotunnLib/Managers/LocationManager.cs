using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Jotunn.Entities;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine;
using static ZoneSystem;

namespace Jotunn.Managers
{
    public class LocationManager: IManager
    {
        private static LocationManager _instance;
        /// <summary>
        ///     Global singleton instance of the manager.
        /// </summary>
        public static LocationManager Instance
        {
            get
            {
                if (_instance == null) _instance = new LocationManager();
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
                Logger.LogInfo($"Adding custom location {customLocation.Prefab.name}");
                 
                var zoneLocation = customLocation.ToZoneLocation();
                self.m_locations.Add(zoneLocation);

                zoneLocation.m_prefab = customLocation.Prefab;
                zoneLocation.m_hash = zoneLocation.m_prefab.name.GetStableHashCode();
                Location location = customLocation.Location;
                zoneLocation.m_interiorRadius = (location.m_hasInterior ? location.m_interiorRadius : 0f);
                zoneLocation.m_exteriorRadius = location.m_exteriorRadius;
                if (Application.isPlaying)
                {
                    PrepareNetViews(zoneLocation.m_prefab, zoneLocation.m_netViews);
                    PrepareRandomSpawns(zoneLocation.m_prefab, zoneLocation.m_randomSpawns);
                    if (!self.m_locationsByHash.ContainsKey(zoneLocation.m_hash))
                    {
                        self.m_locationsByHash.Add(zoneLocation.m_hash, zoneLocation);
                    }
                }
            } 
             
        }

        private void ZNetView_Awake(On.ZNetView.orig_Awake orig, ZNetView self)
        {
#if DEBUG
            if(ZNetView.m_forceDisableInit || ZDOMan.instance == null)
            {
                Jotunn.Logger.LogWarning($"ZNetView of {self.name} will self-destruct");
            }
#endif
            orig(self);
        }
        
        public ZoneLocation GetZoneLocation(string name)
        {
            if(ZoneSystem.instance.m_locationsByHash.TryGetValue(name.GetStableHashCode(), out ZoneLocation location))
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
    }
}
