using System.Collections.Generic;
using System.Linq;
using BepInEx;

namespace Jotunn.Entities
{
    /// <summary> Wrapper to hold each mod localization data. </summary>
    public class CustomTranslation : CustomEntity
    {
        /// <summary> Map that work as [language][token] = translation. </summary>
        private Dictionary<string, Dictionary<string, string>> map { get; set; }

        /// <summary> Default constuctor. </summary>
        public CustomTranslation() => map = new Dictionary<string, Dictionary<string, string>>();

        /// <summary> SourceMod hint constuctor. </summary>
        /// <param name="sourceMod"> Mod data in the shape of BepInPlugin class. </param>
        public CustomTranslation(BepInPlugin sourceMod) : base(sourceMod) => map = new Dictionary<string, Dictionary<string, string>>();

        /// <summary> Add translation to the translation dictionary. </summary>
        /// <param name="language"> Language of the translation you want to add. </param>
        /// <param name="tokenValue"> Token-Value dictionary. </param>
        public void AddTranslation(in string language, Dictionary<string, string> tokenValue)
        {
            if (!map.ContainsKey(language))
            {
                map.Add(language, new Dictionary<string, string>());
            }
            foreach (var tv in tokenValue)
            {
                map[language][tv.Key] = tv.Value;
            }
        }

        /// <summary> Add translation to the translation dictionary. </summary>
        /// <param name="language"> Language of the translation you want to add. </param>
        /// <param name="tokenValue"> Token-Value dictionary. </param>
        public void AddTranslation(in string language, IDictionary<string, object> tokenValue)
        {
            if (!map.ContainsKey(language))
            {
                map.Add(language, new Dictionary<string, string>());
            }
            foreach (var tv in tokenValue)
            {
                map[language][tv.Key] = (string)tv.Value;
            }
        }

        /// <summary> Add translation to the translation dictionary. </summary>
        /// <param name="language"> Language of the translation you want to add. </param>
        /// <param name="token"> Token of the translation you want to add. </param>
        /// <param name="translation"> The translation. </param>
        public void AddTranslation(in string language, in string token, string translation)
        {
            if (!map.ContainsKey(language))
            {
                map.Add(language, new Dictionary<string, string>());
            }
            map[language][token] = translation;
        }

        /// <summary> Retrieve translations for given language. </summary>
        public IReadOnlyList<string> Getlanguages() => map.Keys.ToList();

        /// <summary> Retrieve translations for given language. </summary>
        /// <param name="language"> Language of the translation you want to retrieve. </param>
        public IReadOnlyDictionary<string, string> GetTranslations(in string language) => map.TryGetValue(language, out var x) ? x : null;

        /// <summary> Retrieve Translation if it exists. </summary>
        /// <param name="language"> Language of the translation you want to retrieve. </param>
        /// <param name="token"> Token of the translation you want to retrieve. </param>
        /// <param name="translation"> String with the result of the search or null if unsuccessful. </param>
        /// <returns> True if found the translation, false if not. </returns>
        public bool TryTranslate(in string language, in string token, out string translation)
        {
            translation = null;
            if (map.TryGetValue(language, out var Map2))
            {
                if (Map2.TryGetValue(token, out var value))
                {
                    translation = value;
                }
            }
            return translation != null;
        }

        /// <summary> Checks if a translation exists for given language and token. </summary>
        /// <param name="language"> Language being checked. </param>
        /// <param name="token"> Token being checked. </param>
        /// <returns> The translation. </returns>
        public bool Contains(in string language, in string token)
        {
            if (map.TryGetValue(language, out var translations))
            {
                return translations.ContainsValue(token);
            }
            return false;
        }
    }
}
