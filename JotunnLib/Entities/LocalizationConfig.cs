using System.Collections.Generic;

namespace JotunnLib.Entities
{
    public class LocalizationConfig
    {
        public Dictionary<string, string> Translations = new Dictionary<string, string>();

        public LocalizationConfig(string language)
        {
            Language = language;
        }

        public string Language { get; set; } = "English";
    }
}