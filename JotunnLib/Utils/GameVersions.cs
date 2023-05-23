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
            // Valheim 0.214.305 and below
            var version = ExtractVersion(typeof(Version), null);

            if (version != null)
            {
                return version;
            }

            // Valheim 0.215.1 and above
            var currentVersionProperty = typeof(Version).GetProperty("CurrentVersion", ReflectionHelper.AllBindingFlags);
            var currentVersion = currentVersionProperty.GetValue(null);
            return ExtractVersion(currentVersionProperty.PropertyType, currentVersion);
        }

        private static System.Version ExtractVersion(Type type, object instance)
        {
            var majorField = type.GetField("m_major", ReflectionHelper.AllBindingFlags);
            var minorField = type.GetField("m_minor", ReflectionHelper.AllBindingFlags);
            var patchField = type.GetField("m_patch", ReflectionHelper.AllBindingFlags);

            if (majorField == null || minorField == null || patchField == null)
            {
                return null;
            }

            var major = (int)majorField.GetValue(instance);
            var minor = (int)minorField.GetValue(instance);
            var patch = (int)patchField.GetValue(instance);

            return new System.Version(major, minor, patch);
        }

        private static uint GetNetworkVersion()
        {
            var networkVersionField = typeof(Version).GetField("m_networkVersion", ReflectionHelper.AllBindingFlags);

            if (networkVersionField == null)
            {
                return 0;
            }

            return (uint)networkVersionField.GetValue(null);
        }
    }
}
