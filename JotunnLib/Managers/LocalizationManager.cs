using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JotunnLib.Entities;
using JotunnLib.Utils;
using UnityEngine;

namespace JotunnLib.Managers
{
    /// <summary>
    ///     Handles all logic to do with managing the game's localizations.
    /// </summary>
    public class LocalizationManager : Manager
    {
        /// <summary>
        ///     Your token must start with this character.
        /// </summary>
        public const char TokenFirstChar = '$';

        /// <summary>
        ///     Default language of the game
        /// </summary>
        public const string DefaultLanguage = "English";

        /// <summary>
        ///     Name of the folder that will hold the custom .json translations files
        /// </summary>
        public const string TranslationsFolderName = "Translations";

        /// <summary>
        ///     Name of the community translation files that will be the first custom languages files loaded before any others.
        /// </summary>
        public const string CommunityTranslationFileName = "community_translation.json";

        /// <summary>
        ///     Call into unity's DoQuoteLineSplit
        /// </summary>
        internal static Func<StringReader, List<List<string>>> DoQuoteLineSplit;

        /// <summary>
        ///     Dictionary holding all localizations
        /// </summary>
        internal Dictionary<string, Dictionary<string, string>> Localizations = new Dictionary<string, Dictionary<string, string>>();

        private bool registered;
        public static LocalizationManager Instance { get; private set; }

        /// <summary>
        ///     Event for plugins to register their localizations via code
        /// </summary>
        public event EventHandler LocalizationRegister;

        public LocalizationManager()
        {
            if (Instance != null)
            {
                Logger.LogError("Error, two instances of singleton: " + GetType().Name);
                return;
            }

            Instance = this;
        }

        internal override void Clear()
        {
            Instance = null;
        }

        /// <summary>
        ///     Initialize localization manager
        /// </summary>
        internal override void Init()
        {
            On.Localization.LoadLanguages += Localization_LoadLanguages;
            On.Localization.SetupLanguage += Localization_SetupLanguage;

            var doQuoteLineSplitMethodInfo = typeof(Localization).GetMethod(nameof(Localization.DoQuoteLineSplit), ReflectionHelper.AllBindingFlags);
            DoQuoteLineSplit =
                (Func<StringReader, List<List<string>>>) Delegate.CreateDelegate(typeof(Func<StringReader, List<List<string>>>), null,
                    doQuoteLineSplitMethodInfo);
        }

        private bool Localization_SetupLanguage(On.Localization.orig_SetupLanguage orig, Localization self, string language)
        {
            var result = orig(self, language);
            Logger.LogDebug($"\t-> SetupLanguage called {language}");

            // Register & load localizations for selected language
            Register();
            Load(self, language);

            return result;
        }

        private List<string> Localization_LoadLanguages(On.Localization.orig_LoadLanguages orig, Localization self)
        {
            var result = orig(self);
            LocalizationManager.Instance.Register();

            // Add in localized languages that do not yet exist
            foreach (var language in Localizations.Keys.OrderBy(x => x))
            {
                if (!result.Contains(language))
                {
                    result.Add(language);
                }
            }

            return result;
        }

        /// <summary>
        ///     Register all plugin's localizations
        /// </summary>
        internal override void Register()
        {
            if (registered)
            {
                return;
            }

            Localizations.Clear();

            Logger.LogInfo("---- Registering custom localizations ----");

            AddLanguageFilesFromPluginFolder();

            LocalizationRegister?.Invoke(null, EventArgs.Empty);
            registered = true;
        }

        /// <summary>
        ///     Load the localization into Valheim's buffers
        /// </summary>
        /// <param name="localization"></param>
        /// <param name="language"></param>
        public void Load(Localization localization, string language)
        {
            // only if we have translations for this language
            if (!Localizations.ContainsKey(language))
            {
                return;
            }

            Logger.LogDebug($"Adding tokens for language {language}");
            AddTokens(localization, language);
        }

        /// <summary>
        ///     Registers a new translation for a word for the current language
        /// </summary>
        /// <param name="key">Key to translate</param>
        /// <param name="text">Translation</param>
        [Obsolete("Use either `RegisterLocalization(string language, Dictionary<string, string> localization)` or `RegisterLocalizationConfig(LocalizationConfig config)` instead", true)]
        public void RegisterTranslation(string key, string text)
        {
            ReflectionHelper.InvokePrivate(Localization.instance, "AddWord", new object[] {key, text});
        }

        /// <summary>
        ///     Registers a new Localization for a language
        /// </summary>
        /// <param name="localization">The localization for a language</param>
        public void RegisterLocalization(string language, Dictionary<string, string> localization)
        {
            if (string.IsNullOrEmpty(language))
            {
                Logger.LogError("Error, localization had null or empty language");
                return;
            }

            if (!Localizations.ContainsKey(language))
            {
                Localizations.Add(language, localization);
            }
            else
            {
                // Merge
                foreach (var kv in localization)
                {
                    Localizations[language][kv.Key] = kv.Value;
                }
            }
        }

        /// <summary>
        ///     Add localization config to existing localizations
        /// </summary>
        /// <param name="self"></param>
        /// <param name="config"></param>
        public void RegisterLocalizationConfig(LocalizationConfig config)
        {
            RegisterLocalization(config.Language, config.Translations);
        }

        /// <summary>
        ///     Search for and add localization files
        /// </summary>
        private void AddLanguageFilesFromPluginFolder()
        {
            // First search for the community translation
            var communityTranslationsFilePaths = new List<string>();
            var languagePaths = Directory.GetFiles(Paths.LanguageTranslationsFolder, CommunityTranslationFileName, SearchOption.AllDirectories);
            foreach (var path in languagePaths)
            {
                var isTranslationFile = Path.GetDirectoryName(Path.GetDirectoryName(path)).EndsWith(TranslationsFolderName);
                if (isTranslationFile)
                {
                    AddPath(path, true);
                    communityTranslationsFilePaths.Add(path);
                }
            }

            languagePaths = Directory.GetFiles(Paths.LanguageTranslationsFolder, "*.json", SearchOption.AllDirectories);
            foreach (var path in languagePaths)
            {
                if (communityTranslationsFilePaths.Contains(path))
                {
                    continue;
                }

                var isTranslationFile = Path.GetDirectoryName(Path.GetDirectoryName(path)).EndsWith(TranslationsFolderName);
                if (isTranslationFile)
                {
                    AddPath(path, true);
                }
            }

            languagePaths = Directory.GetFiles(Paths.LanguageTranslationsFolder, "*.language", SearchOption.AllDirectories);
            foreach (var path in languagePaths)
            {
                AddPath(path);
            }
        }


        /// <summary>
        ///     Add a token and its value to the specified language (default to English)
        /// </summary>
        /// <param name="token">token / key</param>
        /// <param name="value">value that will be printed in the game</param>
        /// <param name="language"></param>
        /// <param name="forceReplace">replace the token if it already exists</param>
        public void AddToken(string token, string value, string language = DefaultLanguage, bool forceReplace = false)
        {
            if (token[0] != TokenFirstChar)
            {
                throw new Exception($"Token first char should be {TokenFirstChar} ! (token : {token})");
            }

            Dictionary<string, string> languageDict = null;

            if (!forceReplace)
            {
                if (Localizations.TryGetValue(language, out languageDict))
                {
                    foreach (var pair in languageDict)
                    {
                        if (pair.Key == token)
                        {
                            throw new Exception($"Token named {token} already exist !");
                        }
                    }
                }
            }

            languageDict ??= GetLanguageDict(language);

            var tokenWithoutFirstChar = token.Substring(1);
            languageDict.Remove(tokenWithoutFirstChar);
            languageDict.Add(tokenWithoutFirstChar, value);
        }


        /// <summary>
        ///     Add a token and its value to the English language
        /// </summary>
        /// <param name="token">token / key</param>
        /// <param name="value">value that will be printed in the game</param>
        /// <param name="forceReplace">replace the token if it already exists</param>
        public void AddToken(string token, string value, bool forceReplace = false)
        {
            AddToken(token, value, DefaultLanguage, forceReplace);
        }


        /// <summary>
        ///     Add a file via absolute path
        /// </summary>
        /// <param name="path">Absolute path to file</param>
        /// <param name="isJson">Is the language file a json file</param>
        public void AddPath(string path, bool isJson = false)
        {
            if (path == null)
            {
                throw new NullReferenceException($"param {nameof(path)} is null");
            }

            var fileContent = File.ReadAllText(path);
            if (isJson)
            {
                var language = Path.GetFileName(Path.GetDirectoryName(path));
                AddJson(language, fileContent);

                Logger.LogInfo($"Added json language file {Path.GetFileName(path)}");
            }
            else
            {
                Add(fileContent);

                Logger.LogInfo($"Added language file {Path.GetFileName(path)}");
            }
        }

        /// <summary>
        ///     Add a language file (that match the game format)
        /// </summary>
        /// <param name="fileContent">Entire file as string</param>
        public void Add(string fileContent)
        {
            if (fileContent == null)
            {
                throw new NullReferenceException($"param {nameof(fileContent)} is null");
            }

            LoadLanguageFile(fileContent);
        }

        /// <summary>
        ///     Add a json language file (match crowdin format)
        /// </summary>
        /// <param name="language">Language for the json file, example : "English"</param>
        /// <param name="fileContent">Entire file as string</param>
        public void AddJson(string language, string fileContent)
        {
            if (fileContent == null)
            {
                throw new NullReferenceException($"param {nameof(fileContent)} is null");
            }

            LoadJsonLanguageFile(language, fileContent);
        }

        /// <summary>
        ///     Load unity style language file
        /// </summary>
        /// <param name="fileContent"></param>
        private void LoadLanguageFile(string fileContent)
        {
            var stringReader = new StringReader(fileContent);
            var languages = stringReader.ReadLine().Split(',');

            foreach (var keyAndValues in DoQuoteLineSplit(stringReader))
            {
                if (keyAndValues.Count != 0)
                {
                    var token = keyAndValues[0];
                    if (!token.StartsWith("//") && token.Length != 0)
                    {
                        for (var i = 0; i < languages.Length; i++)
                        {
                            var language = languages[i];

                            var tokenValue = keyAndValues[i];
                            if (string.IsNullOrEmpty(tokenValue) || tokenValue[0] == '\r')
                            {
                                tokenValue = keyAndValues[1];
                            }

                            var languageDict = GetLanguageDict(language);
                            languageDict[token]= tokenValue;
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Get the dictionary for a specific language
        /// </summary>
        /// <param name="language"></param>
        /// <returns></returns>
        private Dictionary<string, string> GetLanguageDict(string language)
        {
            if (!Localizations.ContainsKey(language))
            {
                Localizations.Add(language, new Dictionary<string, string>());
            }

            return Localizations[language];
        }

        /// <summary>
        ///     Load community translation file
        /// </summary>
        /// <param name="language"></param>
        /// <param name="fileContent"></param>
        private void LoadJsonLanguageFile(string language, string fileContent)
        {
            var languageDict = GetLanguageDict(language);

            var json = (IDictionary<string, object>) SimpleJson.SimpleJson.DeserializeObject(fileContent);

            foreach (var pair in json)
            {
                var token = pair.Key;
                var tokenValue = pair.Value;

                languageDict.Remove(token);
                languageDict.Add(token, (string) tokenValue);
            }
        }

        /// <summary>
        ///     Add tokens to the respective localization
        /// </summary>
        /// <param name="self"></param>
        /// <param name="language"></param>
        private void AddTokens(Localization self, string language)
        {
            if (Localizations.TryGetValue(language, out var tokens))
            {
                foreach (var pair in tokens)
                {
                    Logger.LogDebug("\tAdded translation: " + pair.Key + " -> " + pair.Value);
                    self.AddWord(pair.Key, pair.Value);
                }
            }
        }
    }
}