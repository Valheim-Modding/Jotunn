using BepInEx;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.IO;
using JotunnLib.Utils;

namespace JotunnLib.Utils
{
    /// <summary>
    /// Class for adding / replacing localization tokens
    /// </summary>
    public static class Language
    {
        /// <summary>
        /// Your token must start with this character.
        /// </summary>
        public const char TokenFirstChar = '$';

        /// <summary>
        /// Default language of the game
        /// </summary>
        public const string DefaultLanguage = "English";

        /// <summary>
        /// Name of the folder that will hold the custom .json translations files
        /// </summary>
        public const string TranslationsFolderName = "Translations";

        /// <summary>
        /// Name of the community translation files that will be the first custom languages files loaded before any others.
        /// </summary>
        public const string CommunityTranslationFileName = "community_translation.json";

        internal static Dictionary<string, Dictionary<string, string>> AdditionalTokens =
            new Dictionary<string, Dictionary<string, string>>();

        internal static Func<StringReader, List<List<string>>> DoQuoteLineSplit;

        internal static void Init()
        {
            _ = new Hook(
                typeof(Localization).GetMethod(nameof(Localization.SetupLanguage), ReflectionHelper.AllBindingFlags),
                typeof(Language).GetMethod(nameof(AddTokens), ReflectionHelper.AllBindingFlags));

            var doQuoteLineSplitMethodInfo = typeof(Localization).GetMethod(nameof(Localization.DoQuoteLineSplit), ReflectionHelper.AllBindingFlags);
            DoQuoteLineSplit = (Func<StringReader, List<List<string>>>)
                Delegate.CreateDelegate(typeof(Func<StringReader, List<List<string>>>), null, doQuoteLineSplitMethodInfo);

            AddLanguageFilesFromPluginFolder();
        }

        private static Dictionary<string, string> GetLanguageDict(string language)
        {
            if (!AdditionalTokens.TryGetValue(language, out var languageDict))
            {
                languageDict = new Dictionary<string, string>();
                AdditionalTokens.Add(language, languageDict);
            }

            return languageDict;
        }

        private static void AddLanguageFilesFromPluginFolder()
        {
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
        ///  Add a token and its value to the specified language (default to English)
        /// </summary>
        /// <param name="token">token / key</param>
        /// <param name="value">value that will be printed in the game</param>
        /// <param name="language"></param>
        /// <param name="forceReplace">replace the token if it already exists</param>
        public static void AddToken(string token, string value, string language = DefaultLanguage, bool forceReplace = false)
        {
            if (token[0] != TokenFirstChar)
            {
                throw new Exception($"Token first char should be {TokenFirstChar} ! (token : {token})");
            }

            Dictionary<string, string> languageDict = null;

            if (!forceReplace)
            {
                if (AdditionalTokens.TryGetValue(language, out languageDict))
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
        /// Add a token and its value to the English language
        /// </summary>
        /// <param name="token">token / key</param>
        /// <param name="value">value that will be printed in the game</param>
        /// <param name="forceReplace">replace the token if it already exists</param>
        public static void AddToken(string token, string value, bool forceReplace = false) =>
            AddToken(token, value, DefaultLanguage, forceReplace);

        /// <summary>
        /// Add a file via absolute path
        /// </summary>
        /// <param name="path">Absolute path to file</param>
        /// <param name="isJson">Is the language file a json file</param>
        public static void AddPath(string path, bool isJson = false)
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

                JotunnLib.Logger.LogInfo($"Added json language file {Path.GetFileName(path)}");
            }
            else
            {
                Add(fileContent);

                JotunnLib.Logger.LogInfo($"Added language file {Path.GetFileName(path)}");
            }

        }

        /// <summary>
        /// Add a language file (that match the game format)
        /// </summary>
        /// <param name="fileContent">Entire file as string</param>
        public static void Add(string fileContent)
        {
            if (fileContent == null)
            {
                throw new NullReferenceException($"param {nameof(fileContent)} is null");
            }

            LoadLanguageFile(fileContent);
        }

        /// <summary>
        /// Add a json language file (match crowdin format)
        /// </summary>
        /// <param name="language">Language for the json file, example : "English"</param>
        /// <param name="fileContent">Entire file as string</param>
        public static void AddJson(string language, string fileContent)
        {
            if (fileContent == null)
            {
                throw new NullReferenceException($"param {nameof(fileContent)} is null");
            }

            LoadJsonLanguageFile(language, fileContent);
        }

        private static void LoadLanguageFile(string fileContent)
        {
            var stringReader = new StringReader(fileContent);
            var languages = stringReader.ReadLine().Split(new[] { ',' });

            foreach (List<string> keyAndValues in DoQuoteLineSplit(stringReader))
            {
                if (keyAndValues.Count != 0)
                {
                    var token = keyAndValues[0];
                    if (!token.StartsWith("//") && token.Length != 0)
                    {
                        for (var i = 0; i < languages.Length; i++)
                        {
                            var language = languages[i];

                            string tokenValue = keyAndValues[i];
                            if (string.IsNullOrEmpty(tokenValue) || tokenValue[0] == '\r')
                            {
                                tokenValue = keyAndValues[1];
                            }

                            var languageDict = GetLanguageDict(language);
                            languageDict.Remove(token);
                            languageDict.Add(token, tokenValue);
                        }
                    }
                }
            }
        }

        private static void LoadJsonLanguageFile(string language, string fileContent)
        {
            var languageDict = GetLanguageDict(language);

            var json = (IDictionary<string, object>)SimpleJson.SimpleJson.DeserializeObject(fileContent);

            foreach (var pair in json)
            {
                var token = pair.Key;
                var tokenValue = pair.Value;

                languageDict.Remove(token);
                languageDict.Add(token, (string)tokenValue);
            }
        }

        private static bool AddTokens(Func<Localization, string, bool> orig, Localization self, string language)
        {
            var res = orig(self, language);

            if (res)
            {
                if (AdditionalTokens.TryGetValue(language, out var tokens))
                {
                    foreach (var pair in tokens)
                    {
                        self.AddWord(pair.Key, pair.Value);
                    }
                }
            }

            return res;
        }
    }
}
