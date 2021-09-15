using BepInEx;
using Jotunn.Managers;
using UnityEngine;

namespace Jotunn.Entities
{
    /// <summary>
    ///     Wrapper for custom added GameObjects holding the mod reference.
    /// </summary>
    public class CustomPrefab : CustomEntity
    {
        /// <summary>
        ///     Original prefab
        /// </summary>
        public GameObject Prefab { get; }
        
        /// <summary>
        ///     Indicator if references from <see cref="Entities.Mock{T}"/>s will be replaced at runtime.
        /// </summary>
        public bool FixReference { get; set; }
        
        /// <summary>
        ///     Internal ctor with provided <see cref="BepInPlugin"/> metadata.<br />
        ///     Does not fix references.
        /// </summary>
        /// <param name="prefab">Prefab added</param>
        /// <param name="sourceMod">Metadata of the mod adding this prefab</param>
        internal CustomPrefab(GameObject prefab, BepInPlugin sourceMod) : base(sourceMod)
        {
            Prefab = prefab;
        }
        
        /// <summary>
        ///     Custom prefab.<br />
        ///     Can fix references for <see cref="Entities.Mock{T}"/>s.
        /// </summary>
        /// <param name="prefab">The prefab for this custom item.</param>
        /// <param name="fixReference">If true references for <see cref="Entities.Mock{T}"/> objects get resolved at runtime by Jötunn.</param>
        public CustomPrefab(GameObject prefab, bool fixReference)
        {
            Prefab = prefab;
            FixReference = fixReference;
        }

        /// <summary>
        ///     Checks if a custom item is valid (i.e. has a prefab, an <see cref="ItemDrop"/> and an icon, if it should be craftable).
        /// </summary>
        /// <returns>true if all criteria is met</returns>
        public bool IsValid()
        {
            bool valid = true;

            if (!Prefab)
            {
                Logger.LogError($"CustomPrefab {this} has no prefab");
                valid = false;
            }
            return valid;
        }
        
        /// <summary>
        ///     Helper method to determine if a prefab with a given name is a custom prefab created with Jötunn.
        /// </summary>
        /// <param name="prefabName">Name of the prefab to test.</param>
        /// <returns>true if the prefab is added as a custom prefab to the <see cref="PrefabManager"/>.</returns>
        public static bool IsCustomPrefab(string prefabName)
        {
            return PrefabManager.Instance.Prefabs.ContainsKey(prefabName.GetStableHashCode());
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Prefab.name;
        }
    }
}
