using System.Collections.Generic;
using System.Linq;
using JotunnLib.Managers;
using JotunnLib.Utils;
using UnityEngine;

namespace JotunnLib.Patches
{
    internal class LocalizationPatches : PatchInitializer
    {
        public override void Init()
        {
            On.Localization.LoadLanguages += Localization_LoadLanguages;
            On.Localization.SetupLanguage += Localization_SetupLanguage;
        }

        private static bool Localization_SetupLanguage(On.Localization.orig_SetupLanguage orig, Localization self, string language)
        {
            var result = orig(self, language);
            Debug.Log($"\t-> SetupLanguage called {language}");

            // Register & load localizations for selected language
            LocalizationManager.Instance.Register();
            LocalizationManager.Instance.Load(self, language);

            return result;
        }

        private static List<string> Localization_LoadLanguages(On.Localization.orig_LoadLanguages orig, Localization self)
        {
            var result = orig(self);
            LocalizationManager.Instance.Register();

            // Add in localized languages that do not yet exist
            foreach (var language in LocalizationManager.Instance.Localizations.Keys.OrderBy(x => x))
            {
                if (!result.Contains(language))
                {
                    result.Add(language);
                }
            }

            return result;
        }
    }
}