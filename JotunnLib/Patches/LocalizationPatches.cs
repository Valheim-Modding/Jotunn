using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using JotunnLib.Utils;
using JotunnLib.Managers;

namespace JotunnLib.Patches
{
    class LocalizationPatches
    {
        [HarmonyPatch(typeof(Localization), "LoadLanguages")]
        public static class LoadLanguagesPatch
        {
            public static void Postfix(ref Localization __instance, ref List<string> __result)
            {
                LocalizationManager.Instance.Register();

                // Add in localized languages that do not yet exist
                foreach (string language in LocalizationManager.Instance.Languages)
                {
                    if (!__result.Contains(language))
                    {
                        __result.Add(language);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Localization), "SetupLanguage")]
        public static class SetupLanguagePatch
        {
            public static void Postfix(ref Localization __instance, string language)
            {
                Debug.Log("\t-> SetupLanguage called");

                // Register & load localizations for selected language
                LocalizationManager.Instance.Register();
                LocalizationManager.Instance.Load(__instance, language);
            }
        }
    }
}
