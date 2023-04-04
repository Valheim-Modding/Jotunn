using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;

namespace Jotunn.Configs
{
    /// <summary>
    ///    Helper to get existing piece table names
    /// </summary>
    public static class PieceTables
    {
        /// <summary>
        ///     Hammer piece table
        /// </summary>
        public static string Hammer => "_HammerPieceTable";

        /// <summary>
        ///     Cultivator piece table
        /// </summary>
        public static string Cultivator => "_CultivatorPieceTable";

        /// <summary>
        ///     Hoe piece table
        /// </summary>
        public static string Hoe => "_HoePieceTable";

        /// <summary>
        ///     Gets the human readable name to internal names map
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, string> GetNames()
        {
            return NamesMap;
        }

        /// <summary>
        ///     Get a <see cref="BepInEx.Configuration.AcceptableValueList{T}"/> of all piece table names.
        ///     This can be used to create a <see cref="BepInEx.Configuration.ConfigEntry{T}"/> where only valid piece table can be selected.
        ///     <example>
        ///         <code>
        ///             var pieceTableConfig = Config.Bind("Section", "Key", nameof(PieceTables.Hammer), new ConfigDescription("Description", PieceTables.GetAcceptableValueList()));
        ///         </code>
        ///     </example>
        /// </summary>
        /// <returns></returns>
        public static AcceptableValueList<string> GetAcceptableValueList()
        {
            return AcceptableValues;
        }

        /// <summary>
        ///     Get the internal name for a piece table from its human readable name.
        ///     If the given name is not a known piece table, the value is returned unchanged.
        /// </summary>
        /// <param name="pieceTable"></param>
        /// <returns></returns>
        public static string GetInternalName(string pieceTable)
        {
            if (NamesMap.TryGetValue(pieceTable, out string internalName))
            {
                return internalName;
            }

            return pieceTable;
        }

        private static readonly Dictionary<string, string> NamesMap = new Dictionary<string, string>
        {
            { nameof(Hammer), Hammer },
            { nameof(Cultivator), Cultivator },
            { nameof(Hoe), Hoe },
        };

        private static readonly AcceptableValueList<string> AcceptableValues = new AcceptableValueList<string>(NamesMap.Values.ToArray());
    }
}
