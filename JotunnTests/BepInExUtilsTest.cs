using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using Xunit;

namespace Jotunn.Utils
{
    public class BepInExUtilsTest
    {
        private const string Guid = "JotunnTests.UnregisteredPlugin";

        public BepInExUtilsTest()
        {
            // Chainloader's static constructor binds the core config file, which needs a path outside a game install.
            if (BepInEx.Paths.BepInExConfigPath == null)
            {
                string config = Path.Combine(Path.GetTempPath(), "JotunnTests", "BepInEx.cfg");
                typeof(BepInEx.Paths).GetProperty(nameof(BepInEx.Paths.BepInExConfigPath)).GetSetMethod(true).Invoke(null, new object[] { config });
            }
        }

        [Fact]
        public void AssemblyMissBeforeRegistrationIsNotPermanent()
        {
            Assembly assembly = typeof(BepInExUtilsTest).Assembly;
            Assert.Null(BepInExUtils.GetPluginInfoFromAssembly(assembly));

            // BepInEx loads a plugin's assembly before it registers the plugin; the earlier miss must not stick.
            PluginInfo info = CreatePluginInfo(Guid, typeof(BepInExUtilsTest).FullName);
            Chainloader.PluginInfos[Guid] = info;
            try
            {
                Assert.Same(info, BepInExUtils.GetPluginInfoFromAssembly(assembly));
            }
            finally
            {
                Chainloader.PluginInfos.Remove(Guid);
            }
        }

        private static PluginInfo CreatePluginInfo(string guid, string typeName)
        {
            PluginInfo info = new PluginInfo();
            SetInternal(info, nameof(PluginInfo.Metadata), new BepInPlugin(guid, guid, "1.0.0"));
            SetInternal(info, "TypeName", typeName);
            return info;
        }

        private static void SetInternal(PluginInfo info, string property, object value)
        {
            typeof(PluginInfo).GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .GetSetMethod(true).Invoke(info, new[] { value });
        }
    }
}
