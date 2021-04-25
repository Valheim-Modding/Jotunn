using System.Collections.Generic;
using Jotunn.Managers;

namespace Jotunn.Configs
{
    /// <summary>
    ///     Used to add new ItemConversions to a CookingStation.
    /// </summary>
    public class ItemConversionConfig
    {
        /// <summary>
        ///     Amount of time it takes to perform the conversion.
        /// </summary>
        public float CookTime { get; set; } = 10f;

        /// <summary>
        ///     The name of the cooking station prefab for which to add this item conversion.
        /// </summary>
        public string CookingStation { get; set; }

        /// <summary>
        ///     The name of the item prefab you need to put on the CookingStation.
        /// </summary>
        public string FromItem { get; set; }

        /// <summary>
        ///     The name of the item prefab that your "FromItem" will be turned into.
        /// </summary>
        public string ToItem { get; set; }

        /// <summary>
        ///     Turns the ItemConversionConfig into a Valheim CookingStation.ItemConversion item.
        /// </summary>
        /// <returns>The Valheim ItemConversion</returns>
        public CookingStation.ItemConversion GetItemConversion()
        {
            ItemDrop fromItem = PrefabManager.Instance.GetPrefab(FromItem)?.GetComponent<ItemDrop>();
            ItemDrop toItem = PrefabManager.Instance.GetPrefab(ToItem)?.GetComponent<ItemDrop>();

            CookingStation.ItemConversion conv = new CookingStation.ItemConversion()
            {
                m_cookTime = CookTime,
                m_from = fromItem,
                m_to = toItem
            };

            return conv;
        }

        /// <summary>
        ///     Gets the cooking station component from the prefab name.
        /// </summary>
        /// <returns>The CookingStation component, or null if it is not valid</returns>
        public CookingStation GetCookingStation()
        {
            return PrefabManager.Instance.GetPrefab(CookingStation)?.GetComponent<CookingStation>();
        }

        /// <summary>
        ///     Loads a single ItemConversionConfig from a JSON string
        /// </summary>
        /// <param name="json">JSON text</param>
        /// <returns>Loaded ItemConversionConfig</returns>
        public static ItemConversionConfig FromJson(string json)
        {
            return SimpleJson.SimpleJson.DeserializeObject<ItemConversionConfig>(json);
        }

        /// <summary>
        ///     Loads a list of ItemConversionConfigs from a JSON string
        /// </summary>
        /// <param name="json">JSON text</param>
        /// <returns>Loaded list of ItemConversionConfigs</returns>
        public static List<ItemConversionConfig> ListFromJson(string json)
        {
            return SimpleJson.SimpleJson.DeserializeObject<List<ItemConversionConfig>>(json);
        }
    }
}
