using BepInEx.Logging;
using System.Collections.Generic;
using System.Diagnostics;

namespace JotunnLib
{
    /// <summary>
    ///     A namespace wide Logger class, which automatically creates a <see cref="ManualLogSource" />
    ///     for every namespace from which it is being called
    /// </summary>
    internal class Logger
    {
        public static Logger Instance;

        private readonly Dictionary<string, ManualLogSource> m_logger = new Dictionary<string, ManualLogSource>();

        private Logger() { }

        public static void Init()
        {
            if (Instance == null)
            {
                Instance = new Logger();
            }
        }

        public static void Destroy()
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

        internal static void LogFatal(object data)
        {
            Instance.GetLogger().LogFatal(data);
        }

        internal static void LogError(object data)
        {
            Instance.GetLogger().LogError(data);
        }

        internal static void LogWarning(object data)
        {
            Instance.GetLogger().LogWarning(data);
        }

        internal static void LogMessage(object data)
        {
            Instance.GetLogger().LogMessage(data);
        }

        internal static void LogInfo(object data)
        {
            Instance.GetLogger().LogInfo(data);
        }

        internal static void LogDebug(object data)
        {
            Instance.GetLogger().LogDebug(data);
        }
    }
}
