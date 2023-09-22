using System;
using System.Reflection;
using Jotunn.Configs;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace Jotunn.Entities
{
    /// <summary>
    ///     Main interface for adding custom vegetation to the game.<br />
    ///     All custom vegetation have to be wrapped inside this class to add it to Jötunns <see cref="ZoneManager"/>.
    /// </summary>
    public class CustomVegetation : CustomEntity
    {
        /// <summary>
        ///     The prefab for this custom vegetation.
        /// </summary>
        public GameObject Prefab { get; }

        /// <summary>
        ///     Associated <see cref="ZoneSystem.ZoneVegetation"/> component
        /// </summary>
        public ZoneSystem.ZoneVegetation Vegetation { get; }

        /// <summary>
        ///     Name of this custom vegetation
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        ///     Indicator if references from <see cref="Entities.Mock{T}"/>s will be replaced at runtime.
        /// </summary>
        public bool FixReference { get; set; }

        /// <summary>
        ///     Custom vegetation from a prefab.<br />
        /// </summary>
        /// <param name="prefab">The prefab for this custom vegetation.</param>
        /// <param name="config">The vegetation config for this custom vegation.</param> 
        [Obsolete("Use CustomVegetation(GameObject, bool, VegetationConfig) instead and define if references should be fixed")]
        public CustomVegetation(GameObject prefab, VegetationConfig config) : base(Assembly.GetCallingAssembly())
        {
            Prefab = prefab;
            Name = prefab.name;
            Vegetation = config.ToVegetation();
            Vegetation.m_prefab = prefab;
        }

        /// <summary>
        ///     Custom vegetation from a prefab.<br />
        ///     Can fix references for mocks.
        /// </summary>
        /// <param name="prefab">The prefab for this custom vegetation.</param>
        /// <param name="fixReference">If true references for <see cref="Entities.Mock{T}"/> objects get resolved at runtime by Jötunn.</param>
        /// <param name="config">The <see cref="VegetationConfig"/> for this custom vegation.</param>
        public CustomVegetation(GameObject prefab, bool fixReference, VegetationConfig config) : base(Assembly.GetCallingAssembly())
        {
            Prefab = prefab;
            Name = prefab.name;
            Vegetation = config.ToVegetation();
            Vegetation.m_prefab = prefab;
            FixReference = fixReference;
        }

        /// <summary>
        ///     Custom vegetation from a prefab loaded from an <see cref="AssetBundle"/>.<br />
        ///     Can fix references for mocks.
        /// </summary>
        /// <param name="assetBundle">A preloaded <see cref="AssetBundle"/></param>
        /// <param name="assetName">Name of the prefab in the bundle.</param>
        /// <param name="fixReference">If true references for <see cref="Entities.Mock{T}"/> objects get resolved at runtime by Jötunn.</param>
        /// <param name="config">The <see cref="VegetationConfig"/> for this custom vegation.</param>
        public CustomVegetation(AssetBundle assetBundle, string assetName, bool fixReference, VegetationConfig config) : base(Assembly.GetCallingAssembly())
        {
            Name = assetName;

            if (!AssetUtils.TryLoadPrefab(SourceMod, assetBundle, assetName, out GameObject prefab))
            {
                return;
            }

            Prefab = prefab;
            Vegetation = config.ToVegetation();
            Vegetation.m_prefab = Prefab;
            FixReference = fixReference;
        }

        /// <summary>
        ///     Helper method to determine if a prefab with a given name is a custom Vegetation created with Jötunn.
        /// </summary>
        /// <param name="prefabName">Name of the prefab to test.</param>
        /// <returns>true if the prefab is added as a custom vegetation to the <see cref="ZoneManager"/>.</returns>
        public static bool IsCustomVegetation(string prefabName)
        {
            return ZoneManager.Instance.Vegetations.ContainsKey(prefabName);
        }

        /// <summary>
        ///     Checks if a custom vegetation is valid (i.e. has a prefab, an <see cref="ItemDrop"/> and an icon, if it should be craftable).
        /// </summary>
        /// <returns>true if all criteria is met</returns>
        public bool IsValid()
        {
            bool valid = true;

            if (!Prefab)
            {
                Logger.LogError(SourceMod, $"CustomVegetation '{this}' has no prefab");
                valid = false;
            }

            return valid;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Name;
        }
    }
}
