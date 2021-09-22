using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jotunn.Utils
{
    [TestClass]
    public class CompatibilityLevelTest
    {
        private static string ToString(List<Tuple<UnityEngine.Color, string>> errors)
        {
            return string.Join("\n", errors.Select(errorMessage => errorMessage.Item2)).Trim();
        }

        [TestMethod]
        public void BothOnlyJotunn()
        {
            var clientVersionData = new ModCompatibility.ModuleVersionData(new Version(1, 0, 0), new List<Tuple<string, Version, CompatibilityLevel, VersionStrictness>>());
            var serverVersionData = new ModCompatibility.ModuleVersionData(new Version(1, 0, 0), new List<Tuple<string, Version, CompatibilityLevel, VersionStrictness>>());

            var errors = ModCompatibility.CreateErrorMessage(serverVersionData, clientVersionData).ToList();
            Assert.IsFalse(errors.Any(), ToString(errors));
        }

        [TestMethod]
        public void ClientHasModButServerDoesNot()
        {
            var clientMods = new List<Tuple<string, Version, CompatibilityLevel, VersionStrictness>>
            {
                new Tuple<string, Version, CompatibilityLevel, VersionStrictness>("TestMod", new Version(1, 0, 0), CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)
            };
            var clientVersionData = new ModCompatibility.ModuleVersionData(new Version(1, 0, 0), clientMods);
            var serverMods = new List<Tuple<string, Version, CompatibilityLevel, VersionStrictness>>();
            var serverVersionData = new ModCompatibility.ModuleVersionData(new Version(1, 0, 0), serverMods);

            List<Tuple<UnityEngine.Color, string>> errors = ModCompatibility.CreateErrorMessage(serverVersionData, clientVersionData).ToList();

            Assert.IsTrue(errors.Any());
            Assert.AreEqual(ToString(errors),
                "Additional mod detected:\n" +
                "Mod TestMod v1.0.0 is not installed on the server.\n" +
                "Please consider uninstalling this mod."
            );
        }
         
        [TestMethod]
        public void ServerHasModButClientDoesntNeedIt()
        {
            var clientMods = new List<Tuple<string, Version, CompatibilityLevel, VersionStrictness>>();
            var clientVersionData = new ModCompatibility.ModuleVersionData(new Version(1, 0, 0), clientMods);
            var serverMods = new List<Tuple<string, Version, CompatibilityLevel, VersionStrictness>>
            {
                new Tuple<string, Version, CompatibilityLevel, VersionStrictness>("TestMod", new Version(1, 0, 0), CompatibilityLevel.ServerMustHaveMod, VersionStrictness.Minor)
            };
            var serverVersionData = new ModCompatibility.ModuleVersionData(new Version(1, 0, 0), serverMods);

            List<Tuple<UnityEngine.Color, string>> errors = ModCompatibility.CreateErrorMessage(serverVersionData, clientVersionData).ToList();
            Assert.IsFalse(errors.Any(), ToString(errors));
        }
    }
}
