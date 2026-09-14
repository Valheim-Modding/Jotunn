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
        ///     Serving Tray piece table
        /// </summary>
        public static string ServingTray => "_FeasterPieceTable";

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
        ///     This can be used to create a <see cref="BepInEx.Configuration.ConfigEntry{T}"/> where only valid piece table can be selected.<br/><br/>
        ///     Example:
        ///     <code>
        ///          var pieceTableConfig = Config.Bind("Section", "Key", nameof(PieceTables.Hammer), new ConfigDescription("Description", PieceTables.GetAcceptableValueList()));
        ///     </code>
        /// </summary>
        /// <returns></returns>
        public static AcceptableValueList<string> GetAcceptableValueList()
        {
            return AcceptableValues;
        }

        /// <summary>
        ///     Get the internal name for a piece table from its human readable name.
        /// </summary>
        /// <param name="pieceTable"></param>
        /// <returns>
        ///     The matched internal name.
        ///     If the pieceTable parameter is null or empty, an empty string is returned.
        ///     Otherwise the unchanged pieceTable parameter is returned.
        /// </returns>
        public static string GetInternalName(string pieceTable)
        {
            if (string.IsNullOrEmpty(pieceTable))
            {
                return string.Empty;
            }

            if (NamesMap.TryGetValue(pieceTable, out string internalName))
            {
                return internalName;
            }

            return pieceTable;
        }

        /// <summary>
        ///     Get the human readable name for a piece table from its internal name.
        /// </summary>
        /// <param name="internalName"></param>
        /// <returns>
        ///     The matched human readable name or the unchanged internalName if no match was found.
        /// </returns>
        public static string GetDisplayName(string internalName)
        {
            return NamesMap.FirstOrDefault(x => x.Value == internalName).Key ?? internalName;
        }

        private static readonly Dictionary<string, string> NamesMap = new Dictionary<string, string>
        {
            { nameof(Hammer), Hammer },
            { nameof(Cultivator), Cultivator },
            { nameof(Hoe), Hoe },
            { nameof(ServingTray), ServingTray },
        };

        private static readonly AcceptableValueList<string> AcceptableValues = new AcceptableValueList<string>(NamesMap.Keys.ToArray());
    }
}
