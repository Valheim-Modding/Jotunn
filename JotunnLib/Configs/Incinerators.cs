using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;

namespace Jotunn.Configs
{
    /// <summary>
    ///     Helper to get existing incinerator names
    /// </summary>
    public class Incinerators
    {
        /// <summary>
        ///     Incinerator
        /// </summary>
        public static string Incinerator => "incinerator";

        /// <summary>
        ///     Gets the human readable name to internal names map
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, string> GetNames()
        {
            return NamesMap;
        }

        /// <summary>
        ///     Get a <see cref="BepInEx.Configuration.AcceptableValueList{T}"/> of all incinerator names.
        ///     This can be used to create a <see cref="BepInEx.Configuration.ConfigEntry{T}"/> where only valid incinerator can be selected.<br/><br/>
        ///     Example:
        ///     <code>
        ///         var incineratorConfig = Config.Bind("Section", "Key", nameof(Incinerators.Incinerator), new ConfigDescription("Description", Incinerators.GetAcceptableValueList()));
        ///     </code>
        /// </summary>
        /// <returns></returns>
        public static AcceptableValueList<string> GetAcceptableValueList()
        {
            return AcceptableValues;
        }

        /// <summary>
        ///     Get the internal name for a incinerator from its human readable name.
        ///     If the given name is not a known incinerator, the value is returned unchanged.
        /// </summary>
        /// <param name="incinerator"></param>
        /// <returns></returns>
        public static string GetInternalName(string incinerator)
        {
            if (NamesMap.TryGetValue(incinerator, out string internalName))
            {
                return internalName;
            }

            return incinerator;
        }

        private static readonly Dictionary<string, string> NamesMap = new Dictionary<string, string>
        {
            { nameof(Incinerator), Incinerator },
        };

        private static readonly AcceptableValueList<string> AcceptableValues = new AcceptableValueList<string>(NamesMap.Values.ToArray());
    }
}
