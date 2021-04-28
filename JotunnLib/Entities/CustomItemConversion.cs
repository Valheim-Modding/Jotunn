using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace Jotunn.Entities
{
    /// <summary>
    ///     Main interface for adding custom item conversions to the game.<br />
    ///     Supports and combines conversions for the cooking station, fermenter and smelter.<br />
    ///     All custom item conversions have to be wrapped inside this class to add it to Jötunns <see cref="ItemManager"/>.
    /// </summary>
    public class CustomItemConversion
    {
        public enum ConversionType
        {
            CookingStation,
            Fermenter,
            Smelter
        };

        /// <summary>
        ///     Type of the item conversion. Defines to which station the conversion is added.
        /// </summary>
        public ConversionType Type { get; set; }

        /// <summary>
        ///     Config of the item conversion. Depends on the <see cref="Type"/> of the conversion.
        /// </summary>
        public ConversionConfig Config { get; set; }

        /// <summary>
        ///     Checks if a custom item conversion is valid.
        /// </summary>
        /// <returns>true if all criteria is met</returns>
        public bool IsValid()
        {
            return Config.Station != null && Config.FromItem != null && Config.ToItem != null;
        }

        public override bool Equals(object obj)
        {
            return obj.GetHashCode() == GetHashCode();
        }

        public override int GetHashCode()
        {
            return ToString().GetStableHashCode();
        }

        public override string ToString()
        {
            return $"({Type}) {Config.FromItem} -> {Config.ToItem}";
        }
    }
}
