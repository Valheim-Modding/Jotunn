using System;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace Jotunn.Entities
{

    /// <summary>
    ///     Main interface for adding custom locations to the game.<br />
    ///     All custom locations have to be wrapped inside this class to add it to Jötunns <see cref="ZoneManager"/>.
    /// </summary>
    public class CustomLocation : CustomEntity
    {
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
        public CustomLocation(GameObject exteriorPrefab, GameObject interiorPrefab, bool fixReference, LocationConfig locationConfig)
        {
            Prefab = exteriorPrefab;
            Name = exteriorPrefab.name;

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
            ZoneLocation.m_prefab = exteriorPrefab;
            ZoneLocation.m_prefabName = exteriorPrefab.name;
            ZoneLocation.m_hash = exteriorPrefab.name.GetStableHashCode();
            ZoneLocation.m_location = Location;

            FixReference = fixReference;
        }
        
        /// <inheritdoc/>
        public override string ToString()
        {
            return Name;
        }
    }
}
