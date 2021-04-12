using System.Collections.Generic;
using UnityEngine;
using JotunnLib.Managers;

namespace JotunnLib.Configs
{
    public class KeyHintConfig
    {
        /// <summary>
        ///     Item for which the KeyHint should be displayed when equipped.<br />
        ///     Must be the name of the prefab as registered in the <see cref="ItemManager"/>.
        /// </summary>
        public string Item { get; set; }

        /// <summary>
        ///      Create the actual <see cref="GameObject"/> from this config.
        /// </summary>
        /// <returns></returns>
        public GameObject GetKeyHint()
        {
            GameObject ret = new GameObject($"KeyHint{Item}");

            return ret;
        }

        /// <summary>
        ///     Loads a single KeyHintConfig from a JSON string
        /// </summary>
        /// <param name="json">JSON text</param>
        /// <returns>Loaded KeyHintConfig</returns>
        public static KeyHintConfig FromJson(string json)
        {
            return SimpleJson.SimpleJson.DeserializeObject<KeyHintConfig>(json);
        }

        /// <summary>
        ///     Loads a list of KeyHintConfigs from a JSON string
        /// </summary>
        /// <param name="json">JSON text</param>
        /// <returns>Loaded list of KeyHintConfigs</returns>
        public static List<KeyHintConfig> ListFromJson(string json)
        {
            return SimpleJson.SimpleJson.DeserializeObject<List<KeyHintConfig>>(json);
        }
    }
}
