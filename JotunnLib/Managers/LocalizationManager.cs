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

        internal Dictionary<string, List<LocalizationConfig>> Localizations = new Dictionary<string, List<LocalizationConfig>>();

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("Error, two instances of singleton: " + this.GetType().Name);
                return;
            }

            Instance = this;
        }

        /// <summary>
        /// Registers a new translation for a word for the current language
        /// </summary>
        /// <param name="key">Key to translate</param>
        /// <param name="text">Translation</param>
        public void RegisterTranslation(string key, string text)
        {
            ReflectionUtils.InvokePrivate(Localization.instance, "AddWord", new object[] { key, text });
        }

        internal void RegisterTranslation(LocalizationConfig localization)
        {

        }
    }
}
