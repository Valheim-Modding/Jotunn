using ValheimLokiLoader.Utils;

namespace ValheimLokiLoader.Managers
{
    public static class LocalizationManager
    {
        public static void AddTranslation(string key, string text)
        {
            ReflectionUtils.InvokePrivate(Localization.instance, "AddWord", new object[] { key, text });
        }
    }
}
