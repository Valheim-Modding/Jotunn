using System.Collections.Generic;

namespace Jotunn.Configs
{
    public class LocalizationConfig
    {
        public string Language { get; set; } = "English";
        public Dictionary<string, string> Translations = new Dictionary<string, string>();

        public LocalizationConfig()
        {

        }

        public LocalizationConfig(string language)
        {
            Language = language;
        }
    }
}