using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;

namespace Jotunn.Configs
{
    /// <summary>
    ///     Helper to get existing piece category names
    /// </summary>
    public static class PieceCategories
    {
        /// <summary>
        ///     Piece 'Misc' category
        /// </summary>
        public static string Misc => "Misc";

        /// <summary>
        ///     Piece 'Crafting' category
        /// </summary>
        public static string Crafting => "Crafting";

        /// <summary>
        ///     Piece 'Building' category
        /// </summary>
        public static string Building => "BuildingWorkbench";

        /// <summary>
        ///     Piece 'Furniture' category
        /// </summary>
        public static string Furniture => "Furniture";

        /// <summary>
        ///     All piece categories
        /// </summary>
        public static string All => "All";

        /// <summary>
        ///     Gets the human readable name to internal names map
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, string> GetNames()
        {
            return NamesMap;
        }

        /// <summary>
        ///     Get a <see cref="BepInEx.Configuration.AcceptableValueList{T}"/> of all piece category names.
        ///     This can be used to create a <see cref="BepInEx.Configuration.ConfigEntry{T}"/> where only valid piece category can be selected.<br/><br/>
        ///     Example:
        ///     <code>
        ///         var pieceCategoryConfig = Config.Bind("Section", "Key", nameof(PieceCategories.Building), new ConfigDescription("Description", PieceCategories.GetAcceptableValueList()));
        ///     </code>
        /// </summary>
        /// <returns></returns>
        public static AcceptableValueList<string> GetAcceptableValueList()
        {
            return AcceptableValues;
        }

        /// <summary>
        ///     Get the internal name for a piece category from its human readable name.
        /// </summary>
        /// <param name="pieceCategory"></param>
        /// <returns>
        ///     The matched internal name.
        ///     If the pieceCategory parameter is null or empty, an empty string is returned.
        ///     Otherwise the unchanged pieceCategory parameter is returned.
        /// </returns>
        public static string GetInternalName(string pieceCategory)
        {
            if (string.IsNullOrEmpty(pieceCategory))
            {
                return string.Empty;
            }

            if (NamesMap.TryGetValue(pieceCategory, out string internalName))
            {
                return internalName;
            }

            return pieceCategory;
        }

        private static readonly Dictionary<string, string> NamesMap = new Dictionary<string, string>
        {
            { nameof(Misc), Misc },
            { nameof(Crafting), Crafting },
            { nameof(Building), Building },
            { nameof(Furniture), Furniture },
            { nameof(All), All },
        };

        private static readonly AcceptableValueList<string> AcceptableValues = new AcceptableValueList<string>(NamesMap.Keys.ToArray());
    }
}
