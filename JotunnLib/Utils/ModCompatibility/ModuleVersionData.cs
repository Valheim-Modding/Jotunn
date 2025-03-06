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

        public uint NetworkVersion { get; internal set; }

        public int ModModuleDataLayout { get; private set; }

        /// <summary>
        ///     Whether all the ModModule instances were formatted in a supported 
        ///     data layout and all instances had the same data layout. 
        /// </summary>
        public bool IsSupportedDataLayout
        {
            get
            {
                return ModModule.SupportedDataLayouts.Contains(ModModuleDataLayout);
            }
        }

        /// <summary>
        ///     Create from module data
        /// </summary>
        /// <param name="versionData"></param>
        internal ModuleVersionData(List<ModModule> versionData)
        {
            ValheimVersion = GameVersions.ValheimVersion;
            VersionString = GetVersionString();
            NetworkVersion = GameVersions.NetworkVersion;
            Modules = new List<ModModule>(versionData);
            ModModuleDataLayout = GetModModuleDataLayoutVersion(Modules);
        }

        internal ModuleVersionData(System.Version valheimVersion, List<ModModule> versionData)
        {
            ValheimVersion = valheimVersion;
            VersionString = GetVersionString();
            NetworkVersion = GameVersions.NetworkVersion;
            Modules = new List<ModModule>(versionData);
            ModModuleDataLayout = GetModModuleDataLayoutVersion(Modules);
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

                // Read encoded ModModules in legacy format
                var numberOfLegacyModules = pkg.ReadInt();
                while (numberOfLegacyModules > 0)
                {
                    Modules.Add(new ModModule(pkg, legacy: true));
                    numberOfLegacyModules--;
                }

                if (pkg.m_reader.BaseStream.Position != pkg.m_reader.BaseStream.Length)
                {
                    VersionString = pkg.ReadString();
                }

                if (pkg.m_reader.BaseStream.Position != pkg.m_reader.BaseStream.Length)
                {
                    NetworkVersion = pkg.ReadUInt();
                }

                // Get current data layout ModModules if present
                if (pkg.m_reader.BaseStream.Position != pkg.m_reader.BaseStream.Length)
                {
                    var modModules = new List<ModModule>();

                    // Read and store the relevant ModModules
                    var numberOfModules = pkg.ReadInt();
                    var currentPos = pkg.GetPos();

                    while (numberOfModules > 0)
                    {
                        try
                        {
                            var modModule = new ModModule(pkg, legacy: false);
                            modModules.Add(modModule);
                            numberOfModules--;
                        }
                        catch (NotSupportedException ex)
                        {
                            pkg.SetPos(currentPos);  // get data layout version
                            this.ModModuleDataLayout = pkg.ReadInt(); 
                            Logger.LogError($"Could not parse unsupported data layout version {ModModuleDataLayout} from zPackage");
                            Logger.LogError(ex.Message);

                            // abort reading as the start of the next ModModule is unknown
                            break;
                        }
                    }

                    // overwrite Modules to replace legacy ModModule formatted data with current ModModule formatted data
                    Modules = modModules;
                    ModModuleDataLayout = GetModModuleDataLayoutVersion(Modules);
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
                module.WriteToPackage(pkg, legacy: true);
            }

            pkg.Write(VersionString);
            pkg.Write(NetworkVersion);

            pkg.Write(Modules.Count);
            foreach (var module in Modules)
            {
                module.WriteToPackage(pkg, legacy: false);
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
                sb.AppendLine($"{mod.ModName} {mod.GetVersionString()} {mod.CompatibilityLevel} {mod.VersionStrictness}");
            }

            return sb.ToString();
        }

        // Additional ToString method to show data without NetworkCompatibility attribute
        public string ToString(bool showEnforce)
        {
            var sb = new StringBuilder();

            string versionString = VersionString;

            if (string.IsNullOrEmpty(versionString))
            {
                versionString = $"Valheim {ValheimVersion.Major}.{ValheimVersion.Minor}.{ValheimVersion.Build}";
            }

            if (NetworkVersion > 0)
            {
                sb.AppendLine($"Valheim {versionString} (n-{NetworkVersion})");
            }
            else
            {
                sb.AppendLine($"Valheim {versionString}");
            }

            foreach (var mod in Modules)
            {
                sb.AppendLine($"{mod.ModName} {mod.GetVersionString()}" + (showEnforce ? $" {mod.CompatibilityLevel} {mod.VersionStrictness}" : ""));
            }

            return sb.ToString();
        }

        public ModModule FindModule(ModModule modModule, bool legacyDataLayout)
        {
            if (legacyDataLayout)
            {
                return Modules.FirstOrDefault(x => x.ModName == modModule.ModName);
            }

            return Modules.FirstOrDefault(x => x.ModID == modModule.ModID);
        }

        public bool HasModule(ModModule modModule, bool legacyDataLayout)
        {
            return FindModule(modModule, legacyDataLayout) != null;
        }

        private static string GetVersionString()
        {
            // ServerCharacters replaces the version string on the server but not client and does it's own checks afterwards
            return Version.GetVersionString().Replace("-ServerCharacters", "");
        }

        private static int GetModModuleDataLayoutVersion(List<ModModule> modules)
        {
            // Handle tracking ModModule data layouts 
            if (modules.Any(x => x.DataLayoutVersion != modules.FirstOrDefault()?.DataLayoutVersion))
            {
                throw new NotSupportedException("DataVersionLayout is not the same for all ModModule instances.");
            }

            return modules.FirstOrDefault()?.DataLayoutVersion ?? ModModule.CurrentDataLayoutVersion;
        }
    }
}
