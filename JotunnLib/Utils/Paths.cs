using System.IO;

namespace JotunnLib.Utils
{
    public static class Paths
    {
        public static string JotunnLibFolder
        {
            get
            {
                var saveDataPath = global::Utils.GetSaveDataPath();
                return Path.Combine(saveDataPath, Main.ModName);
            }
        }

        public static string CustomItemDataFolder => Path.Combine(JotunnLibFolder, "CustomItemData");

        public static string LanguageTranslationsFolder => BepInEx.Paths.PluginPath;
    }
}
