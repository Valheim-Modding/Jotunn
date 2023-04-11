using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Utils;
using UnityEngine;
using Paths = Jotunn.Utils.Paths;

namespace Jotunn.Managers
{
    /// <summary> 
    ///     Manager for handling localizations for all custom content added to the game.
    /// </summary>
    public class LocalizationManager : IManager
    {
        /// <summary>
        ///     List where all data is collected.
        /// </summary>
        internal readonly Dictionary<string, CustomLocalization> Localizations = new Dictionary<string, CustomLocalization>();

        /// <summary>
        ///     Your token must start with this character.
        /// </summary>
        public const char TokenFirstChar = '$';

        /// <summary>
        ///     Default language of the game.
        /// </summary>
        public const string DefaultLanguage = "English";

        /// <summary>
        ///     Name of the folder that will hold the custom .json translations files.
        /// </summary>
        public const string TranslationsFolderName = "Translations";

        /// <summary>
        ///     Name of the community translation files that will be the first custom languages files loaded before any others.
        /// </summary>
        public const string CommunityTranslationFileName = "community_translation.json";

        /// <summary>
        ///     String of chars not allowed in a token string.
        /// </summary>
        internal const string ForbiddenChars = " (){}[]+-!?/\\\\&%,.:-=<>\n";

        /// <summary>
        ///     Array of chars not allowed in a token string.
        /// </summary>
        internal static readonly char[] ForbiddenCharsArr = ForbiddenChars.ToCharArray();

        /// <summary>
        ///     The singleton instance of this manager.
        /// </summary>
        public static LocalizationManager Instance => _instance ??= new LocalizationManager();
        private static LocalizationManager _instance;

        /// <summary>
        ///     Hide .ctor
        /// </summary>
        private LocalizationManager() {}

        /// <summary>
        ///     Localizations for internal use.
        /// </summary>
        internal CustomLocalization JotunnLocalization = new CustomLocalization(Main.Instance.Info.Metadata);

        /// <summary>
        ///     Event that gets fired after all custom localization has been added to the game.
        ///     Use this event if you need to translate strings using the vanilla <see cref="Localization"/> class.
        ///     Your code will execute every time the localization gets reset (on every menu start).
        ///     If you want to execute just once you will need to unregister from the event after execution.
        /// </summary>
        public static event Action OnLocalizationAdded;

        /// <summary>
        ///     Call into unity's DoQuoteLineSplit.
        /// </summary>
        internal static Func<StringReader, List<List<string>>> DoQuoteLineSplit;

        /// <summary>
        ///     Initialize localization manager.
        /// </summary>
        void IManager.Init()
        {
            Main.Harmony.PatchAll(typeof(Patches));

            DoQuoteLineSplit = (Func<StringReader, List<List<string>>>)
                Delegate.CreateDelegate(
                    typeof(Func<StringReader, List<List<string>>>),
                    null,
                    typeof(Localization).GetMethod(
                        nameof(Localization.DoQuoteLineSplit),
                        ReflectionHelper.AllBindingFlags));

            AddLocalization(JotunnLocalization);
        }

        private static class Patches
        {
            [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.SetupGui)), HarmonyPostfix]
            private static void LoadAndSetupModLanguages() => Instance.LoadAndSetupModLanguages(Localization.instance);

            [HarmonyPatch(typeof(Localization), nameof(Localization.LoadLanguages)), HarmonyPostfix]
            private static void Localization_LoadLanguages(ref List<string> __result) => Instance.AddLanguages(ref __result);

            [HarmonyPatch(typeof(Localization), nameof(Localization.SetupLanguage)), HarmonyPostfix]
            private static void Localization_SetupLanguage(Localization __instance, string language) => Instance.AddTranslations(__instance, language);
        }

        // Some mod could have initialized Localization before all mods are loaded.
        // See https://github.com/Valheim-Modding/Jotunn/issues/193
        private void LoadAndSetupModLanguages(Localization localization)
        {
            AddLanguages(ref localization.m_languages);
            AddTranslations(localization, GetPlayerLanguage());

            InvokeOnLocalizationAdded();
        }

        private void InvokeOnLocalizationAdded()
        {
            OnLocalizationAdded?.SafeInvoke();
        }

        private void AddLanguages(ref List<string> result)
        {
            // Add in localized languages that do not yet exist
            foreach (var ct in Localizations.Values)
            {
                foreach (var language in ct.GetLanguages())
                {
                    if (!result.Contains(language))
                    {
                        result.Add(language);
                    }
                }
            }
        }

        private void AddTranslations(Localization localization, string language)
        {
            foreach (var pair in GetAllTranslations(DefaultLanguage))
            {
                localization.AddWord(pair.Key, pair.Value);
            }

            if (string.IsNullOrEmpty(language) || language == DefaultLanguage)
            {
                return;
            }

            foreach (var pair in GetAllTranslations(language))
            {
                localization.AddWord(pair.Key, pair.Value);
            }
        }

        private IEnumerable<KeyValuePair<string, string>> GetAllTranslations(string language)
        {
            return Localizations.Values.SelectMany(ct => ct.GetTranslations(language));
        }

        internal static string GetPlayerLanguage()
        {
            return PlayerPrefs.GetString("language", DefaultLanguage);
        }

        private IEnumerable<FileInfo> GetTranslationFiles(string path, string searchPattern)
        {
            return GetTranslationFiles(new DirectoryInfo(path), searchPattern);
        }

        private IEnumerable<FileInfo> GetTranslationFiles(DirectoryInfo pathDirectoryInfo, string searchPattern)
        {
            if (!pathDirectoryInfo.Exists) yield break;

            foreach (var path in Directory
                .GetFiles(pathDirectoryInfo.FullName, searchPattern, SearchOption.AllDirectories).Where(path =>
                    new DirectoryInfo(path).Parent?.Parent?.Name == TranslationsFolderName))
            {
                yield return new FileInfo(path);
            }
        }

        internal void LoadingAutomaticLocalizations()
        {
            var jsonFormat = new HashSet<FileInfo>();
            var unityFormat = new HashSet<FileInfo>();

            // Json format community files
            foreach (var fileInfo in GetTranslationFiles(Paths.LanguageTranslationsFolder, CommunityTranslationFileName))
            {
                jsonFormat.Add(fileInfo);
            }

            foreach (var fileInfo in GetTranslationFiles(Paths.LanguageTranslationsFolder, "*.json"))
            {
                jsonFormat.Add(fileInfo);
            }

            foreach (var fileInfo in GetTranslationFiles(Paths.LanguageTranslationsFolder, "*.language"))
            {
                unityFormat.Add(fileInfo);
            }

            foreach (var fileInfo in jsonFormat)
            {
                try
                {
                    var mod = BepInExUtils.GetPluginInfoFromPath(fileInfo)?.Metadata;
                    GetLocalization(mod ?? Main.Instance.Info.Metadata).AddFileByPath(fileInfo.FullName, true);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Exception caught while loading localization file {fileInfo}: {ex}");
                }
            }

            foreach (var fileInfo in unityFormat)
            {
                try
                {
                    var mod = BepInExUtils.GetPluginInfoFromPath(fileInfo)?.Metadata;
                    GetLocalization(mod ?? Main.Instance.Info.Metadata).AddFileByPath(fileInfo.FullName);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Exception caught while loading localization file {fileInfo}: {ex}");
                }
            }
        }

        /// <summary>
        ///     Add your mod's custom localization. Only one <see cref="CustomLocalization"/> can be added per mod.
        /// </summary>
        /// <param name="customLocalization">The localization to add.</param>
        /// <returns>true if the custom localization was added to the manager.</returns>
        public bool AddLocalization(CustomLocalization customLocalization)
        {
            if (Localizations.ContainsKey(customLocalization.SourceMod.GUID))
            {
                Logger.LogWarning(customLocalization.SourceMod, $"{customLocalization} already added");
                return false;
            }

            Localizations.Add(customLocalization.SourceMod.GUID, customLocalization);
            return true;
        }

        /// <summary>
        ///     Get the CustomLocalization for your mod.
        ///     Creates a new <see cref="CustomLocalization"/> if no localization was added before.
        /// </summary>
        /// <returns>Existing or newly created <see cref="CustomLocalization"/>.</returns>
        public CustomLocalization GetLocalization()
        {
            return GetLocalization(BepInExUtils.GetSourceModMetadata());
        }

        /// <summary>
        ///     Get the CustomLocalization for a given mod.
        ///     Creates a new <see cref="CustomLocalization"/> if no localization was added before.
        /// </summary>
        /// <returns>Existing or newly created <see cref="CustomLocalization"/>.</returns>
        internal CustomLocalization GetLocalization(BepInPlugin sourceMod)
        {
            if (Localizations.TryGetValue(sourceMod.GUID, out CustomLocalization localization))
            {
                return localization;
            }

            localization = new CustomLocalization(sourceMod);
            AddLocalization(localization);
            return localization;
        }

        /// <summary>
        ///     Retrieve a translation if it's found in any CustomLocalization or <see cref="Localization.Translate"/>.
        /// </summary>
        /// <param name="word"> Word to translate. </param>
        /// <returns> Translated word in player language or english as a fallback. </returns>
        public string TryTranslate(string word)
        {
            var cleanedWord = word.TrimStart(TokenFirstChar);
            string translation;

            foreach (var localization in Localizations.Values)
            {
                // skip the Jotunn localization to allow other modded translations first
                if (localization == JotunnLocalization)
                {
                    continue;
                }

                translation = localization.TryTranslate(word);
                if (IsValidTranslation(translation))
                {
                    return translation;
                }
            }

            // now search within the Jotunn localization explicitly
            translation = JotunnLocalization.TryTranslate(word);
            if (IsValidTranslation(translation))
            {
                return translation;
            }

            // fallback to vanilla localization if nothing found
            if (Localization.m_instance != null)
            {
                return Localization.m_instance.Translate(cleanedWord);
            }

            return $"[{word}]";
        }

        private static bool IsValidTranslation(string translation)
        {
            return !string.IsNullOrEmpty(translation) && translation[0] != '[';
        }

#region Obsolete

        /// <summary> 
        ///     Registers a new Localization for a language.
        /// </summary>
        /// <param name="config"> Wrapper which contains a language and a Token-Value dictionary. </param>
        [Obsolete("Use AddLocalization(CustomLocalization) instead")]
        public void AddLocalization(LocalizationConfig config)
            => GetLocalization().AddTranslation(config.Language, config.Translations);

        /// <summary> 
        ///     Registers a new Localization for a language.
        /// </summary>
        /// <param name="language"> The language being added. </param>
        /// <param name="localization"> Token-Value dictionary. </param>
        [Obsolete("Use AddLocalization(CustomLocalization) instead")]
        public void AddLocalization(string language, Dictionary<string, string> localization)
            => GetLocalization().AddTranslation(language, localization);

        /// <summary> 
        ///     Add a token and its value to the "English" language.
        /// </summary>
        /// <param name="token"> Token </param>
        /// <param name="value"> Translation. </param>
        /// <param name="forceReplace"> Replace the token if it already exists </param>
        [Obsolete("Use AddLocalization(CustomLocalization) instead")]
        public void AddToken(string token, string value, bool forceReplace = false)
            => GetLocalization().AddTranslation(token, value);

        /// <summary> 
        ///     Add a token and its value to the specified language (default to "English").
        /// </summary>
        /// <param name="token"> Token </param>
        /// <param name="value"> Translation. </param>
        /// <param name="language"> Language ID for this token. </param>
        /// <param name="forceReplace"> Replace the token if it already exists </param>
        [Obsolete("Use AddLocalization(CustomLocalization) instead")]
        public void AddToken(string token, string value, string language, bool forceReplace = false)
            => GetLocalization().AddTranslation(language, token, value);

        /// <summary> 
        ///     Add a file via absolute path.
        /// </summary>
        /// <param name="path"> Absolute path to file. </param>
        /// <param name="isJson"> Is the language file a json file. </param>
        [Obsolete("Use AddLocalization(CustomLocalization) instead")]
        public void AddPath(string path, bool isJson = false)
            => GetLocalization().AddFileByPath(path, isJson);

        /// <summary>
        ///     Add a json language file (match crowdin format).
        /// </summary>
        /// <param name="language"> Language for the json file, for example, "English" </param>
        /// <param name="fileContent"> Entire file as string </param>
        [Obsolete("Use AddLocalization(CustomLocalization) instead")]
        public void AddJson(string language, string fileContent)
            => GetLocalization().AddJsonFile(language, fileContent);

        /// <summary>
        ///     Add a language file that matches Valheim's language format.
        /// </summary>
        /// <param name="fileContent"> Entire file as string </param>
        [Obsolete("Use AddLocalization(CustomLocalization) instead")]
        public void AddLanguageFile(string fileContent)
            => GetLocalization().AddLanguageFile(fileContent);

#endregion
    }
}
