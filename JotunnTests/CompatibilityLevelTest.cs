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

        public static IEnumerable<object[]> MajorStrictnessData()
        {
            // Mayor different
            yield return new object[] { v_1_0_0, v_2_0_0, false };
            yield return new object[] { v_2_0_0, v_1_0_0, false };
            yield return new object[] { v_1_0_5, v_2_0_4, false };
            // Minor different
            yield return new object[] { v_1_0_0, v_1_1_0, true };
            yield return new object[] { v_1_1_0, v_1_0_0, true };
            // Patch different
            yield return new object[] { v_1_1_1, v_1_1_0, true };
            yield return new object[] { v_1_1_0, v_1_1_1, true };
        }

        [Theory]
        [MemberData(nameof(MajorStrictnessData))]
        public void ModVersionCompare_MajorStrictness(System.Version modServerVersion, System.Version modClientVersion, bool expectedResult)
        {
            serverVersionData.Modules = new List<ModModule>
            {
                new ModModule("TestMod", modServerVersion, CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Major)
            };
            clientVersionData.Modules = new List<ModModule>
            {
                new ModModule("TestMod", modClientVersion, CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Major)
            };
            Assert.Equal(ModCompatibility.CompareVersionData(serverVersionData, clientVersionData), expectedResult);
        }

        public static IEnumerable<object[]> MinorStrictnessData()
        {
            // Mayor different
            yield return new object[] { v_1_0_0, v_2_0_0, false };
            yield return new object[] { v_2_0_0, v_1_0_0, false };
            yield return new object[] { v_1_0_5, v_2_0_4, false };
            // Mayor same
            yield return new object[] { v_2_0_0, v_2_0_4, true };
            yield return new object[] { v_2_0_4, v_2_0_0, true };
            // Minor different
            yield return new object[] { v_1_0_0, v_1_1_0, false };
            yield return new object[] { v_1_1_0, v_1_0_0, false };
            // Patch different
            yield return new object[] { v_1_1_1, v_1_1_0, true };
            yield return new object[] { v_1_1_0, v_1_1_1, true };
        }

        [Theory]
        [MemberData(nameof(MinorStrictnessData))]
        public void ModVersionCompare_MinorStrictness(System.Version modServerVersion, System.Version modClientVersion, bool expectedResult)
        {
            serverVersionData.Modules = new List<ModModule>
            {
                new ModModule("TestMod", modServerVersion, CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)
            };
            clientVersionData.Modules = new List<ModModule>
            {
                new ModModule("TestMod", modClientVersion, CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)
            };
            Assert.Equal(ModCompatibility.CompareVersionData(serverVersionData, clientVersionData), expectedResult);
        }

        public static IEnumerable<object[]> PatchStrictnessData()
        {
            // Mayor different
            yield return new object[] { v_1_0_0, v_2_0_0, false };
            yield return new object[] { v_2_0_0, v_1_0_0, false };
            yield return new object[] { v_1_0_5, v_2_0_4, false };
            // Minor different
            yield return new object[] { v_1_0_0, v_1_1_0, false };
            yield return new object[] { v_1_1_0, v_1_0_0, false };
            // Patch different
            yield return new object[] { v_1_1_1, v_1_1_0, false };
            yield return new object[] { v_1_1_0, v_1_1_1, false };
        }

        [Theory]
        [MemberData(nameof(PatchStrictnessData))]
        public void ModVersionCompare_PatchStrictness(System.Version modServerVersion, System.Version modClientVersion, bool expectedResult)
        {
            serverVersionData.Modules = new List<ModModule>
            {
                new ModModule("TestMod", modServerVersion, CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Patch)
            };
            clientVersionData.Modules = new List<ModModule>
            {
                new ModModule("TestMod", modClientVersion, CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Patch)
            };
            Assert.Equal(ModCompatibility.CompareVersionData(serverVersionData, clientVersionData), expectedResult);
        }
    }
}
