using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jotunn.Configs;
using Jotunn.Utils;
using UnityEngine;

namespace Jotunn.Managers
{
    /// <summary>
    ///     Manager for handling localizations for all custom content added to the game.
    /// </summary>
    public class LocalizationManager : IManager
    {
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

        private const string LocalizationEndChars = " (){}[]+-!?/\\\\&%,.:-=<>\n";

        private static LocalizationManager _instance;
        /// <summary>
        ///     The singleton instance of this manager.
        /// </summary>
        public static LocalizationManager Instance
        {
            get
            {
                if (_instance == null) _instance = new LocalizationManager();
                return _instance;
            }
        }

        /// <summary>
        ///     Call into unity's DoQuoteLineSplit.
        /// </summary>
        internal static Func<StringReader, List<List<string>>> DoQuoteLineSplit;

        /// <summary>
        ///     Dictionary holding all localizations.
        /// </summary>
        internal Dictionary<string, Dictionary<string, string>> Localizations = new Dictionary<string, Dictionary<string, string>>();

        /// <summary>
        ///     Initialize localization manager.
        /// </summary>
        public void Init()
        {
            On.FejdStartup.SetupGui += LoadAndSetupModLanguages;

            var doQuoteLineSplitMethodInfo = typeof(Localization).GetMethod(nameof(Localization.DoQuoteLineSplit), ReflectionHelper.AllBindingFlags);
            DoQuoteLineSplit =
                (Func<StringReader, List<List<string>>>) Delegate.CreateDelegate(typeof(Func<StringReader, List<List<string>>>), null,
                    doQuoteLineSplitMethodInfo);
        }

        // Some mod could have initialized Localization before all mods are loaded.
        // See https://github.com/Valheim-Modding/Jotunn/issues/193
        private void LoadAndSetupModLanguages(On.FejdStartup.orig_SetupGui orig, FejdStartup self)
        {
            orig(self);

            On.Localization.LoadLanguages += Localization_LoadLanguages;
            On.Localization.SetupLanguage += Localization_SetupLanguage;

            List<string> tmplist = Localization.instance.m_languages.ToList();
            tmplist.AddRange(Localization.instance.LoadLanguages());
            tmplist = tmplist.Distinct().ToList();
            Localization.instance.m_languages.Clear();
            Localization.instance.m_languages.AddRange(tmplist);

            string lang = PlayerPrefs.GetString("language", DefaultLanguage);
            Localization.instance.SetupLanguage(lang);

            On.Localization.LoadLanguages -= Localization_LoadLanguages;
            On.Localization.SetupLanguage -= Localization_SetupLanguage;
        }

        private List<string> Localization_LoadLanguages(On.Localization.orig_LoadLanguages orig, Localization self)
        {
            var result = orig(self);

            Logger.LogInfo("---- Loading custom localizations ----");

            AddLanguageFilesFromPluginFolder();

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

        private bool Localization_SetupLanguage(On.Localization.orig_SetupLanguage orig, Localization self, string language)
        {
            var result = orig(self, language);
            
            // Only if we have translations for this language
            if (Localizations.ContainsKey(language))
            {
                Logger.LogInfo($"---- Adding tokens for language '{language}' ----");

                AddTokens(self, language);
            }

            return result;
        }

        /// <summary>
        ///     Registers a new Localization for a language.
        /// </summary>
        /// <param name="language">The language added</param>
        /// <param name="localization">The localization for a language</param>
        public void AddLocalization(string language, Dictionary<string, string> localization)
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
        /// <param name="config"></param>
        public void AddLocalization(LocalizationConfig config)
        {
            AddLocalization(config.Language, config.Translations);
        }

        /// <summary>
        ///     Search for and add localization files.
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
        ///     Add a token and its value to the specified language (default to "English").
        /// </summary>
        /// <param name="token">token / key</param>
        /// <param name="value">value that will be printed in the game</param>
        /// <param name="language"></param>
        /// <param name="forceReplace">replace the token if it already exists</param>
        public void AddToken(string token, string value, string language = DefaultLanguage, bool forceReplace = false)
        {
            Dictionary<string, string> languageDict = null;

            if (token.Any(x => LocalizationEndChars.Contains(x)))
            {
                Logger.LogError($"Token '{token}' must not contain an end char ({LocalizationEndChars}).");
                return;
            }

            if (!forceReplace)
            {
                if (Localizations.TryGetValue(language, out languageDict))
                {
                    if (languageDict.Keys.Contains(token))
                    {
                        Logger.LogError($"Token named '{token}' already exist!");
                        return;
                    }
                }
            }

            languageDict ??= GetLanguageDict(language);

            languageDict.Remove(token.TrimStart(TokenFirstChar));
            languageDict.Add(token.TrimStart(TokenFirstChar), value);
        }

        /// <summary>
        ///     Add a token and its value to the "English" language.
        /// </summary>
        /// <param name="token">token / key</param>
        /// <param name="value">value that will be printed in the game</param>
        /// <param name="forceReplace">replace the token if it already exists</param>
        public void AddToken(string token, string value, bool forceReplace = false)
        {
            AddToken(token, value, DefaultLanguage, forceReplace);
        }

        /// <summary>
        ///     Add a file via absolute path.
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

                Logger.LogInfo($"Added json language file: {Path.GetFileName(path)}");
            }
            else
            {
                AddLanguageFile(fileContent);
                Logger.LogInfo($"Added language file: {Path.GetFileName(path)}");
            }
        }

        /// <summary>
        ///     Add a language file that matches Valheim's language format.
        /// </summary>
        /// <param name="fileContent">Entire file as string</param>
        public void AddLanguageFile(string fileContent)
        {
            if (fileContent == null)
            {
                throw new NullReferenceException($"param {nameof(fileContent)} is null");
            }

            LoadLanguageFile(fileContent);
        }

        /// <summary>
        ///     Add a json language file (match crowdin format).
        /// </summary>
        /// <param name="language">Language for the json file, for example, "English"</param>
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
        ///     Tries to translate a word with <see cref="Localization"/>, handles null and tokenized input
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public string TryTranslate(string word)
        {
            var toTranslate = word;

            if (string.IsNullOrEmpty(toTranslate))
            {
                return null;
            }

            if (toTranslate[0] == TokenFirstChar)
            {
                toTranslate = toTranslate.Substring(1);
            }

            return Localization.instance.Translate(toTranslate);
        }

        /// <summary>
        ///     Load Unity style language file.
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
        ///     Get the dictionary for a specific language.
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
        ///     Load community translation file.
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
        ///     Add tokens to the respective localization.
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
