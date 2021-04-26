using BepInEx.Logging;
using System.Collections.Generic;
using System.Diagnostics;

namespace Jotunn
{
    /// <summary>
    ///     A namespace wide Logger class, which automatically creates a ManualLogSource
    ///     for every Class from which it is being called.
    /// </summary>
    public class Logger
    {
        private static Logger instance;
        
        private readonly Dictionary<string, ManualLogSource> logger = new Dictionary<string, ManualLogSource>();

        private Logger() { }

        internal static void Init()
        {
            if (instance == null)
            {
                instance = new Logger();
            }
        }

        internal static void Destroy()
        {
            LogDebug("Destroying Logger");

            foreach (var entry in instance.logger)
            {
                BepInEx.Logging.Logger.Sources.Remove(entry.Value);
            }

            instance.logger.Clear();
        }

        private ManualLogSource GetLogger()
        {
            var type = new StackFrame(2).GetMethod().DeclaringType;

            ManualLogSource ret;
            if (!logger.TryGetValue(type.FullName, out ret))
            {
                //ret = BepInEx.Logging.Logger.CreateLogSource(type.Namespace);
                ret = BepInEx.Logging.Logger.CreateLogSource(type.FullName);
                logger.Add(type.FullName, ret);
            }

            return ret;
        }

        public static void LogFatal(object data)
        {
            instance.GetLogger().LogFatal(data);
        }

        public static void LogError(object data)
        {
            instance.GetLogger().LogError(data);
        }

        public static void LogWarning(object data)
        {
            instance.GetLogger().LogWarning(data);
        }

        public static void LogMessage(object data)
        {
            instance.GetLogger().LogMessage(data);
        }

        public static void LogInfo(object data)
        {
            instance.GetLogger().LogInfo(data);
        }

        public static void LogDebug(object data)
        {
            instance.GetLogger().LogDebug(data);
        }
    }
}
