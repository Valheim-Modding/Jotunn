using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
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
        private readonly ConfigFile configFile;
        private readonly BepInPlugin sourceMod;
        private readonly string configFileDir;
        private readonly string configFileName;
        private readonly System.Timers.Timer reloadTimer;

        // Must be kept referenced: the running watcher only holds a weak reference to it,
        // so an unreferenced FileSystemWatcher is garbage collected and silently stops raising events.
        private readonly FileSystemWatcher watcher;

        /// <summary>
        ///     Create a file watcher to trigger reloads of the config file when it is changed, created, or renamed.
        /// </summary>
        /// <param name="configFile"></param>
        /// <param name="reloadDelay">
        ///     Time in milliseconds without further changes before the file is reloaded.
        ///     Every change restarts the delay, so a file written in several steps is only reloaded once it is complete.
        /// </param>
        public ConfigFileWatcher(ConfigFile configFile, long reloadDelay = 1000)
        {
            sourceMod = BepInExUtils.GetPluginInfoFromAssembly(Assembly.GetCallingAssembly())?.Metadata;
            if (sourceMod == null || sourceMod.GUID == Main.Instance.Info.Metadata.GUID)
            {
                sourceMod = BepInExUtils.GetSourceModMetadata();
            }

            this.configFile = configFile;
            reloadTimer = new System.Timers.Timer(Math.Max(1, reloadDelay));
            reloadTimer.AutoReset = false;
            reloadTimer.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            reloadTimer.Elapsed += (sender, eventArgs) => ReloadConfigFile();

            // Watch the real file: on Linux the FileSystemWatcher does not follow symlinks,
            // neither a symlinked config folder nor a symlinked config file
            string configFilePath = ResolveSymlinks(configFile.ConfigFilePath);
            configFileDir = Directory.GetParent(configFilePath).FullName;
            configFileName = Path.GetFileName(configFilePath);

            watcher = new FileSystemWatcher(configFileDir, configFileName);
            watcher.Changed += ScheduleReload;
            watcher.Created += ScheduleReload;
            watcher.Renamed += ScheduleReload;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        [DllImport("libc", EntryPoint = "realpath", SetLastError = true)]
        private static extern IntPtr RealPath(string path, IntPtr resolvedPath);

        [DllImport("libc", EntryPoint = "free")]
        private static extern void Free(IntPtr pointer);

        /// <summary>
        ///     Resolves all symlinks of an existing path on Unix systems (Linux, macOS).
        ///     Returns the path unchanged on Windows or if it can not be resolved.
        /// </summary>
        private static string ResolveSymlinks(string path)
        {
            if (Environment.OSVersion.Platform != PlatformID.Unix && Environment.OSVersion.Platform != PlatformID.MacOSX)
            {
                return path;
            }

            try
            {
                IntPtr resolved = RealPath(path, IntPtr.Zero);

                if (resolved == IntPtr.Zero)
                {
                    return path;
                }

                try
                {
                    return Marshal.PtrToStringAnsi(resolved);
                }
                finally
                {
                    Free(resolved);
                }
            }
            catch (Exception)
            {
                // libc not available
                return path;
            }
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
        ///     (Re)starts the reload delay. Reloading only after the file stayed unchanged for the whole delay
        ///     avoids loading a half written file and missing the final state of a file written in several steps.
        /// </summary>
        private void ScheduleReload(object sender, FileSystemEventArgs eventArgs)
        {
            reloadTimer.Stop();
            reloadTimer.Start();
        }

        /// <summary>
        ///     Reloads the config file, if it exists.
        /// </summary>
        internal void ReloadConfigFile()
        {
            if (!File.Exists(configFile.ConfigFilePath))
            {
                return;
            }

            // Only log file name to avoid exposing user info if it located within AppData (such as when using r2modman)
            try
            {
                Logger.LogInfo(sourceMod, $"Reloading {configFileName}");
                bool saveOnConfigSet = configFile.SetSaveOnConfigSet(false); // turn off saving on config entry set
                configFile.Reload();
                configFile.SaveOnConfigSet = saveOnConfigSet; // reset config saving state
                InvokeOnConfigFileReloaded(); // fire event
            }
            catch
            {
                Logger.LogError(sourceMod, $"There was an issue loading {configFileName}");
                Logger.LogError(sourceMod, "Please check your config entries for spelling and format!");
            }
        }
    }
}
