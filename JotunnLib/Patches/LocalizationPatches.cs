using System;
using System.Collections.Generic;
using UnityEngine;
using JotunnLib.Utils;
using JotunnLib.Managers;

namespace JotunnLib.Patches
{
    class LocalizationPatches : PatchInitializer
    {
        internal override void Init()
        {
            On.Localization.LoadLanguages += Localization_LoadLanguages;
            On.Localization.SetupLanguage += Localization_SetupLanguage;
        }

        private static bool Localization_SetupLanguage(On.Localization.orig_SetupLanguage orig, Localization self, string language)
        {
            bool result = orig(self, language);
            Debug.Log("\t-> SetupLanguage called");

            // Register & load localizations for selected language
            LocalizationManager.Instance.Register();
            LocalizationManager.Instance.Load(self, language);

            return result;
        }

        private static List<string> Localization_LoadLanguages(On.Localization.orig_LoadLanguages orig, Localization self)
        {
            List<string> result = orig(self);
            LocalizationManager.Instance.Register();

            // Add in localized languages that do not yet exist
            foreach (string language in LocalizationManager.Instance.Languages)
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
