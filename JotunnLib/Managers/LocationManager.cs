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

        private readonly Dictionary<string, Location> locationsDict = new Dictionary<string, Location>();
        private readonly List<ZoneLocation> customZoneLocations = new List<ZoneLocation>();
        private readonly List<Location> customLocations = new List<Location>();

        public void Init()
        {

            LocationContainer = new GameObject("Locations");
            LocationContainer.transform.parent = Main.RootObject.transform;
            LocationContainer.SetActive(false);
 
            IL.ZoneSystem.SetupLocations += ZoneSystem_SetupLocations_IL;
            On.ZNetView.Awake += ZNetView_Awake;
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

        private void ZoneSystem_SetupLocations_IL(ILContext il)
        { 
            ILCursor c = new ILCursor(il);
            int locationsLoc = 0;
            c.GotoNext(MoveType.Before, 
                zz => zz.MatchNewobj<List<Location>>(),
                zz => zz.MatchStloc(out locationsLoc)
            );
            //Restart cursor in case order changes in updates
            c = new ILCursor(il); 
            int locationListsLoc = 0;
            c.GotoNext(MoveType.Before,
                zz => zz.MatchNewobj<List<LocationList>>(),
                zz => zz.MatchStloc(out locationListsLoc)
            );
            c = new ILCursor(il);
            c.GotoNext(MoveType.Before,
                zz => zz.MatchCallOrCallvirt(typeof(List<LocationList>).GetMethod(nameof(List<LocationList>.Sort), new Type[] { typeof(Comparison<LocationList>) }))
            );
            c.Emit(OpCodes.Ldloc, locationsLoc);
            c.Emit(OpCodes.Ldloc, locationListsLoc);
            var method = typeof(LocationManager).GetMethod(nameof(VanillaLocationsCallback), BindingFlags.Static | BindingFlags.NonPublic);
            c.Emit(OpCodes.Call, method);  
        }

        private static void VanillaLocationsCallback(List<Location> locations, List<LocationList> locationLists)
        {
            LocationManager.Instance.RegisterVanillaLocation(locations, locationLists); 
        }

        private void RegisterVanillaLocation(List<Location> locations, List<LocationList> locationLists)
        {
#if DEBUG
            Jotunn.Logger.LogInfo($"Vanilla locations: {locations.Count}, locationLists: {locationLists.Count}");
#endif
            foreach (Location location in locations)
            {
                locationsDict.Add(location.name, location);
            }
            OnVanillaLocationsAvailable.SafeInvoke();

            Jotunn.Logger.LogInfo($"Adding {customLocations.Count} custom locations");
            locations.AddRange(customLocations);
            ZoneSystem.instance.m_locations.AddRange(customZoneLocations);
        }

        public Location GetLocation(string name)
        {
            if(locationsDict.TryGetValue(name, out Location location))
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
            customLocation.Prefab.transform.SetParent(LocationContainer.transform);
            customZoneLocations.Add(customLocation.ToZoneLocation());
            customLocations.Add(customLocation.Location);
            return true;
        }
    }
}
