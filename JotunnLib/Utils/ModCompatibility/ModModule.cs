using BepInEx;

namespace Jotunn.Utils
{
    internal class ModModule
    {
        public string name;
        public System.Version version;
        public CompatibilityLevel compatibilityLevel;
        public VersionStrictness versionStrictness;

        public ModModule(string name, System.Version version, CompatibilityLevel compatibilityLevel, VersionStrictness versionStrictness)
        {
            this.name = name;
            this.version = version;
            this.compatibilityLevel = compatibilityLevel;
            this.versionStrictness = versionStrictness;
        }

        public ModModule(ZPackage pkg)
        {
            name = pkg.ReadString();
            version = new System.Version(pkg.ReadInt(), pkg.ReadInt(), pkg.ReadInt());
            compatibilityLevel = (CompatibilityLevel)pkg.ReadInt();
            versionStrictness = (VersionStrictness)pkg.ReadInt();
        }

        public void WriteToPackage(ZPackage pkg)
        {
            pkg.Write(name);
            pkg.Write(version.Major);
            pkg.Write(version.Minor);
            pkg.Write(version.Build);
            pkg.Write((int)compatibilityLevel);
            pkg.Write((int)versionStrictness);
        }

        public ModModule(BepInPlugin plugin, NetworkCompatibilityAttribute networkAttribute)
        {
            this.name = plugin.Name;
            this.version = plugin.Version;
            this.compatibilityLevel = networkAttribute.EnforceModOnClients;
            this.versionStrictness = networkAttribute.EnforceSameVersion;
        }

        public string GetVersionString()
        {
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }

        /// <summary>
        ///     Module must at least be loaded on the server
        /// </summary>
        /// <returns></returns>
        public bool IsNeededOnServer()
        {
            return compatibilityLevel == CompatibilityLevel.EveryoneMustHaveMod || compatibilityLevel == CompatibilityLevel.ServerMustHaveMod;
        }

        /// <summary>
        ///     Module must at least be loaded on the client
        /// </summary>
        /// <returns></returns>
        public bool IsNeededOnClient()
        {
            return compatibilityLevel == CompatibilityLevel.EveryoneMustHaveMod || compatibilityLevel == CompatibilityLevel.ClientMustHaveMod;
        }

        /// <summary>
        ///    Module is not enforced by the server or client
        /// </summary>
        /// <returns></returns>
        public bool IsNotEnforced()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return compatibilityLevel == CompatibilityLevel.NotEnforced || compatibilityLevel == CompatibilityLevel.NoNeedForSync;
#pragma warning restore CS0618 // Type or member is obsolete
        }

        /// <summary>
        ///     Module is not enforced, only version check if both client and server have it
        /// </summary>
        /// <returns></returns>
        public bool OnlyVersionCheck()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return compatibilityLevel == CompatibilityLevel.OnlySyncWhenInstalled || compatibilityLevel == CompatibilityLevel.VersionCheckOnly;
#pragma warning restore CS0618 // Type or member is obsolete
        }

        /// <summary>
        ///     Checks if the compare module has a lower version then the other base module
        /// </summary>
        /// <param name="baseModule"></param>
        /// <param name="compareModule"></param>
        /// <param name="strictness"></param>
        /// <returns></returns>
        public static bool IsLowerVersion(ModModule baseModule, ModModule compareModule, VersionStrictness strictness)
        {
            if (strictness == VersionStrictness.None)
            {
                return false;
            }

            bool majorSmaller = compareModule.version.Major < baseModule.version.Major;
            bool minorSmaller = compareModule.version.Minor < baseModule.version.Minor;
            bool patchSmaller = compareModule.version.Build < baseModule.version.Build;

            bool majorEqual = compareModule.version.Major == baseModule.version.Major;
            bool minorEqual = compareModule.version.Minor == baseModule.version.Minor;

            if (strictness >= VersionStrictness.Major && majorSmaller)
            {
                return true;
            }

            if (strictness >= VersionStrictness.Minor && minorSmaller && (majorSmaller || majorEqual))
            {
                return true;
            }

            if (strictness >= VersionStrictness.Patch && patchSmaller && (minorSmaller || minorEqual) && (majorSmaller || majorEqual))
            {
                return true;
            }

            return false;
        }
    }
}
