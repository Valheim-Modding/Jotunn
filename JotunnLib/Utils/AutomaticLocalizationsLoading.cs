using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jotunn.Managers;

namespace Jotunn.Utils
{
    internal static class AutomaticLocalizationsLoading
    {
        public const string TranslationsFolderName = "Translations";
        public const string CommunityTranslationFileName = "community_translation.json";

        public static void Init()
        {
            var jsonFormat = new HashSet<FileInfo>();
            var unityFormat = new HashSet<FileInfo>();
            var yamlFormat = new HashSet<FileInfo>();

            // Json format community files
            foreach (var fileInfo in GetTranslationFiles(Paths.LanguageTranslationsFolder, CommunityTranslationFileName))
            {
                jsonFormat.Add(fileInfo);
            }

            foreach (var fileInfo in GetTranslationFiles(Paths.LanguageTranslationsFolder, "*.json"))
            {
                jsonFormat.Add(fileInfo);
            }

            foreach (var fileInfo in GetTranslationFiles(Paths.LanguageTranslationsFolder, "*.language"))
            {
                unityFormat.Add(fileInfo);
            }

            foreach (var fileInfo in GetTranslationFiles(Paths.LanguageTranslationsFolder, "*.yaml"))
            {
                yamlFormat.Add(fileInfo);
            }

            foreach (var fileInfo in GetTranslationFiles(Paths.LanguageTranslationsFolder, "*.yml"))
            {
                yamlFormat.Add(fileInfo);
            }

            foreach (var fileInfo in jsonFormat)
            {
                try
                {
                    var mod = BepInExUtils.GetPluginInfoFromPath(fileInfo)?.Metadata;
                    LocalizationManager.Instance.GetLocalization(mod ?? Main.Instance.Info.Metadata).AddFileByPath(fileInfo.FullName, true);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Exception caught while loading localization file {fileInfo}: {ex}");
                }
            }

            foreach (var fileInfo in unityFormat)
            {
                try
                {
                    var mod = BepInExUtils.GetPluginInfoFromPath(fileInfo)?.Metadata;
                    LocalizationManager.Instance.GetLocalization(mod ?? Main.Instance.Info.Metadata).AddFileByPath(fileInfo.FullName);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Exception caught while loading localization file {fileInfo}: {ex}");
                }
            }

            if (yamlFormat.Count > 0 && !Entities.CustomLocalization.IsYamlDotNetAvailable())
            {
                Logger.LogWarning($"Found {yamlFormat.Count} YAML localization file(s) but YamlDotNet is not loaded. " +
                    "Mods using .yaml/.yml localization must include YamlDotNet.dll as a dependency.");
            }
            else
            {
                foreach (var fileInfo in yamlFormat)
                {
                    try
                    {
                        var mod = BepInExUtils.GetPluginInfoFromPath(fileInfo)?.Metadata;
                        LocalizationManager.Instance.GetLocalization(mod ?? Main.Instance.Info.Metadata).AddFileByPath(fileInfo.FullName);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Exception caught while loading localization file {fileInfo}: {ex}");
                    }
                }
            }
        }

        private static IEnumerable<FileInfo> GetTranslationFiles(string path, string searchPattern)
        {
            return GetTranslationFiles(new DirectoryInfo(path), searchPattern);
        }

        private static IEnumerable<FileInfo> GetTranslationFiles(DirectoryInfo pathDirectoryInfo, string searchPattern)
        {
            if (!pathDirectoryInfo.Exists)
            {
                yield break;
            }

            var files = Directory.GetFiles(pathDirectoryInfo.FullName, searchPattern, SearchOption.AllDirectories);

            foreach (var path in files.Where(path => new DirectoryInfo(path).Parent?.Parent?.Name == TranslationsFolderName))
            {
                yield return new FileInfo(path);
            }
        }
    }
}
