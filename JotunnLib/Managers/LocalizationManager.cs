﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
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
        internal readonly List<CustomLocalization> Localizations = new List<CustomLocalization>();
        
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
        public void Init()
        {
            On.FejdStartup.SetupGui += LoadAndSetupModLanguages;

            DoQuoteLineSplit = (Func<StringReader, List<List<string>>>)
                Delegate.CreateDelegate(
                    typeof(Func<StringReader, List<List<string>>>),
                    null,
                    typeof(Localization).GetMethod(
                        nameof(Localization.DoQuoteLineSplit),
                        ReflectionHelper.AllBindingFlags));
        }

        // Some mod could have initialized Localization before all mods are loaded.
        // See https://github.com/Valheim-Modding/Jotunn/issues/193
        private void LoadAndSetupModLanguages(On.FejdStartup.orig_SetupGui orig, FejdStartup self)
        {
            orig(self);
            
            On.Localization.LoadLanguages += Localization_LoadLanguages;
            On.Localization.SetupLanguage += Localization_SetupLanguage;

            var tmp = new HashSet<string>(Localization.instance.m_languages.ToList());
            foreach (var language in Localization.instance.LoadLanguages())
            {
                tmp.Add(language);
            }

            Localization.instance.m_languages.Clear();
            Localization.instance.m_languages.AddRange(tmp);

            Localization.instance.SetupLanguage(DefaultLanguage);
            string lang = PlayerPrefs.GetString("language", DefaultLanguage);
            if (lang != DefaultLanguage)
            {
                Localization.instance.SetupLanguage(lang);
            }
            
            InvokeOnLocalizationAdded();
            
            On.Localization.LoadLanguages -= Localization_LoadLanguages;
            On.Localization.SetupLanguage -= Localization_SetupLanguage;
        }

        private void InvokeOnLocalizationAdded()
        {
            OnLocalizationAdded?.SafeInvoke();
        }

        private List<string> Localization_LoadLanguages(On.Localization.orig_LoadLanguages orig, Localization self)
        {
            var result = orig(self);

            Logger.LogInfo("Loading custom localizations");

            try
            {
                AutomaticLocalizationLoading();
            
                // Add Jötunn's default localization at the end of the list
                AddLocalization(JotunnLocalization);

                // Add in localized languages that do not yet exist
                foreach (var ct in Localizations)
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
            catch (Exception ex)
            {
                Logger.LogWarning($"Exception caught while loading custom localizations: {ex}");
            }

            return result;
        }

        private bool Localization_SetupLanguage(On.Localization.orig_SetupLanguage orig, Localization self, string language)
        {
            var result = orig(self, language);
            
            Logger.LogInfo($"Adding tokens for language '{language}'");
            
            try
            {
                foreach (var ct in Localizations)
                {
                    var langDic = ct.GetTranslations(language);
                    if (langDic == null)
                    {
                        continue;
                    }

                    Logger.LogDebug($"Adding tokens for '{ct}'");

                    foreach (var pair in langDic)
                    {
                        Logger.LogDebug($"Added translation: {pair.Key} -> {pair.Value}");
                        self.AddWord(pair.Key, pair.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Exception caught while adding custom localizations: {ex}");
            }

            return result;
        }

        private IEnumerable<FileInfo> GetTranslationFiles(string path, string searchPattern) => GetTranslationFiles(new DirectoryInfo(path), searchPattern);

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

        private void AutomaticLocalizationLoading()
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
            if (Localizations.Any(x => x.SourceMod == customLocalization.SourceMod))
            {
                Logger.LogWarning($"{customLocalization} already added");
                return false;
            }

            Localizations.Add(customLocalization);

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
            var ret = Localizations.FirstOrDefault(ctx => ctx.SourceMod == sourceMod);

            if (ret != null)
            {
                return ret;
            }

            if (sourceMod == Main.Instance.Info.Metadata)
            {
                return JotunnLocalization;
            }

            ret = new CustomLocalization(sourceMod);
            Localizations.Add(ret);
            return ret;
        }

        /// <summary>
        ///     Retrieve a translation if it's found in any CustomLocalization or <see cref="Localization.Translate"/>.
        /// </summary>
        /// <param name="word"> Word to translate. </param>
        /// <returns> Translated word in player language or english as a fallback. </returns>
        public string TryTranslate(string word)
        {
            var cleanedWord = word.TrimStart(TokenFirstChar);

            foreach (var ct in Localizations)
            {
                var translation = ct.TryTranslate(word);
                if (translation != null && translation[0] != '[')
                {
                    return translation;
                }
            }
            
            return Localization.instance.Translate(cleanedWord);
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
