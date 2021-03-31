using System.IO;
using UnityEngine;

namespace JotunnLib.Utils
{
    public static class Paths
    {
        public static string ValheimLibFolder
        {
            get
            {
                var saveDataPath = Utils.GetSaveDataPath();
                const string valheimLibFolder = nameof(ValheimLib);

                return Path.Combine(saveDataPath, valheimLibFolder);
            }
        }

        public static string CustomItemDataFolder => Path.Combine(ValheimLibFolder, "CustomItemData");

        public static string LanguageTranslationsFolder => BepInEx.Paths.PluginPath;
    }
}
