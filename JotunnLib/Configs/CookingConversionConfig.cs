using System.Collections.Generic;
using Jotunn.Entities;

namespace Jotunn.Configs
{
    /// <summary>
    ///     Used to add new ItemConversions to the CookingStation
    /// </summary>
    public class CookingConversionConfig : ConversionConfig
    {
        /// <summary>
        ///     Amount of time it takes to perform the conversion.
        /// </summary>
        public float CookTime { get; set; } = 10f;

        /// <summary>
        ///     Turns the CookingConversionConfig into a Valheim CookingStation.ItemConversion item.
        /// </summary>
        /// <returns>The Valheim ItemConversion</returns>
        public CookingStation.ItemConversion GetItemConversion()
        {
            CookingStation.ItemConversion conv = new CookingStation.ItemConversion()
            {
                m_cookTime = CookTime,
                m_from = Mock<ItemDrop>.Create(FromItem),
                m_to = Mock<ItemDrop>.Create(ToItem),
            };

            return conv;
        }

        /// <summary>
        ///     Loads a single CookingConversionConfig from a JSON string
        /// </summary>
        /// <param name="json">JSON text</param>
        /// <returns>Loaded CookingConversionConfig</returns>
        public static CookingConversionConfig FromJson(string json)
        {
            return SimpleJson.SimpleJson.DeserializeObject<CookingConversionConfig>(json);
        }

        /// <summary>
        ///     Loads a list of CookingConversionConfigs from a JSON string
        /// </summary>
        /// <param name="json">JSON text</param>
        /// <returns>Loaded list of CookingConversionConfigs</returns>
        public static List<CookingConversionConfig> ListFromJson(string json)
        {
            return SimpleJson.SimpleJson.DeserializeObject<List<CookingConversionConfig>>(json);
        }
    }
}
