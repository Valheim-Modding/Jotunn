using System;
using System.Collections.Generic;
using System.Text;

namespace Jotunn.Utils
{
    /// <summary>
    ///     Deserialize version string into a usable format.
    /// </summary>
    internal class ModuleVersionData
    {
        /// <summary>
        ///     Create from module data
        /// </summary>
        /// <param name="versionData"></param>
        internal ModuleVersionData(List<Tuple<string, System.Version, CompatibilityLevel, VersionStrictness>> versionData)
        {
            ValheimVersion = new System.Version(Version.m_major, Version.m_minor, Version.m_patch);
            Modules = new List<Tuple<string, System.Version, CompatibilityLevel, VersionStrictness>>();
            Modules.AddRange(versionData);
        }

        internal ModuleVersionData(System.Version valheimVersion, List<Tuple<string, System.Version, CompatibilityLevel, VersionStrictness>> versionData)
        {
            ValheimVersion = valheimVersion;
            Modules = new List<Tuple<string, System.Version, CompatibilityLevel, VersionStrictness>>();
            Modules.AddRange(versionData);
        }

        /// <summary>
        ///     Create from ZPackage
        /// </summary>
        /// <param name="pkg"></param>
        internal ModuleVersionData(ZPackage pkg)
        {
            try
            {
                // Needed !!
                pkg.SetPos(0);
                ValheimVersion = new System.Version(pkg.ReadInt(), pkg.ReadInt(), pkg.ReadInt());

                var numberOfModules = pkg.ReadInt();

                while (numberOfModules > 0)
                {
                    Modules.Add(new Tuple<string, System.Version, CompatibilityLevel, VersionStrictness>(pkg.ReadString(),
                         new System.Version(pkg.ReadInt(), pkg.ReadInt(), pkg.ReadInt()), (CompatibilityLevel)pkg.ReadInt(),
                         (VersionStrictness)pkg.ReadInt()));
                    numberOfModules--;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Could not deserialize version message data from zPackage");
                Logger.LogError(ex.Message);
            }
        }

        /// <summary>
        ///     Valheim version
        /// </summary>
        public System.Version ValheimVersion { get; }

        /// <summary>
        ///     Module data
        /// </summary>
        public List<Tuple<string, System.Version, CompatibilityLevel, VersionStrictness>> Modules { get; } =
            new List<Tuple<string, System.Version, CompatibilityLevel, VersionStrictness>>();


        /// <summary>
        ///     Create ZPackage
        /// </summary>
        /// <returns>ZPackage</returns>
        public ZPackage ToZPackage()
        {
            var pkg = new ZPackage();
            pkg.Write(ValheimVersion.Major);
            pkg.Write(ValheimVersion.Minor);
            pkg.Write(ValheimVersion.Build);

            pkg.Write(Modules.Count);

            foreach (var module in Modules)
            {
                pkg.Write(module.Item1);
                pkg.Write(module.Item2.Major);
                pkg.Write(module.Item2.Minor);
                pkg.Write(module.Item2.Build);
                pkg.Write((int)module.Item3);
                pkg.Write((int)module.Item4);
            }

            return pkg;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return ((ValheimVersion != null ? ValheimVersion.GetHashCode() : 0) * 397) ^ (Modules != null ? Modules.GetHashCode() : 0);
            }
        }

        // Default ToString override
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Valheim {ValheimVersion.Major}.{ValheimVersion.Minor}.{ValheimVersion.Build}");

            foreach (var mod in Modules)
            {
                sb.AppendLine($"{mod.Item1} {mod.Item2.Major}.{mod.Item2.Minor}.{mod.Item2.Build} {mod.Item3} {mod.Item4}");
            }

            return sb.ToString();
        }

        // Additional ToString method to show data without NetworkCompatibility attribute
        public string ToString(bool showEnforce)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Valheim {ValheimVersion.Major}.{ValheimVersion.Minor}.{ValheimVersion.Build}");

            foreach (var mod in Modules)
            {
                sb.AppendLine($"{mod.Item1} {mod.Item2.Major}.{mod.Item2.Minor}.{mod.Item2.Build}" + (showEnforce ? " {mod.Item3} {mod.Item4}" : ""));
            }

            return sb.ToString();
        }
    }
}
