using System;

namespace Jotunn.Utils
{
    /// <summary>
    ///     Utility class for getting game versions
    /// </summary>
    public static class GameVersions
    {
        /// <summary>
        ///     The semantic version of the running Valheim game
        /// </summary>
        public static System.Version ValheimVersion { get; } = GetValheimVersion();

        /// <summary>
        ///     The network version of the running Valheim game, determining compatibility with other clients
        /// </summary>
        public static uint NetworkVersion { get; } = GetNetworkVersion();

        private static System.Version GetValheimVersion()
        {
            return new System.Version(Version.CurrentVersion.m_major, Version.CurrentVersion.m_minor, Version.CurrentVersion.m_patch);
        }

        private static uint GetNetworkVersion()
        {
            // Use reflection because networkVersion is a constant field, i.e. evaluated at compile time
            var field = typeof(Version).GetField("m_networkVersion")
                ?? typeof(Version).GetField("c_networkVersion") // Valheim 0.221.13+
                ?? throw new Exception("Could not find network version field in Version class");
            return (uint)field.GetValue(null);
        }
    }
}
