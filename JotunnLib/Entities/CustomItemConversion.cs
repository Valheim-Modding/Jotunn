using Jotunn.Configs;
using Jotunn.Managers;

namespace Jotunn.Entities
{
    /// <summary>
    ///     Main interface for adding custom item conversions to the game.<br />
    ///     Supports and combines conversions for the cooking station, fermenter and smelter.<br />
    ///     All custom item conversions have to be wrapped inside this class to add it to Jötunns <see cref="ItemManager"/>.
    /// </summary>
    public class CustomItemConversion : CustomEntity
    {
        /// <summary>
        ///     Type of the conversion component used in game.
        /// </summary>
        public enum ConversionType
        {
            /// <summary>
            ///     Add a conversion to a station with the CookingStation component attached.
            /// </summary>
            CookingStation,
            /// <summary>
            ///     Add a conversion to a station with the Fermenter component attached.
            /// </summary>
            Fermenter,
            /// <summary>
            ///     Add a conversion to a station with the Smelter component attached.
            /// </summary>
            Smelter,
            /// <summary>
            ///     Add a conversion to a station with the Incinerator component attached.
            /// </summary>
            Incinerator
        };

        /// <summary>
        ///     Type of the item conversion. Defines to which station the conversion is added.
        /// </summary>
        public ConversionType Type { get; }

        /// <summary>
        ///     Config of the item conversion. Depends on the <see cref="Type"/> of the conversion.
        /// </summary>
        public ConversionConfig Config { get; }

        /// <summary>
        ///     Indicator if the conversion needs fixing.
        /// </summary>
        internal bool FixReference = true;

        /// <summary>
        ///     Actual ItemConversion type as <see cref="object"/>. Needs to be cast according to <see cref="ConversionType"/>.
        /// </summary>
        internal object ItemConversion
        {
            get
            {
                return Type switch
                {
                    ConversionType.CookingStation => _cookingConversion,
                    ConversionType.Fermenter => _fermenterConversion,
                    ConversionType.Smelter => _smelterConversion,
                    ConversionType.Incinerator => _incineratorConversion,
                    _ => null,
                };
            }
        }
        private CookingStation.ItemConversion _cookingConversion;
        private Fermenter.ItemConversion _fermenterConversion;
        private Smelter.ItemConversion _smelterConversion;
        private Incinerator.IncineratorConversion _incineratorConversion;

        /// <summary>
        ///     Create a custom item conversion. Depending on the config class this custom
        ///     conversion represents one of the following item conversions:<br />
        ///     <list type="bullet">
        ///         <item>CookingStation.ItemConversion</item>
        ///         <item>Fermenter.ItemConversion</item>
        ///         <item>Smelter.ItemConversion</item>
        ///     </list>
        /// </summary>
        /// <param name="config">The item conversion config</param>
        public CustomItemConversion(ConversionConfig config)
        {
            if (config is CookingConversionConfig cookConfig)
            {
                Type = ConversionType.CookingStation;
                _cookingConversion = cookConfig.GetItemConversion();
            }
            if (config is FermenterConversionConfig fermentConfig)
            {
                Type = ConversionType.Fermenter;
                _fermenterConversion = fermentConfig.GetItemConversion();
            }
            if (config is SmelterConversionConfig smeltConfig)
            {
                Type = ConversionType.Smelter;
                _smelterConversion = smeltConfig.GetItemConversion();
            }
            if (config is IncineratorConversionConfig inciConfig)
            {
                Type = ConversionType.Incinerator;
                _incineratorConversion = inciConfig.GetIncineratorConversion();
            }

            Config = config;
        }

        /// <summary>
        ///     Checks if a custom item conversion is valid.
        /// </summary>
        /// <returns>true if all criteria is met</returns>
        public bool IsValid()
        {
            return Config.Station != null && Config.FromItem != null && Config.ToItem != null;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj.GetHashCode() == GetHashCode();
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return ToString().GetStableHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{Config.Station} ({Type}): {Config.FromItem} -> {Config.ToItem}";
        }
    }
}
