using System.Collections.Generic;
using Jotunn.Entities;

namespace Jotunn.Configs
{
    /// <summary>
    ///     Used to add new ItemConversions to the Smelter
    /// </summary>
    public class SmelterConversionConfig : ConversionConfig
    {
        /// <summary>
        ///     Turns the SmelterConversionConfig into a Valheim Smelter.ItemConversion item.
        /// </summary>
        /// <returns>The Valheim ItemConversion</returns>
        public Smelter.ItemConversion GetItemConversion()
        {
            Smelter.ItemConversion conv = new Smelter.ItemConversion()
            {
                m_from = Mock<ItemDrop>.Create(FromItem),
                m_to = Mock<ItemDrop>.Create(ToItem),
            };

            return conv;
        }

        /// <summary>
        ///     Loads a single SmelterConversionConfig from a JSON string
        /// </summary>
        /// <param name="json">JSON text</param>
        /// <returns>Loaded SmelterConversionConfig</returns>
        public static SmelterConversionConfig FromJson(string json)
        {
            return SimpleJson.SimpleJson.DeserializeObject<SmelterConversionConfig>(json);
        }

        /// <summary>
        ///     Loads a list of SmelterConversionConfigs from a JSON string
        /// </summary>
        /// <param name="json">JSON text</param>
        /// <returns>Loaded list of SmelterConversionConfigs</returns>
        public static List<SmelterConversionConfig> ListFromJson(string json)
        {
            return SimpleJson.SimpleJson.DeserializeObject<List<SmelterConversionConfig>>(json);
        }
    }
}
