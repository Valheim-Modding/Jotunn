using System.Collections.Generic;
using System.Linq;
using Jotunn.Entities;
using Jotunn.Managers;

namespace Jotunn.Utils
{
    /// <summary>
    ///     Utility class to query metadata about loaded mods and their added content
    /// </summary>
    public static class ModRegistry
    {
        /// <summary>
        ///     Get all loaded mod's metadata
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<ModInfo> GetMods()
        {
            foreach (var mod in BepInExUtils.GetDependentPlugins().Values.Select(mod => mod.Info.Metadata))
            {
                yield return new ModInfo()
                {
                    GUID = mod.GUID,
                    Name = mod.Name,
                    Version = mod.Version,
                    Items = GetItems(mod.GUID),
                    Pieces = GetPieces(mod.GUID)
                };
            }
        }

        /// <summary>
        ///     Get all added <see cref="CustomPiece"/> instances
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<CustomPiece> GetPieces()
        {
            return PieceManager.Instance.Pieces.ToList();
        }

        /// <summary>
        ///     Get all added <see cref="CustomPiece"/> instances of a mod by GUID
        /// </summary>
        /// <param name="modGuid">GUID of the mod</param>
        /// <returns></returns>
        public static IEnumerable<CustomPiece> GetPieces(string modGuid)
        {
            return PieceManager.Instance.Pieces.Where(mod => mod.SourceMod.GUID.Equals(modGuid));
        }

        /// <summary>
        ///     Get all added <see cref="CustomItem"/> instances
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<CustomItem> GetItems()
        {
            return ItemManager.Instance.Items.ToList();
        }

        /// <summary>
        ///     Get all added <see cref="CustomItem"/> instances of a mod by GUID
        /// </summary>
        /// <param name="modGuid">GUID of the mod</param>
        /// <returns></returns>
        public static IEnumerable<CustomItem> GetItems(string modGuid)
        {
            return ItemManager.Instance.Items.Where(mod => mod.SourceMod.GUID.Equals(modGuid));
        }

        /// <summary>
        ///     Model class holding metadata of Jötunn mods.
        /// </summary>
        public class ModInfo
        {
            /// <summary>
            ///     The mod GUID
            /// </summary>
            public string GUID { get; internal set; }

            /// <summary>
            ///     Human readable name
            /// </summary>
            public string Name { get; internal set; }

            /// <summary>
            ///     Current version
            /// </summary>
            public System.Version Version { get; internal set; }

            /// <summary>
            ///     Custom items added by that mod
            /// </summary>
            public IEnumerable<CustomItem> Items { get; internal set; }

            /// <summary>
            ///     Custom pieces added by that mod
            /// </summary>
            public IEnumerable<CustomPiece> Pieces { get; internal set; }
        }
    }
}
