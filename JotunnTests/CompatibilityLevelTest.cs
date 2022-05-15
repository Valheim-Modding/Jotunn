using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jotunn.Utils
{
    [TestClass]
    public class CompatibilityLevelTest
    {
        private System.Version v_1_0_0 = new System.Version(1, 0, 0);

        ModuleVersionData clientVersionData;
        ModuleVersionData serverVersionData;

        [TestInitialize]
        public void Setup()
        {
            clientVersionData = new ModuleVersionData(v_1_0_0, new List<ModModule>());
            serverVersionData = new ModuleVersionData(v_1_0_0, new List<ModModule>());
        }

        [TestMethod]
        public void BothOnlyJotunn()
        {
            Assert.IsTrue(ModCompatibility.CompareVersionData(serverVersionData, clientVersionData));
        }

        [TestMethod]
        public void ClientHasModButServerDoesNot()
        {
            clientVersionData.Modules = new List<ModModule>
            {
                new ModModule("TestMod", v_1_0_0, CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)
            };

            Assert.IsFalse(ModCompatibility.CompareVersionData(serverVersionData, clientVersionData));
        }

        [TestMethod]
        public void ServerHasModButClientDoesntNeedIt()
        {
            serverVersionData.Modules = new List<ModModule>
            {
                new ModModule("TestMod", v_1_0_0, CompatibilityLevel.ServerMustHaveMod, VersionStrictness.Minor)
            };

            Assert.IsTrue(ModCompatibility.CompareVersionData(serverVersionData, clientVersionData));
        }
    }
}
