using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jotunn.Utils
{
    [TestClass]
    public class CompatibilityLevelTest
    {
        [TestMethod]
        public void BothOnlyJotunn()
        {
            var clientVersionData = new ModuleVersionData(new System.Version(1, 0, 0), new List<ModModule>());
            var serverVersionData = new ModuleVersionData(new System.Version(1, 0, 0), new List<ModModule>());

            Assert.IsTrue(ModCompatibility.CompareVersionData(serverVersionData, clientVersionData));
        }

        [TestMethod]
        public void ClientHasModButServerDoesNot()
        {
            var clientMods = new List<ModModule>
            {
                new ModModule("TestMod", new System.Version(1, 0, 0), CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)
            };
            var clientVersionData = new ModuleVersionData(new System.Version(1, 0, 0), clientMods);
            var serverMods = new List<ModModule>();
            var serverVersionData = new ModuleVersionData(new System.Version(1, 0, 0), serverMods);

            Assert.IsFalse(ModCompatibility.CompareVersionData(serverVersionData, clientVersionData));
        }
         
        [TestMethod]
        public void ServerHasModButClientDoesntNeedIt()
        {
            var clientMods = new List<ModModule>();
            var clientVersionData = new ModuleVersionData(new System.Version(1, 0, 0), clientMods);
            var serverMods = new List<ModModule>
            {
                new ModModule("TestMod", new System.Version(1, 0, 0), CompatibilityLevel.ServerMustHaveMod, VersionStrictness.Minor)
            };
            var serverVersionData = new ModuleVersionData(new System.Version(1, 0, 0), serverMods);

            Assert.IsTrue(ModCompatibility.CompareVersionData(serverVersionData, clientVersionData));
        }
    }
}
