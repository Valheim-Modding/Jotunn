using BepInEx;
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
        ///     ctor
        /// </summary>
        /// <param name="prefab">Prefab added</param>
        internal CustomPrefab(GameObject prefab)
        {
            Prefab = prefab;
        }

        /// <summary>
        ///     ctor
        /// </summary>
        /// <param name="prefab">Prefab added</param>
        /// <param name="sourceMod">Metadata of the mod adding this prefab</param>
        internal CustomPrefab(GameObject prefab, BepInPlugin sourceMod) : base(sourceMod)
        {
            Prefab = prefab;
        }
    }
}
