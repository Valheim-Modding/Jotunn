using System;
using System.Collections.Generic;
using Xunit;

namespace Jotunn.Utils
{
    public class CompatibilityLevelTest
    {
        private static System.Version v_1_0_0 = new System.Version(1, 0, 0);
        private static System.Version v_1_0_5 = new System.Version(1, 1, 5);
        private static System.Version v_1_1_0 = new System.Version(1, 1, 0);
        private static System.Version v_1_1_1 = new System.Version(1, 1, 1);

        private static System.Version v_2_0_0 = new System.Version(2, 0, 0);
        private static System.Version v_2_0_4 = new System.Version(2, 0, 4);
        private static System.Version v_2_2_0 = new System.Version(2, 2, 0);
        private static System.Version v_2_2_2 = new System.Version(2, 2, 2);

        ModuleVersionData clientVersionData;
        ModuleVersionData serverVersionData;

        public CompatibilityLevelTest()
        {
            clientVersionData = new ModuleVersionData(v_1_0_0, new List<ModModule>());
            serverVersionData = new ModuleVersionData(v_1_0_0, new List<ModModule>());
        }

        [Fact]
        public void BothOnlyJotunn()
        {
            Assert.True(ModCompatibility.CompareVersionData(serverVersionData, clientVersionData));
        }

        [Fact]
        public void ClientHasModButServerDoesNot()
        {
            clientVersionData.Modules = new List<ModModule>
            {
                new ModModule("TestMod", v_1_0_0, CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)
            };
            Assert.False(ModCompatibility.CompareVersionData(serverVersionData, clientVersionData));
        }

        [Fact]
        public void ServerHasModButClientDoesntNeedIt()
        {
            serverVersionData.Modules = new List<ModModule>
            {
                new ModModule("TestMod", v_1_0_0, CompatibilityLevel.ServerMustHaveMod, VersionStrictness.Minor)
            };
            Assert.True(ModCompatibility.CompareVersionData(serverVersionData, clientVersionData));
        }

        [Fact]
        public void EqualVersionData()
        {
            Assert.True(ModCompatibility.CompareVersionData(serverVersionData, serverVersionData));
        }

        [Fact]
        public void DifferentValheimVersion()
        {
            clientVersionData.ValheimVersion = v_2_0_0;
            Assert.False(ModCompatibility.CompareVersionData(serverVersionData, clientVersionData));
        }

        [Fact]
        public void ModVersionCompare_MajorStrictness()
        {
            // At least Mayor different
            TestVersionCompare(v_1_0_0, v_2_0_0, CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Major, false);
            TestVersionCompare(v_1_0_5, v_2_0_4, CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Major, false);
            // At least Minor different
            TestVersionCompare(v_1_0_0, v_1_1_0, CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Major, true);
            // At least Patch different
            TestVersionCompare(v_1_1_1, v_1_1_0, CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Major, true);
        }

        [Fact]
        public void ModVersionCompare_MinorStrictness()
        {
            // At least Mayor different
            TestVersionCompare(v_1_0_0, v_2_0_0, CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor, false);
            TestVersionCompare(v_1_0_5, v_2_0_4, CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor, false);
            // At least Minor different
            TestVersionCompare(v_1_0_0, v_1_1_0, CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor, false);
            // At least Patch different
            TestVersionCompare(v_1_1_1, v_1_1_0, CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor, true);
        }

        [Fact]
        public void ModVersionCompare_PatchStrictness()
        {
            // At least Mayor different
            TestVersionCompare(v_1_0_0, v_2_0_0, CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Patch, false);
            TestVersionCompare(v_1_0_5, v_2_0_4, CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Patch, false);
            // At least Minor different
            TestVersionCompare(v_1_1_0, v_1_0_0, CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Patch, false);
            // At least Patch different
            TestVersionCompare(v_1_1_1, v_1_1_0, CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Patch, false);
        }

        private void TestVersionCompare(System.Version v1, System.Version v2, CompatibilityLevel level, VersionStrictness strictness,
            bool expected)
        {
            serverVersionData.Modules = new List<ModModule> { new ModModule("TestMod", v1, level, strictness) };
            clientVersionData.Modules = new List<ModModule> { new ModModule("TestMod", v2, level, strictness) };
            Assert.Equal(ModCompatibility.CompareVersionData(serverVersionData, clientVersionData), expected);

            serverVersionData.Modules = new List<ModModule> { new ModModule("TestMod", v2, level, strictness) };
            clientVersionData.Modules = new List<ModModule> { new ModModule("TestMod", v1, level, strictness) };
            Assert.Equal(ModCompatibility.CompareVersionData(serverVersionData, clientVersionData), expected);
        }
    }
}
