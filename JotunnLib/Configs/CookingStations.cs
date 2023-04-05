using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;

namespace Jotunn.Configs
{
    /// <summary>
    ///     Helper to get existing cooking station names
    /// </summary>
    public static class CookingStations
    {
        /// <summary>
        ///     Cooking station
        /// </summary>
        public static string CookingStation => "piece_cookingstation";

        /// <summary>
        ///     Iron cooking station
        /// </summary>
        public static string IronCookingStation => "piece_cookingstation_iron";

        /// <summary>
        ///     Stone oven cooking station
        /// </summary>
        public static string StoneOven => "piece_oven";

        /// <summary>
        ///     Gets the human readable name to internal names map
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, string> GetNames()
        {
            return NamesMap;
        }

        /// <summary>
        ///     Get a <see cref="BepInEx.Configuration.AcceptableValueList{T}"/> of all cooking station names.
        ///     This can be used to create a <see cref="BepInEx.Configuration.ConfigEntry{T}"/> where only valid cooking stations can be selected.<br/><br/>
        ///     Example:
        ///     <code>
        ///         var stationConfig = Config.Bind("Section", "Key", nameof(CookingStations.CookingStation), new ConfigDescription("Description", CookingStations.GetAcceptableValueList()));
        ///     </code>
        /// </summary>
        /// <returns></returns>
        public static AcceptableValueList<string> GetAcceptableValueList()
        {
            return AcceptableValues;
        }

        /// <summary>
        ///     Get the internal name for a cooking station from its human readable name.
        ///     If the given name is not a known cooking station, the value is returned unchanged.
        /// </summary>
        /// <param name="cookingStation"></param>
        /// <returns></returns>
        public static string GetInternalName(string cookingStation)
        {
            if (NamesMap.TryGetValue(cookingStation, out string internalName))
            {
                return internalName;
            }

            return cookingStation;
        }

        private static readonly Dictionary<string, string> NamesMap = new Dictionary<string, string>
        {
            { nameof(CookingStation), CookingStation },
            { nameof(IronCookingStation), IronCookingStation },
            { nameof(StoneOven), StoneOven },
        };

        private static readonly AcceptableValueList<string> AcceptableValues = new AcceptableValueList<string>(NamesMap.Values.ToArray());
    }
}
