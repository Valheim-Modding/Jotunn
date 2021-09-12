using System;
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
        ///     Wrapper for ease of access to the localization data.
        /// </summary>
        internal class LocalizationData
        {
            /// <summary> 
            ///     List where all data is collected.
            /// </summary>
            private readonly List<CustomLocalization> Data = new List<CustomLocalization>();

            /// <summary> 
            ///     Readonly accessor for collected data.
            /// </summary>
            public IReadOnlyList<CustomLocalization> GetRaw() => Data as IReadOnlyList<CustomLocalization>;

            /// <summary> 
            ///     Get the CustomLocalization for this mod or creates one if it doesn't exist.
            /// </summary>
            /// <param name="sourceMod"> Mod data in the shape of BepInPlugin class. </param>
            /// <returns> Existing or newly created CustomLocalization. </returns>
            public CustomLocalization Get(BepInPlugin sourceMod = null)
            {
                var plugin = sourceMod ?? BepInExUtils.GetSourceModMetadata();
                var ct = Data.FirstOrDefault(ctx => ctx.SourceMod == plugin);
                if (ct != null)
                {
                    return ct;
                }

                ct = sourceMod is null ? new CustomLocalization() : new CustomLocalization(sourceMod);
                Data.Add(ct);
                return ct;
            }

            /// <summary> 
            ///     Search every plugin localization data to find and retrieve a match.
            /// </summary>
            /// <param name="language"> Language of the translation you want to retrieve. </param>
            /// <param name="token"> Token of the translation you want to retrieve. </param>
            /// <param name="translation"> String with the result of the search or null if unsuccessful. </param>
            /// <returns> True if found the translation, false if not. </returns>
            public bool TryTranslate(in string language, in string token, out string translation)
            {
                translation = null;
                foreach (var ct in Data)
                {
                    if (ct.TryTranslate(language, token, out translation))
                    {
                        break;
                    }
                }
                return translation != null;
            }

            /// <summary> 
            ///     Search every plugin localization data to find a match.
            /// </summary>
            /// <param name="language"> Language of the translation you want to retrieve. </param>
            /// <param name="token"> Token of the translation you want to retrieve. </param>
            /// <returns> The translation. </returns>
            public bool Contains(in string language, in string token)
            {
                foreach (var ct in Data)
                {
                    if (ct.Contains(language, token))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

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
        internal readonly char[] ForbiddenCharsArr = ForbiddenChars.ToCharArray();

        /// <summary> 
        ///     The singleton instance of this manager.
        /// </summary>
        public static LocalizationManager Instance => _instance ??= new LocalizationManager();
        private static LocalizationManager _instance;

        /// <summary>
        ///     Event that gets fired after all custom localization has been added to the game.
        ///     Use this event if you need to translate strings using the vanilla <see cref="Localization"/> class.
        ///     Your code will execute every time the localization gets reset (on every menu start).
        ///     If you want to execute just once you will need to unregister from the event after execution.
        ///
        /// </summary>
        public static event Action OnLocalizationAdded;

        /// <summary> 
        ///     Call into unity's DoQuoteLineSplit.
        /// </summary>
        internal static Func<StringReader, List<List<string>>> DoQuoteLineSplit;

        /// <summary> 
        ///     Object that holds all localization data added by Jotunn plugins.
        /// </summary>
        internal LocalizationData Data = new LocalizationData();

        /// <summary> 
        ///     Initialize localization manager.
        /// </summary>
        public void Init()
        {
            On.FejdStartup.SetupGui += LoadAndSetupModLanguages;

            var doQuoteLineSplitMethodInfo = typeof(Localization).GetMethod(nameof(Localization.DoQuoteLineSplit), ReflectionHelper.AllBindingFlags);
            DoQuoteLineSplit =
                (Func<StringReader, List<List<string>>>)Delegate.CreateDelegate(typeof(Func<StringReader, List<List<string>>>), null,
                    doQuoteLineSplitMethodInfo);
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

            AddLanguageFilesFromPluginFolder();

            // Add in localized languages that do not yet exist
            foreach (var ct in Data.GetRaw())
            {
                foreach (var language in ct.Getlanguages())
                {
                    if (!result.Contains(language))
                    {
                        result.Add(language);
                    }
                }
            }

            return result;
        }

        private bool Localization_SetupLanguage(On.Localization.orig_SetupLanguage orig, Localization self, string language)
        {
            var result = orig(self, language);
            var data = Data.GetRaw();

            foreach (var ct in data)
            {
                var langDic = ct.GetTranslations(language);
                if (langDic == null)
                {
                    continue;
                }

                Logger.LogInfo($"Adding tokens for language '{language}'");

                foreach (var pair in langDic)
                {
                    Logger.LogDebug("Added translation: " + pair.Key + " -> " + pair.Value);
                    self.AddWord(pair.Key, pair.Value);
                }
            }

            return result;
        }

        /// <summary> 
        ///     Tries to translate a word with loaded plugin translations or <see cref="Localization.Translate"/>.
        /// </summary>
        /// <param name="word"> Word to translate. </param>
        /// <returns> Translated word in player language or english as a fallback. </returns>
        public string TryTranslate(string word)
        {
            if (!ValidateToken(word))
            {
                return null;
            }

            var trim = word.TrimStart(TokenFirstChar);
            var playerLang = PlayerPrefs.GetString("language", DefaultLanguage);

            if (Data.TryTranslate(playerLang, trim, out var translation))
            {
                return translation;
            }

            if (Data.TryTranslate(DefaultLanguage, trim, out translation))
            {
                return translation;
            }

            return Localization.instance.Translate(trim);
        }

        #region Add Directly

        /// <summary> 
        ///     Registers a new Localization for a language.
        /// </summary>
        /// <param name="config"> Wrapper which contains a language and a Token-Value dictionary. </param>
        public void AddLocalization(LocalizationConfig config)
            => AddLocalization(config.Language, config.Translations);

        /// <summary> 
        ///     Registers a new Localization for a language.
        /// </summary>
        /// <param name="language"> The language being added. </param>
        /// <param name="localization"> Token-Value dictionary. </param>
        public void AddLocalization(string language, Dictionary<string, string> localization)
        {
            if (localization is null || localization.Count < 1)
            {
                throw new ArgumentNullException(nameof(language));
            }
            if (!ValidateLanguage(language))
            {
                return;
            }

            Data.Get().AddTranslation(language, localization);
        }

        /// <summary> 
        ///     Add a token and its value to the "English" language.
        /// </summary>
        /// <param name="token">token / key</param>
        /// <param name="value">value that will be printed in the game</param>
        /// <param name="forceReplace">replace the token if it already exists</param>
        public void AddToken(string token, string value, bool forceReplace = false)
            => AddToken(token, value, DefaultLanguage, forceReplace);

        /// <summary> 
        ///     Add a token and its value to the specified language (default to "English").
        /// </summary>
        /// <param name="token"> Token </param>
        /// <param name="value"> Translation. </param>
        /// <param name="language"> Language ID for this token. </param>
        /// <param name="forceReplace"> Replace the token if it already exists </param>
        public void AddToken(string token, string value, string language, bool forceReplace = false)
        {
            if (!ValidateLanguage(language))
            {
                return;
            }
            if (!ValidateToken(token))
            {
                return;
            }
            if (!ValidateTranslation(value))
            {
                return;
            }

            var trim = token.TrimStart(TokenFirstChar);
            var ct = Data.Get();

            if (!forceReplace && ct.Contains(language, token))
            {
                Logger.LogWarning($"Token named '{trim}' already exist!");
                return;
            }

            ct.AddTranslation(language, trim, value);
        }

        #endregion

        #region Add by File

        /// <summary> 
        ///     Search and add localization files.
        /// </summary>
        private void AddLanguageFilesFromPluginFolder() // TODO: change to get sourceMod info
        {
            static string GetDirName(in string str) => Path.GetDirectoryName(str);
            var jsonFormat = new HashSet<string>();
            var unityFormat = new HashSet<string>();

            // Json format community files
            var paths = Directory.GetFiles(Paths.LanguageTranslationsFolder, CommunityTranslationFileName, SearchOption.AllDirectories);
            foreach (var path in paths)
            {
                if (GetDirName(GetDirName(path)).EndsWith(TranslationsFolderName))
                {
                    jsonFormat.Add(path);
                }
            }

            // Json format files
            paths = Directory.GetFiles(Paths.LanguageTranslationsFolder, "*.json", SearchOption.AllDirectories);
            foreach (var path in paths)
            {
                if (GetDirName(GetDirName(path)).EndsWith(TranslationsFolderName))
                {
                    jsonFormat.Add(path);
                }
            }

            // Unity format files
            paths = Directory.GetFiles(Paths.LanguageTranslationsFolder, "*.language", SearchOption.AllDirectories);
            foreach (var path in paths)
            {
                unityFormat.Add(path);
            }

            foreach (var path in jsonFormat)
            {
                AddPath(path, true);
            }
            foreach (var path in unityFormat)
            {
                AddPath(path);
            }
        }

        /// <summary> 
        ///     Add a file via absolute path.
        /// </summary>
        /// <param name="path"> Absolute path to file. </param>
        /// <param name="isJson"> Is the language file a json file. </param>
        public void AddPath(string path, bool isJson = false)
        {
            if (path is null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            var fileContent = File.ReadAllText(path);

            if (isJson)
            {
                AddJson(Path.GetFileName(Path.GetDirectoryName(path)), fileContent);
            }
            else
            {
                AddLanguageFile(fileContent);
            }

            Logger.LogDebug($"Added {(isJson ? "Json" : "")} language file: {Path.GetFileName(path)}");
        }
        
        /// <summary> 
        ///     Add a json language file (match crowdin format).
        /// </summary>
        /// <param name="language"> Language for the json file, for example, "English" </param>
        /// <param name="fileContent"> Entire file as string </param>
        public void AddJson(string language, string fileContent)
        {
            if (language is null)
            {
                throw new ArgumentNullException(nameof(language));
            }
            if (fileContent is null)
            {
                throw new ArgumentNullException(nameof(fileContent));
            }

            LoadJsonLanguageFile(language, fileContent);
        }

        /// <summary> 
        ///     Add a language file that matches Valheim's language format.
        /// </summary>
        /// <param name="fileContent">Entire file as string</param>
        public void AddLanguageFile(string fileContent)
        {
            if (fileContent is null)
            {
                throw new ArgumentNullException(nameof(fileContent));
            }

            LoadLanguageFile(fileContent);
        }

        /// <summary> 
        ///     Load Unity style translation file.
        /// </summary>
        /// <param name="fileContent"> Contents of the language file in string format. </param>
        /// <param name="sourceMod"> Mod data in the shape of BepInPlugin class. </param>
        private void LoadLanguageFile(string fileContent, BepInPlugin sourceMod = null)
        {
            var translations = Data.Get(sourceMod ?? BepInExUtils.GetSourceModMetadata());
            var strReader = new StringReader(fileContent);
            var languages = strReader.ReadLine().Split(',');

            foreach (var slicedLine in DoQuoteLineSplit(strReader))
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

                    translations.AddTranslation(language, token, translation);
                }
            }
        }

        /// <summary> 
        ///     Load Json style translation file.
        /// </summary>
        /// <param name="language"> Language of the translation file. </param>
        /// <param name="fileContent"> Contents of the language file in string format. </param>
        /// <param name="sourceMod"> Mod data in the shape of BepInPlugin class. </param>
        private void LoadJsonLanguageFile(string language, string fileContent, BepInPlugin sourceMod = null)
        {
            if (!ValidateLanguage(language))
            {
                return;
            }

            var translations = Data.Get(sourceMod ?? BepInExUtils.GetSourceModMetadata());
            var json = (IDictionary<string, object>)SimpleJson.SimpleJson.DeserializeObject(fileContent);

            translations.AddTranslation(language, json);
        }

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
            if (token.IndexOfAny(ForbiddenCharsArr) != -1)
            {
                Logger.LogWarning($"Token '{token}' must not contain following chars: '{ForbiddenChars}'.");
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
    }
}
