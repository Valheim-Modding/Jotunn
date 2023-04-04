using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;

namespace Jotunn.Configs
{
    /// <summary>
    ///     Helper to get existing smelter names
    /// </summary>
    public static class Smelters
    {
        /// <summary>
        ///     Smelter
        /// </summary>
        public static string Smelter => "smelter";

        /// <summary>
        ///     Blast furnace
        /// </summary>
        public static string BlastFurnace => "blastfurnace";

        /// <summary>
        ///     Charcoal kiln
        /// </summary>
        public static string CharcoalKiln => "charcoal_kiln";

        /// <summary>
        ///     Eitr refinery
        /// </summary>
        public static string EitrRefinery => "eitrrefinery";

        /// <summary>
        ///     Bathtub
        /// </summary>
        public static string Bathtub => "piece_bathtub";

        /// <summary>
        ///     Spinning wheel
        /// </summary>
        public static string SpinningWheel => "piece_spinningwheel";

        /// <summary>
        ///     Windmill
        /// </summary>
        public static string Windmill => "windmill";

        /// <summary>
        ///     Gets the human readable name to internal names map
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, string> GetNames()
        {
            return NamesMap;
        }

        /// <summary>
        ///     Get a <see cref="BepInEx.Configuration.AcceptableValueList{T}"/> of all smelter names.
        ///     This can be used to create a <see cref="BepInEx.Configuration.ConfigEntry{T}"/> where only valid smelter can be selected.
        ///     <example>
        ///         <code>
        ///             var smelterConfig = Config.Bind("Section", "Key", nameof(Smelters.Smelter), new ConfigDescription("Description", Smelters.GetAcceptableValueList()));
        ///         </code>
        ///     </example>
        /// </summary>
        /// <returns></returns>
        public static AcceptableValueList<string> GetAcceptableValueList()
        {
            return AcceptableValues;
        }

        /// <summary>
        ///     Get the internal name for a smelter from its human readable name.
        ///     If the given name is not a known smelter, the value is returned unchanged.
        /// </summary>
        /// <param name="smelter"></param>
        /// <returns></returns>
        public static string GetInternalName(string smelter)
        {
            if (NamesMap.TryGetValue(smelter, out string internalName))
            {
                return internalName;
            }

            return smelter;
        }

        private static readonly Dictionary<string, string> NamesMap = new Dictionary<string, string>
        {
            { nameof(Smelter), Smelter },
            { nameof(BlastFurnace), BlastFurnace },
            { nameof(CharcoalKiln), CharcoalKiln },
            { nameof(EitrRefinery), EitrRefinery },
            { nameof(Bathtub), Bathtub },
            { nameof(SpinningWheel), SpinningWheel },
            { nameof(Windmill), Windmill },
        };

        private static readonly AcceptableValueList<string> AcceptableValues = new AcceptableValueList<string>(NamesMap.Values.ToArray());
    }
}
