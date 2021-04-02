using BepInEx.Logging;
using System.Collections.Generic;
using System.Diagnostics;

namespace JotunnLib
{
    /// <summary>
    ///     A namespace wide Logger class, which automatically creates a <see cref="ManualLogSource" />
    ///     for every namespace from which it is being called
    /// </summary>
    public class Logger
    {
        private static Logger Instance;

        private readonly Dictionary<string, ManualLogSource> m_logger = new Dictionary<string, ManualLogSource>();

        private Logger() { }

        internal static void Init()
        {
            if (Instance == null)
            {
                Instance = new Logger();
            }
        }

        internal static void Destroy()
        {
            LogDebug("Destroying Logger");

            foreach (var entry in Instance.m_logger)
            {
                BepInEx.Logging.Logger.Sources.Remove(entry.Value);
            }

            Instance.m_logger.Clear();
        }

        private ManualLogSource GetLogger()
        {
            var type = new StackFrame(2).GetMethod().DeclaringType;

            ManualLogSource ret;
            if (!m_logger.TryGetValue(type.Namespace, out ret))
            {
                ret = BepInEx.Logging.Logger.CreateLogSource(type.Namespace);
                m_logger.Add(type.Namespace, ret);
            }

            return ret;
        }

        public static void LogFatal(object data)
        {
            Instance.GetLogger().LogFatal(data);
        }

        public static void LogError(object data)
        {
            Instance.GetLogger().LogError(data);
        }

        public static void LogWarning(object data)
        {
            Instance.GetLogger().LogWarning(data);
        }

        public static void LogMessage(object data)
        {
            Instance.GetLogger().LogMessage(data);
        }

        public static void LogInfo(object data)
        {
            Instance.GetLogger().LogInfo(data);
        }

        public static void LogDebug(object data)
        {
            Instance.GetLogger().LogDebug(data);
        }
    }
}
