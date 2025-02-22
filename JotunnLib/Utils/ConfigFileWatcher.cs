using System;
using System.IO;
using BepInEx.Configuration;
using BepInEx;
using Jotunn.Extensions;

namespace Jotunn.Utils
{
    /// <summary>
    ///     Watches a <see cref="ConfigFile"/> for changes and raises events when the configuration file is modified.
    /// </summary>
    public class ConfigFileWatcher
    {
        private const long TICKS_PER_MILISEC = 10000; // One milisecond

        private DateTime lastReadTime = DateTime.MinValue;
        private readonly ConfigFile configFile;
        private readonly string ConfigFileDir;
        private readonly string ConfigFileName;
        private readonly long ReloadDelay;

        /// <summary>
        ///     Create a file watcher to trigger reloads of the config file when it is changed, created, or renamed.
        /// </summary>
        /// <param name="configFile"></param>
        /// <param name="reloadDelay">Time in milliseconds before another event can be fired.</param>
        public ConfigFileWatcher(ConfigFile configFile, long reloadDelay = 1000)
        {
            this.configFile = configFile;
            this.ReloadDelay = reloadDelay * TICKS_PER_MILISEC;
            ConfigFileDir = Directory.GetParent(configFile.ConfigFilePath).FullName;
            ConfigFileName = Path.GetFileName(configFile.ConfigFilePath);
            var watcher = new FileSystemWatcher(ConfigFileDir, ConfigFileName);
            watcher.Changed += ReloadConfigFile;
            watcher.Created += ReloadConfigFile;
            watcher.Renamed += ReloadConfigFile;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        /// <summary>
        ///     Event triggered after the file watcher reloads the configuration file.
        /// </summary>
        public event Action OnConfigFileReloaded;

        /// <summary>
        ///     Safely invoke the <see cref="OnConfigFileReloaded"/> event
        /// </summary>
        private void InvokeOnConfigFileReloaded()
        {
            OnConfigFileReloaded?.SafeInvoke();
        }

        /// <summary>
        ///     Reloads config file if and only if the last write time differs from the last read time.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        internal void ReloadConfigFile(object sender, FileSystemEventArgs eventArgs)
        {
            DateTime now = DateTime.Now;
            long deltaTime = now.Ticks - lastReadTime.Ticks;
            if (!File.Exists(configFile.ConfigFilePath) || deltaTime < this.ReloadDelay)
            {
                return;
            }

            try
            {
                Logger.LogInfo($"Reloading {configFile.ConfigFilePath}");
                bool saveOnConfigSet = configFile.DisableSaveOnConfigSet(); // turn off saving on config entry set
                configFile.Reload();
                configFile.SaveOnConfigSet = saveOnConfigSet; // reset config saving state
                lastReadTime = now;
                InvokeOnConfigFileReloaded(); // fire event
            }
            catch
            {
                Logger.LogError($"There was an issue loading {configFile.ConfigFilePath}");
                Logger.LogError("Please check your config entries for spelling and format!");
            }
        }
    }
}
