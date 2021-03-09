using UnityEngine;
using JotunnLib.Utils;

namespace JotunnLib.Managers
{
    /// <summary>
    /// Handles all logic to do with managing the game's localizations
    /// </summary>
    public class LocalizationManager : Manager
    {
        public static LocalizationManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("Error, two instances of singleton: " + this.GetType().Name);
                return;
            }

            Instance = this;
        }

        public void RegisterTranslation(string key, string text)
        {
            ReflectionUtils.InvokePrivate(Localization.instance, "AddWord", new object[] { key, text });
        }
    }
}
