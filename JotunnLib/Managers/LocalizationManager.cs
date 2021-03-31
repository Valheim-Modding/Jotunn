using System;
using System.Collections.Generic;
using UnityEngine;
using JotunnLib.Entities;
using JotunnLib.Utils;

namespace JotunnLib.Managers
{
    /// <summary>
    /// Handles all logic to do with managing the game's localizations.
    /// </summary>
    public class LocalizationManager : Manager
    {
        public static LocalizationManager Instance { get; private set; }
        public event EventHandler LocalizationRegister;

        internal Dictionary<string, List<LocalizationConfig>> Localizations = new Dictionary<string, List<LocalizationConfig>>();
        internal List<string> Languages = new List<string>();
        private bool registered = false;

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("Error, two instances of singleton: " + this.GetType().Name);
                return;
            }

            Instance = this;
        }

        internal override void Register()
        {
            if (registered)
            {
                return;
            }

            Localizations.Clear();
            Languages.Clear();

            Debug.Log("---- Registering custom localizations ----");
            LocalizationRegister?.Invoke(null, EventArgs.Empty);
            registered = true;
        }

        internal void Load(Localization instance, string language)
        {
            if (!Localizations.ContainsKey(language))
            {
                return;
            }

            foreach (LocalizationConfig localization in Localizations[language])
            {
                foreach (var pair in localization.Translations)
                {
                    Debug.Log("\tAdded translation: " + pair.Key + " -> " + pair.Value);
                    ReflectionHelper.InvokePrivate(instance, "AddWord", new object[] { pair.Key, pair.Value });
                }
            }
        }

        /// <summary>
        /// Registers a new translation for a word for the current language
        /// </summary>
        /// <param name="key">Key to translate</param>
        /// <param name="text">Translation</param>
        public void RegisterTranslation(string key, string text)
        {
            ReflectionHelper.InvokePrivate(Localization.instance, "AddWord", new object[] { key, text });
        }

        /// <summary>
        /// Registers a new Localization for a language
        /// </summary>
        /// <param name="localization">The localization config for a language</param>
        public void RegisterLocalization(LocalizationConfig localization)
        {
            if (string.IsNullOrEmpty(localization.Language))
            {
                Debug.LogError("Error, localization had null or empty language");
                return;
            }

            if (!Localizations.ContainsKey(localization.Language))
            {
                Localizations.Add(localization.Language, new List<LocalizationConfig>());
            }

            if (!Languages.Contains(localization.Language))
            {
                Languages.Add(localization.Language);
            }

            Localizations[localization.Language].Add(localization);
        }
    }
}
