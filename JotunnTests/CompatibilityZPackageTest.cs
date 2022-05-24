using System.Collections.Generic;
using Xunit;

namespace Jotunn.Utils
{
    public class CompatibilityZPackageTest
    {
        private static System.Version v_1_0_5 = new System.Version(1, 1, 5);

        private ModuleVersionData moduleData;

        public CompatibilityZPackageTest()
        {
            moduleData = new ModuleVersionData(v_1_0_5, new List<ModModule>());
        }
        
        [Fact]
        public void ModModuleZPackage()
        {
            ModModule module = new ModModule("TestMod", v_1_0_5, CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor);

            ZPackage pkg = new ZPackage();
            module.WriteToPackage(pkg);
            pkg.SetPos(0);
            ModModule result = new ModModule(pkg);

            Assert.Equal(module.name, result.name);
            Assert.Equal(module.version, result.version);
            Assert.Equal(module.compatibilityLevel, result.compatibilityLevel);
            Assert.Equal(module.versionStrictness, result.versionStrictness);
        }

        [Fact]
        public void ModuleVersionDataZPackage()
        {
            moduleData.ValheimVersion = v_1_0_5;
            moduleData.VersionString = "1.2.3-Test";
            moduleData.Modules = new List<ModModule> { new ModModule("TestMod", v_1_0_5, CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor) };

            ZPackage pkg = moduleData.ToZPackage();
            pkg.SetPos(0);

            ModuleVersionData result = new ModuleVersionData(pkg);

            Assert.Equal(moduleData.VersionString, result.VersionString);
            Assert.Equal(moduleData.ValheimVersion, result.ValheimVersion);
            Assert.Equal(moduleData.Modules.Count, result.Modules.Count);
        }

        [Fact]
        public void ModuleVersionDataZPackage_OldVersion()
        {
            moduleData.ValheimVersion = v_1_0_5;
            moduleData.VersionString = "1.2.3-Test";
            moduleData.Modules = new List<ModModule> { new ModModule("TestMod", v_1_0_5, CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor) };

            var pkg = new ZPackage();
            pkg.Write(moduleData.ValheimVersion.Major);
            pkg.Write(moduleData.ValheimVersion.Minor);
            pkg.Write(moduleData.ValheimVersion.Build);

            pkg.Write(moduleData.Modules.Count);

            foreach (var module in moduleData.Modules)
            {
                module.WriteToPackage(pkg);
            }

            pkg.SetPos(0);

            ModuleVersionData result = new ModuleVersionData(pkg);

            Assert.Equal("", result.VersionString);
            Assert.Equal(moduleData.ValheimVersion, result.ValheimVersion);
            Assert.Equal(moduleData.Modules.Count, result.Modules.Count);
        }
    }
}
