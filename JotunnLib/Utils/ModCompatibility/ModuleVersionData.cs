using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jotunn.Utils
{
    /// <summary>
    ///     Deserialize version string into a usable format.
    /// </summary>
    internal class ModuleVersionData
    {
        /// <summary>
        ///     Valheim version
        /// </summary>
        public System.Version ValheimVersion { get; internal set; }

        /// <summary>
        ///     Module data
        /// </summary>
        public List<ModModule> Modules { get; internal set; } = new List<ModModule>();

        public string VersionString { get; internal set; } = string.Empty;

        /// <summary>
        ///     Create from module data
        /// </summary>
        /// <param name="versionData"></param>
        internal ModuleVersionData(List<ModModule> versionData)
        {
            ValheimVersion = new System.Version(Version.m_major, Version.m_minor, Version.m_patch);
            VersionString = GetVersionString();
            Modules = new List<ModModule>(versionData);
        }

        internal ModuleVersionData(System.Version valheimVersion, List<ModModule> versionData)
        {
            ValheimVersion = valheimVersion;
            VersionString = GetVersionString();
            Modules = new List<ModModule>(versionData);
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

                if (pkg.m_reader.BaseStream.Position != pkg.m_reader.BaseStream.Length)
                {
                    VersionString = pkg.ReadString();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Could not deserialize version message data from zPackage");
                Logger.LogError(ex.Message);
            }
        }

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
                module.WriteToPackage(pkg);
            }

            pkg.Write(VersionString);

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

            if (string.IsNullOrEmpty(VersionString))
            {
                sb.AppendLine($"Valheim {ValheimVersion.Major}.{ValheimVersion.Minor}.{ValheimVersion.Build}");
            }
            else
            {
                sb.AppendLine($"Valheim {VersionString}");
            }

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

            if (string.IsNullOrEmpty(VersionString))
            {
                sb.AppendLine($"Valheim {ValheimVersion.Major}.{ValheimVersion.Minor}.{ValheimVersion.Build}");
            }
            else
            {
                sb.AppendLine($"Valheim {VersionString}");
            }

            foreach (var mod in Modules)
            {
                sb.AppendLine($"{mod.name} {mod.GetVersionString()}" + (showEnforce ? $" {mod.compatibilityLevel} {mod.versionStrictness}" : ""));
            }

            return sb.ToString();
        }

        public ModModule FindModule(string name)
        {
            return Modules.FirstOrDefault(x => x.name == name);
        }

        public bool HasModule(string name)
        {
            return FindModule(name) != null;
        }

        private static string GetVersionString()
        {
            // ServerCharacters replaces the version string on the server but not client and does it's own checks afterwards
            return Version.GetVersionString().Replace("-ServerCharacters", "");
        }
    }
}
