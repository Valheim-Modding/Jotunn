using System.Reflection;
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
        ///     Reference to the <see cref="BepInPlugin"/> which added this entity.
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

        /// <summary>
        ///     ctor with passed calling assembly.
        ///     If the calling is no BepInEx plugin or Jotunn itself, the SourceMod will be searched via reflection
        /// </summary>
        /// <param name="callingAssembly"></param>
        internal CustomEntity(Assembly callingAssembly)
        {
            SourceMod = BepInExUtils.GetPluginInfoFromAssembly(callingAssembly)?.Metadata;

            if (SourceMod == null || SourceMod.GUID == Main.Instance.Info.Metadata.GUID)
            {
                SourceMod = BepInExUtils.GetSourceModMetadata();
            }
        }
    }
}
