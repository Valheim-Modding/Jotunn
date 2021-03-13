using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JotunnLib.Entities
{
    internal class LocalizationConfig
    {
        public string Language { get; set; } = "English";
        public Dictionary<string, string> Translations = new Dictionary<string, string>();
    }
}
