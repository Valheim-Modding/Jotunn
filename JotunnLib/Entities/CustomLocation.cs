using System;
using System.Reflection;
using Jotunn.Configs;
using Jotunn.Managers;
using SoftReferenceableAssets;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Jotunn.Entities
{

    /// <summary>
    ///     Main interface for adding custom locations to the game.<br />
    ///     All custom locations have to be wrapped inside this class to add it to Jötunns <see cref="ZoneManager"/>.
    /// </summary>
    public class CustomLocation : CustomEntity
    {
        private readonly LocationConfig _locationConfig;

        /// <summary>
        ///     The exterior prefab for this custom location.
        /// </summary>
        public GameObject Prefab { get; }

        /// <summary>
        ///     Associated <see cref="ZoneSystem.ZoneLocation"/> component
        /// </summary>
        public ZoneSystem.ZoneLocation ZoneLocation { get; }

        /// <summary>
        ///     Associated <see cref="Location"/> component
        /// </summary>
        public Location Location { get; }

        /// <summary>
        ///     Name of this custom location
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Indicator if references from <see cref="Entities.Mock{T}"/>s will be replaced at runtime.
        /// </summary>
        public bool FixReference { get; set; }
        
        /// <summary>
        ///     Indicator if location is added from SoftReferenceableAssets.<br />
        ///     Used to delay mocking prefabs until ZoneSystem.SpawnLocation()
        /// </summary>
        public bool SoftReference { get; set; }

        /// <summary>
        ///     Custom location from a prefab with a <see cref="LocationConfig"/> attached.<br />
        ///     Does not fix references.
        /// </summary>
        /// <param name="exteriorPrefab">The exterior prefab for this custom location.</param>
        /// <param name="locationConfig">The <see cref="LocationConfig"/> for this custom location.</param>
        [Obsolete("Use CustomLocation(GameObject, bool, LocationConfig) instead and define if references should be fixed")]
        public CustomLocation(GameObject exteriorPrefab, LocationConfig locationConfig)
            : this(exteriorPrefab, null, false, locationConfig) { }

        /// <summary>
        ///     Custom location from a prefab with a <see cref="LocationConfig"/> attached.<br />
        ///     Does not fix references.
        /// </summary>
        /// <param name="exteriorPrefab">The exterior prefab for this custom location.</param>
        /// <param name="interiorPrefab">The interior prefab for this custom location.</param>
        /// <param name="locationConfig">The <see cref="LocationConfig"/> for this custom location.</param>
        [Obsolete("Use CustomLocation(GameObject, GameObject, bool, LocationConfig) instead and define if references should be fixed")]
        public CustomLocation(GameObject exteriorPrefab, GameObject interiorPrefab, LocationConfig locationConfig)
            : this(exteriorPrefab, interiorPrefab, false, locationConfig) { }

        /// <summary>
        ///     Custom location from a prefab with a <see cref="LocationConfig"/> attached.
        /// </summary>
        /// <param name="exteriorPrefab">The exterior prefab for this custom location.</param>
        /// <param name="fixReference">If true references for <see cref="Entities.Mock{T}"/> objects get resolved at runtime by Jötunn.</param>
        /// <param name="locationConfig">The <see cref="LocationConfig"/> for this custom location.</param>
        public CustomLocation(GameObject exteriorPrefab, bool fixReference, LocationConfig locationConfig)
            : this(exteriorPrefab, null, fixReference, locationConfig) { }

        /// <summary>
        ///     Custom location from a prefab with a <see cref="LocationConfig"/> attached.
        /// </summary>
        /// <param name="exteriorPrefab">The exterior prefab for this custom location.</param>
        /// <param name="interiorPrefab">The interior prefab for this custom location.</param>
        /// <param name="fixReference">If true references for <see cref="Entities.Mock{T}"/> objects get resolved at runtime by Jötunn.</param>
        /// <param name="locationConfig">The <see cref="LocationConfig"/> for this custom location.</param>
        public CustomLocation(GameObject exteriorPrefab, GameObject interiorPrefab, bool fixReference, LocationConfig locationConfig) : base(Assembly.GetCallingAssembly())
        {
            Prefab = exteriorPrefab;
            Name = exteriorPrefab.name;
            _locationConfig = locationConfig;

            if (exteriorPrefab.TryGetComponent<Location>(out var location))
            {
                Location = location;
            }
            else
            {
                Location = exteriorPrefab.AddComponent<Location>();
                Location.m_clearArea = locationConfig.ClearArea;
                Location.m_exteriorRadius = locationConfig.ExteriorRadius;
                Location.m_interiorPrefab = interiorPrefab;
                Location.m_hasInterior = locationConfig.HasInterior;
                Location.m_interiorRadius = locationConfig.InteriorRadius;
                Location.m_interiorEnvironment = locationConfig.InteriorEnvironment;
            }

            ZoneLocation = locationConfig.GetZoneLocation();
            ZoneLocation.m_prefab = new SoftReference<GameObject>(AssetManager.Instance.AddAsset(exteriorPrefab));
            ZoneLocation.m_prefabName = exteriorPrefab.name;

            SyncZoneLocationFromComponent(Location, locationConfig);

            FixReference = fixReference;
        }
        
        /// <summary>
        ///     Custom location from a prefab with a <see cref="LocationConfig"/> attached. Using SoftReference system.
        /// </summary>
        /// <param name="softReferencePrefab">The exterior prefab for this custom location.</param>
        /// <param name="fixReference">If true references for <see cref="Entities.Mock{T}"/> objects get resolved at runtime by Jötunn.</param>
        /// <param name="locationConfig">The <see cref="LocationConfig"/> for this custom location.</param>
        public CustomLocation(SoftReference<GameObject> softReferencePrefab, bool fixReference, LocationConfig locationConfig) : base(Assembly.GetCallingAssembly())
        {
            if (!softReferencePrefab.IsValid)
            {
                Logger.LogError($"SoftReference invalid for prefab: {softReferencePrefab.Name}");
                return;
            }

            _locationConfig = locationConfig;
            var parent = ZoneManager.Instance.LocationContainer.transform;
            AssetManager.Instance.ResolveMocksOnLoad(softReferencePrefab, parent, OnLocationResolve);
            Name = softReferencePrefab.Name;
            ZoneLocation = locationConfig.GetZoneLocation();
            ZoneLocation.m_prefab = softReferencePrefab;
            ZoneLocation.m_prefabName = softReferencePrefab.Name;
            FixReference = fixReference;
            SoftReference = true;
        }

        private void OnLocationResolve(GameObject gameObject)
        {
            if (gameObject.TryGetComponent<Location>(out var location))
            {
                SyncZoneLocationFromComponent(location, _locationConfig);
            }

            if (gameObject.TryGetComponent<ZoneSystem.ZoneLocation>(out var zoneLocation))
            {
                ZoneManager.Instance.PrepareLocation(zoneLocation, SourceMod);
            }
        }

        private void SyncZoneLocationFromComponent(Location location, LocationConfig locationConfig)
        {
            if (location == null || ZoneLocation == null)
            {
                return;
            }

            if (!locationConfig.HasExteriorRadius) ZoneLocation.m_exteriorRadius = location.m_exteriorRadius;
            if (!locationConfig.HasInteriorRadius) ZoneLocation.m_interiorRadius = location.m_interiorRadius;
            if (!locationConfig.HasClearArea) ZoneLocation.m_clearArea = location.m_clearArea;
        }

        /// <summary>
        ///     Helper method to determine if a location prefab with a given name is a custom location created with Jötunn.
        /// </summary>
        /// <param name="prefabName">Name of the prefab to test.</param>
        /// <returns>true if the prefab is added as a custom location to the <see cref="ZoneManager"/>.</returns>
        public static bool IsCustomLocation(string prefabName)
        {
            return ZoneManager.Instance.Locations.ContainsKey(prefabName);
        }
        
        /// <inheritdoc/>
        public override string ToString()
        {
            return Name;
        }
    }
}
