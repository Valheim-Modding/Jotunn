using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;

namespace Jotunn.Configs
{
    /// <summary>
    ///     Helper to get existing crafting station names
    /// </summary>
    public static class CraftingStations
    {
        /// <summary>
        ///     No crafting station
        /// </summary>
        public static string None => string.Empty;

        /// <summary>
        ///    Workbench crafting station
        /// </summary>
        public static string Workbench => "piece_workbench";

        /// <summary>
        ///    Forge crafting station
        /// </summary>
        public static string Forge => "forge";

        /// <summary>
        ///     Stonecutter crafting station
        /// </summary>
        public static string Stonecutter => "piece_stonecutter";

        /// <summary>
        ///     Cauldron crafting station
        /// </summary>
        public static string Cauldron => "piece_cauldron";

        /// <summary>
        ///     Artisan table crafting station
        /// </summary>
        public static string ArtisanTable => "piece_artisanstation";

        /// <summary>
        ///     Black forge crafting station
        /// </summary>
        public static string BlackForge => "blackforge";

        /// <summary>
        ///     Galdr table crafting station
        /// </summary>
        public static string GaldrTable => "piece_magetable";

        /// <summary>
        ///     Gets the human readable name to internal names map
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, string> GetNames()
        {
            return NamesMap;
        }

        /// <summary>
        ///     Get a <see cref="BepInEx.Configuration.AcceptableValueList{T}"/> of all crafting station names.
        ///     This can be used to create a <see cref="BepInEx.Configuration.ConfigEntry{T}"/> where only valid crafting stations can be selected.
        ///     <example>
        ///         <code>
        ///             var stationConfig = Config.Bind("Section", "Key", nameof(CraftingStations.Workbench), new ConfigDescription("Description", CraftingStations.GetAcceptableValueList()));
        ///         </code>
        ///     </example>
        /// </summary>
        /// <returns></returns>
        public static AcceptableValueList<string> GetAcceptableValueList()
        {
            return AcceptableValues;
        }

        /// <summary>
        ///     Get the internal name for a crafting station from its human readable name.
        ///     If the given name is not a known crafting station, the value is returned unchanged.
        /// </summary>
        /// <param name="craftingStation"></param>
        /// <returns></returns>
        public static string GetInternalName(string craftingStation)
        {
            if (NamesMap.TryGetValue(craftingStation, out string internalName))
            {
                return internalName;
            }

            return craftingStation;
        }

        private static readonly Dictionary<string, string> NamesMap = new Dictionary<string, string>
        {
            { nameof(None), None },
            { nameof(Workbench), Workbench },
            { nameof(Forge), Forge },
            { nameof(Stonecutter), Stonecutter },
            { nameof(Cauldron), Cauldron },
            { nameof(ArtisanTable), ArtisanTable },
            { nameof(BlackForge), BlackForge },
            { nameof(GaldrTable), GaldrTable },
        };

        private static readonly AcceptableValueList<string> AcceptableValues = new AcceptableValueList<string>(NamesMap.Values.ToArray());
    }
}
