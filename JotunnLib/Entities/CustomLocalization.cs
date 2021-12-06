using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using Jotunn.Managers;
using UnityEngine;

namespace Jotunn.Entities
{
    /// <summary> Wrapper to hold each mod localization data. </summary>
    public class CustomLocalization : CustomEntity
    {
        /// <summary> Map that work as [language][token] = translation. </summary>
        internal Dictionary<string, Dictionary<string, string>> Map { get; }

        /// <summary> Default constuctor. </summary>
        public CustomLocalization()
            => Map = new Dictionary<string, Dictionary<string, string>>();

        /// <summary> SourceMod hint constuctor. </summary>
        /// <param name="sourceMod"> Mod data in the shape of BepInPlugin class. </param>
        public CustomLocalization(BepInPlugin sourceMod) : base(sourceMod)
            => Map = new Dictionary<string, Dictionary<string, string>>();

        /// <summary> Retrieve list of languages that have been added. </summary>
        public IEnumerable<string> GetLanguages() => Map.Keys;

        /// <summary> Retrieve translations for given language. </summary>
        /// <param name="language"> Language of the translation you want to retrieve. </param>
        public IReadOnlyDictionary<string, string> GetTranslations(in string language)
            => Map.TryGetValue(language, out var x) ? x : null;
        
        /// <summary>
        ///     Retrieve a translation from this custom localization or <see cref="Localization.Translate"/>.
        ///     Searches with the user language with a fallback to English.
        /// </summary>
        /// <param name="word">Word to translate.</param>
        /// <returns>Translated word in player language or english as a fallback.</returns>
        public string TryTranslate(string word)
        {
            if (string.IsNullOrEmpty(word))
            {
                throw new ArgumentNullException(nameof(word));
            }
            if (word.IndexOfAny(LocalizationManager.ForbiddenCharsArr) != -1)
            {
                Logger.LogWarning($"Token '{word}' must not contain following chars: '{LocalizationManager.ForbiddenChars}'.");
                return null;
            }

            var cleanedWord = word.TrimStart(LocalizationManager.TokenFirstChar);
            var playerLang = PlayerPrefs.GetString("language", LocalizationManager.DefaultLanguage);

            if (Map.TryGetValue(playerLang, out var playerDictionary) && 
                playerDictionary.TryGetValue(cleanedWord, out var playerTranslation))
            {
                return playerTranslation;
            }

            if (!playerLang.Equals(LocalizationManager.DefaultLanguage) &&
                Map.TryGetValue(LocalizationManager.DefaultLanguage, out var dictionary) && 
                dictionary.TryGetValue(cleanedWord, out var translation))
            {
                return translation;
            }

            return Localization.instance.Translate(cleanedWord);
        }

        /// <summary> Checks if a translation exists for given language and token. </summary>
        /// <param name="language"> Language being checked. </param>
        /// <param name="token"> Token being checked. </param>
        /// <returns> True if the token was found. </returns>
        public bool Contains(in string language, in string token)
        {
            var cleanedToken = token.TrimStart(LocalizationManager.TokenFirstChar);
            if (Map.TryGetValue(language, out var translations))
            {
                return translations.ContainsKey(cleanedToken);
            }
            return false;
        }

        #region Add Directly

        /// <summary> Add a translation. </summary>
        /// <param name="token"> Token of the translation you want to add. </param>
        /// <param name="translation"> The translation. </param>
        public void AddTranslation(in string token, string translation)
            => AddTranslation(LocalizationManager.DefaultLanguage, token, translation);

        /// <summary> Add a translation. </summary>
        /// <param name="language"> Language of the translation you want to add. </param>
        /// <param name="token"> Token of the translation you want to add. </param>
        /// <param name="translation"> The translation. </param>
        public void AddTranslation(in string language, in string token, string translation)
        {
            if (!Map.ContainsKey(language))
            {
                Map.Add(language, new Dictionary<string, string>());
            }

            if (!ValidateLanguage(language))
            {
                return;
            }
            if (!ValidateToken(token))
            {
                return;
            }
            if (!ValidateTranslation(translation))
            {
                return;
            }
            var cleanedToken = token.TrimStart(LocalizationManager.TokenFirstChar);

            Map[language][cleanedToken] = translation;
        }

        /// <summary> Add a group of translations. </summary>
        /// <param name="language"> Language of the translation you want to add. </param>
        /// <param name="tokenValue"> Token-Value dictionary. </param>
        public void AddTranslation(in string language, Dictionary<string, string> tokenValue)
        {
            if (!Map.ContainsKey(language))
            {
                Map.Add(language, new Dictionary<string, string>());
            }

            if (!ValidateLanguage(language))
            {
                return;
            }

            foreach (var tv in tokenValue)
            {
                var token = tv.Key.TrimStart(LocalizationManager.TokenFirstChar);
                var translation = tv.Value;

                if (!ValidateToken(token))
                {
                    continue;
                }
                if (!ValidateTranslation(translation))
                {
                    continue;
                }

                Map[language][token] = translation;
            }
        }

        #endregion

        #region Add by File

        /// <summary> Add a translation file via absolute path. </summary>
        /// <param name="path"> Absolute path to file. </param>
        /// <param name="isJson"> Is the language file a json file. </param>
        public void AddFileByPath(string path, bool isJson = false)
        {
            if (path is null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            
            var fileContent = File.ReadAllText(path);

            if (fileContent is null)
            {
                throw new ArgumentNullException(nameof(fileContent));
            }

            if (isJson)
            {
                AddJsonFile(Path.GetFileName(Path.GetDirectoryName(path)), fileContent);
            }
            else
            {
                AddLanguageFile(fileContent);
            }

            Logger.LogDebug($"Added {(isJson ? "Json" : "")} language file: {Path.GetFileName(path)}");
        }

        /// <summary> Add a json language file (match crowdin format). </summary>
        /// <param name="language"> Language for the json file, for example, "English" </param>
        /// <param name="fileContent"> Entire file as string </param>
        public void AddJsonFile(string language, string fileContent)
        {
            if (!ValidateLanguage(language))
            {
                return;
            }

            if (!SimpleJson.SimpleJson.TryDeserializeObject(fileContent, out var json))
            {
                Logger.LogWarning($"Error cought while reading JSON localization: Invalid JSON string");
                return;
            }

            if (!Map.ContainsKey(language))
            {
                Map.Add(language, new Dictionary<string, string>());
            }

            foreach (var tv in json as IDictionary<string, object>)
            {
                var translation = tv.Value as string;
                var token = tv.Key.TrimStart(LocalizationManager.TokenFirstChar);

                if (!ValidateToken(token))
                {
                    continue;
                }
                if (!ValidateTranslation(translation))
                {
                    continue;
                }

                Map[language][token] = translation;
            }
        }

        /// <summary> Add a Unity style translation file. </summary>
        /// <param name="fileContent"> Contents of the language file in string format. </param>
        public void AddLanguageFile(string fileContent)
        {
            var strReader = new StringReader(fileContent);
            var languages = strReader.ReadLine().Split(',');

            foreach (var slicedLine in LocalizationManager.DoQuoteLineSplit(strReader))
            {
                if (slicedLine.Count == 0)
                {
                    continue;
                }

                var token = slicedLine[0];

                if (token.StartsWith("//") || token.Length == 0)
                {
                    continue;
                }
                if (!ValidateToken(token))
                {
                    continue;
                }

                var cleanedToken = token.TrimStart(LocalizationManager.TokenFirstChar);

                for (var i = 1; i < slicedLine.Count; i++)
                {
                    var language = languages[i];
                    var translation = slicedLine[i];

                    if (string.IsNullOrEmpty(translation) || translation[0] == '\r')
                    {
                        translation = slicedLine[1];
                    }
                    if (!ValidateLanguage(language))
                    {
                        continue;
                    }
                    if (!ValidateTranslation(translation))
                    {
                        continue;
                    }
                    if (!Map.ContainsKey(language))
                    {
                        Map.Add(language, new Dictionary<string, string>());
                    }

                    Map[language][cleanedToken] = translation;
                }
            }
        }

        #endregion

        #region Deletion

        /// <summary> Attempts to remove a given token from certain language. </summary>
        /// <param name="language"> Language from which to search the token. </param>
        /// <param name="token"> Token to clear. </param>
        public void ClearToken(in string language, in string token)
        {
            if (Map.ContainsKey(language))
            {
                Map.Remove(token.TrimStart(LocalizationManager.TokenFirstChar));
            }
        }

        /// <summary> Attempts to remove a given token from default language. </summary>
        /// <param name="token"> Token to clear. </param>
        public void ClearToken(in string token)
            => ClearToken(LocalizationManager.DefaultLanguage, token);

        /// <summary> Attempts to remove given language. </summary>
        /// <param name="language"> Language to clear. </param>
        public void ClearLanguage(in string language)
            => Map.Remove(language);

        /// <summary> Clear all localization data. </summary>
        public void ClearAll()
            => Map.Clear();

        #endregion

        #region Validation Methods

        private bool ValidateLanguage(in string language)
        {
            if (string.IsNullOrEmpty(language))
            {
                throw new ArgumentNullException(nameof(language));
            }
            if (!char.IsUpper(language[0]))
            {
                Logger.LogWarning($"Language '{language}' must start with a capital letter");
                return false;
            }
            return true;
        }

        private bool ValidateToken(in string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentNullException(nameof(token));
            }
            if (token.IndexOfAny(LocalizationManager.ForbiddenCharsArr) != -1)
            {
                Logger.LogWarning($"Token '{token}' must not contain following chars: '{LocalizationManager.ForbiddenChars}'.");
                return false;
            }
            return true;
        }

        private bool ValidateTranslation(in string translation)
        {
            if (string.IsNullOrEmpty(translation))
            {
                throw new ArgumentNullException(nameof(translation));
            }
            return true;
        }

        #endregion

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Localization ({SourceMod.GUID})";
        }
    }
}
