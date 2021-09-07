using BepInEx;
using Jotunn.Utils;

namespace Jotunn.Entities
{
    /// <summary>
    ///     Base class for all custom entities
    /// </summary>
    public abstract class CustomEntity
    {
        /// <summary>
        ///     Reference to the <see cref="BepInPlugin"/> which added this prefab.
        /// </summary>
        public BepInPlugin SourceMod { get; }

        /// <summary>
        ///     ctor automatically getting the SourceMod
        /// </summary>
        internal CustomEntity()
        {
            SourceMod = BepInExUtils.GetSourceModMetadata();
        }

        /// <summary>
        ///     ctor with manual assigned SourceMod
        /// </summary>
        /// <param name="sourceMod">Metadata of the mod adding this entity</param>
        internal CustomEntity(BepInPlugin sourceMod)
        {
            SourceMod = sourceMod;
        }
    }
}
