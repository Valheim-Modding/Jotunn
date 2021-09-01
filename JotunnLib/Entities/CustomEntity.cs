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
        public BepInPlugin SourceMod { get; } = BepInExUtils.GetSourceModMetadata();
    }
}
