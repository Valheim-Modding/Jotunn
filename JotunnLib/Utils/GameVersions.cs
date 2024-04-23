using System;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;

namespace Jotunn.Utils
{
    internal static class GameVersions
    {
        public static System.Version ValheimVersion { get; } = GetValheimVersion();

        public static uint NetworkVersion { get; } = GetNetworkVersion();

        private static System.Version GetValheimVersion()
        {
            return new System.Version(Version.CurrentVersion.m_major, Version.CurrentVersion.m_minor, Version.CurrentVersion.m_patch);
        }

        private static uint GetNetworkVersion()
        {
            return Version.m_networkVersion;
        }
    }
}
