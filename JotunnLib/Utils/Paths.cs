using System.IO;

namespace Jotunn.Utils
{
    /// <summary>
    ///     Various Path constants used in Jötunn
    /// </summary>
    public static class Paths
    {
        /// <summary>
        ///     Path to the game's save path
        /// </summary>
        public static string JotunnFolder
        {
            get
            {
                var saveDataPath = global::Utils.GetSaveDataPath();
                return Path.Combine(saveDataPath, Main.ModName);
            }
        }

        /// <summary>
        ///     Path to the custom item folder
        /// </summary>
        public static string CustomItemDataFolder => Path.Combine(JotunnFolder, "CustomItemData");

        /// <summary>
        ///     Path to the global translation folder
        /// </summary>
        public static string LanguageTranslationsFolder => BepInEx.Paths.PluginPath;
    }
}
