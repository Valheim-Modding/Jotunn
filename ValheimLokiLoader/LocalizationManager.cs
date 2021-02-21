using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ValheimLokiLoader
{
    public static class LocalizationManager
    {
        public static void AddTranslation(string key, string text)
        {
            Util.InvokePrivate(Localization.instance, "AddWord", new object[] { key, text });
        }
    }
}
