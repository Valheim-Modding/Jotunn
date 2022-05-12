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
        internal ModuleVersionData(List<ModModule> versionData)
        {
            ValheimVersion = new System.Version(Version.m_major, Version.m_minor, Version.m_patch);
            Modules = new List<ModModule>();
            Modules.AddRange(versionData);
        }

        internal ModuleVersionData(System.Version valheimVersion, List<ModModule> versionData)
        {
            ValheimVersion = valheimVersion;
            Modules = new List<ModModule>();
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
                    Modules.Add(new ModModule(pkg));
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
        public List<ModModule> Modules { get; } = new List<ModModule>();

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
                pkg.Write(module.name);
                pkg.Write(module.version.Major);
                pkg.Write(module.version.Minor);
                pkg.Write(module.version.Build);
                pkg.Write((int)module.compatibilityLevel);
                pkg.Write((int)module.versionStrictness);
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
                sb.AppendLine($"{mod.name} {mod.GetVersionString()} {mod.compatibilityLevel} {mod.versionStrictness}");
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
                sb.AppendLine($"{mod.name} {mod.GetVersionString()}" + (showEnforce ? $" {mod.compatibilityLevel} {mod.versionStrictness}" : ""));
            }

            return sb.ToString();
        }
    }
}
