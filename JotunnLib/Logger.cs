using BepInEx.Logging;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.Globalization;
using BepInEx;

namespace Jotunn
{
    /// <summary>
    ///     A namespace wide Logger class, which automatically creates a ManualLogSource
    ///     for every Class from which it is being called.
    /// </summary>
    public class Logger
    {
        /// <summary>
        ///     Add DateTime to the log output
        /// </summary>
        public static bool ShowDate = false;

        private static Logger instance = new Logger();

        private readonly Dictionary<string, ManualLogSource> logger = new Dictionary<string, ManualLogSource>();

        /// <summary>
        ///     Remove and clear all Logger instances
        /// </summary>
        internal static void Destroy()
        {
            LogDebug("Destroying Logger");

            foreach (var entry in instance.logger)
            {
                BepInEx.Logging.Logger.Sources.Remove(entry.Value);
            }

            instance.logger.Clear();
        }

        /// <summary>
        ///     Get or create a <see cref="ManualLogSource"/> with the callers <see cref="Type.FullName"/>
        /// </summary>
        /// <returns>A BepInEx <see cref="ManualLogSource"/></returns>
        private ManualLogSource GetLogger()
        {
            var type = new StackFrame(3).GetMethod().DeclaringType;

            ManualLogSource ret;
            if (!logger.TryGetValue(type.FullName, out ret))
            {
                ret = BepInEx.Logging.Logger.CreateLogSource(type.FullName);
                logger.Add(type.FullName, ret);
            }

            return ret;
        }

        private static void Log(LogLevel level, BepInPlugin sourceMod, object data)
        {
            string prefix = string.Empty;

            if (ShowDate)
            {
                prefix += $"[{DateTime.Now.ToString(DateTimeFormatInfo.InvariantInfo)}] ";
            }

            if (sourceMod != null)
            {
                prefix += $"[{sourceMod.Name}] ";
            }

            instance.GetLogger().Log(level, $"{prefix}{data}");
        }

        /// <summary>
        ///     Logs a message with <see cref="BepInEx.Logging.LogLevel.Fatal"/> level.
        /// </summary>
        /// <param name="data">Data to log</param>
        public static void LogFatal(object data) => Log(LogLevel.Fatal, null, data);

        /// <summary>
        ///     Logs a message with <see cref="BepInEx.Logging.LogLevel.Fatal"/> level.
        ///     This is used when the responsible mod is different from mod logging this message.
        /// </summary>
        /// <param name="sourceMod">Known mod that is responsible for this log</param>
        /// <param name="data">Data to log</param>
        public static void LogFatal(BepInPlugin sourceMod, object data) => Log(LogLevel.Fatal, sourceMod, data);

        /// <summary>
        ///     Logs a message with <see cref="BepInEx.Logging.LogLevel.Error"/> level.
        /// </summary>
        /// <param name="data">Data to log</param>
        public static void LogError(object data) => Log(LogLevel.Error, null, data);

        /// <summary>
        ///     Logs a message with <see cref="BepInEx.Logging.LogLevel.Error"/> level.
        ///     This is used when the responsible mod is different from mod logging this message.
        /// </summary>
        /// <param name="sourceMod">Known mod that is responsible for this log</param>
        /// <param name="data">Data to log</param>
        public static void LogError(BepInPlugin sourceMod, object data) => Log(LogLevel.Error, sourceMod, data);

        /// <summary>
        ///     Logs a message with <see cref="BepInEx.Logging.LogLevel.Warning"/> level.
        /// </summary>
        /// <param name="data">Data to log</param>
        public static void LogWarning(object data) => Log(LogLevel.Warning, null, data);

        /// <summary>
        ///     Logs a message with <see cref="BepInEx.Logging.LogLevel.Warning"/> level.
        ///     This is used when the responsible mod is different from mod logging this message.
        /// </summary>
        /// <param name="sourceMod">Known mod that is responsible for this log</param>
        /// <param name="data">Data to log</param>
        public static void LogWarning(BepInPlugin sourceMod, object data) => Log(LogLevel.Warning, sourceMod, data);

        /// <summary>
        ///     Logs a message with <see cref="BepInEx.Logging.LogLevel.Message"/> level.
        /// </summary>
        /// <param name="data">Data to log</param>
        public static void LogMessage(object data) => Log(LogLevel.Message, null, data);

        /// <summary>
        ///     Logs a message with <see cref="BepInEx.Logging.LogLevel.Message"/> level.
        ///     This is used when the responsible mod is different from mod logging this message.
        /// </summary>
        /// <param name="sourceMod">Known mod that is responsible for this log</param>
        /// <param name="data">Data to log</param>
        public static void LogMessage(BepInPlugin sourceMod, object data) => Log(LogLevel.Message, sourceMod, data);

        /// <summary>
        ///     Logs a message with <see cref="BepInEx.Logging.LogLevel.Info"/> level.
        /// </summary>
        /// <param name="data">Data to log</param>
        public static void LogInfo(object data) => Log(LogLevel.Info, null, data);

        /// <summary>
        ///     Logs a message with <see cref="BepInEx.Logging.LogLevel.Info"/> level.
        ///     This is used when the responsible mod is different from mod logging this message.
        /// </summary>
        /// <param name="sourceMod">Known mod that is responsible for this log</param>
        /// <param name="data">Data to log</param>
        public static void LogInfo(BepInPlugin sourceMod, object data) => Log(LogLevel.Info, sourceMod, data);

        /// <summary>
        ///     Logs a message with <see cref="BepInEx.Logging.LogLevel.Debug"/> level.
        /// </summary>
        /// <param name="data">Data to log</param>
        public static void LogDebug(object data) => Log(LogLevel.Debug, null, data);

        /// <summary>
        ///     Logs a message with <see cref="BepInEx.Logging.LogLevel.Debug"/> level.
        ///     This is used when the responsible mod is different from mod logging this message.
        /// </summary>
        /// <param name="sourceMod">Known mod that is responsible for this log</param>
        /// <param name="data">Data to log</param>
        public static void LogDebug(BepInPlugin sourceMod, object data) => Log(LogLevel.Debug, sourceMod, data);
    }
}
