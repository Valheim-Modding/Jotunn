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
            var clientVersionData = new ModuleVersionData(new System.Version(1, 0, 0), new List<Tuple<string, System.Version, CompatibilityLevel, VersionStrictness>>());
            var serverVersionData = new ModuleVersionData(new System.Version(1, 0, 0), new List<Tuple<string, System.Version, CompatibilityLevel, VersionStrictness>>());

            Assert.IsTrue(ModCompatibility.CompareVersionData(serverVersionData, clientVersionData));
        }

        [TestMethod]
        public void ClientHasModButServerDoesNot()
        {
            var clientMods = new List<Tuple<string, System.Version, CompatibilityLevel, VersionStrictness>>
            {
                new Tuple<string, System.Version, CompatibilityLevel, VersionStrictness>("TestMod", new System.Version(1, 0, 0), CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)
            };
            var clientVersionData = new ModuleVersionData(new System.Version(1, 0, 0), clientMods);
            var serverMods = new List<Tuple<string, System.Version, CompatibilityLevel, VersionStrictness>>();
            var serverVersionData = new ModuleVersionData(new System.Version(1, 0, 0), serverMods);

            Assert.IsFalse(ModCompatibility.CompareVersionData(serverVersionData, clientVersionData));
        }
         
        [TestMethod]
        public void ServerHasModButClientDoesntNeedIt()
        {
            var clientMods = new List<Tuple<string, System.Version, CompatibilityLevel, VersionStrictness>>();
            var clientVersionData = new ModuleVersionData(new System.Version(1, 0, 0), clientMods);
            var serverMods = new List<Tuple<string, System.Version, CompatibilityLevel, VersionStrictness>>
            {
                new Tuple<string, System.Version, CompatibilityLevel, VersionStrictness>("TestMod", new System.Version(1, 0, 0), CompatibilityLevel.ServerMustHaveMod, VersionStrictness.Minor)
            };
            var serverVersionData = new ModuleVersionData(new System.Version(1, 0, 0), serverMods);

            Assert.IsTrue(ModCompatibility.CompareVersionData(serverVersionData, clientVersionData));
        }
    }
}
