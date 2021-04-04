using System.IO;
using UnityEngine;

namespace JotunnLib.Utils
{
    public static class Paths
    {
        public static string JotunnLibFolder
        {
            get
            {
                var saveDataPath = global::Utils.GetSaveDataPath();
                const string jotunnLibFolder = nameof(Main);

                return Path.Combine(saveDataPath, jotunnLibFolder);
            }
        }

        public static string CustomItemDataFolder => Path.Combine(JotunnLibFolder, "CustomItemData");

        public static string LanguageTranslationsFolder => BepInEx.Paths.PluginPath;
    }
}
