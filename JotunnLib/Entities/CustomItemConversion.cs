using Jotunn.Configs;
using Jotunn.Managers;

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

        public ConversionType Type;
        
        /// <summary>
        ///     Indicator if references from <see cref="Entities.Mock{T}"/>s will be replaced at runtime.
        /// </summary>
        public bool FixReference { get; set; } = false;

        /// <summary>
        ///     Checks if a custom item conversion is valid.
        /// </summary>
        /// <returns>true if all criteria is met</returns>
        public bool IsValid()
        {
            return true;
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
            return $"({Type.ToString()})";
        }
    }
}
