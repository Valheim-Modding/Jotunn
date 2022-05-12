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
    }
}
