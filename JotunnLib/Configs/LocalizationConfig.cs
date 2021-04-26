using System.Collections.Generic;

namespace Jotunn.Configs
{
    /// <summary>
    ///     Configuration class for adding custom localizations.
    /// </summary>
    public class LocalizationConfig
    {
        /// <summary>
        ///     Language of this localization. Defaults to English.
        /// </summary>
        public string Language { get; set; } = "English";

        /// <summary>
        ///     Dictionary of tokens and their respective translation in this configs language.
        /// </summary>
        public Dictionary<string, string> Translations = new Dictionary<string, string>();

        /// <summary>
        ///     A new localization for a specific language.
        /// </summary>
        /// <param name="language">Name of the language</param>
        public LocalizationConfig(string language)
        {
            Language = language;
        }
    }
}
