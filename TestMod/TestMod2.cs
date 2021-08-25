// JotunnLib
// a Valheim mod
// 
// File:    TestMod2.cs
// Project: TestMod

using BepInEx;
using BepInEx.Configuration;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace TestMod
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    [BepInDependency(Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Patch)]
    internal class TestMod2 : BaseUnityPlugin
    {
        private const string ModGUID = "com.jotunn.2testmod";
        private const string ModName = "Jotunn Test Mod #2";
        private const string ModVersion = "0.1.0";
        private const string JotunnTestModConfigSection = "JotunnTest2";
        private const string OrderConfigSection = "Order Test";

        public void Awake()
        {
            // Add a client side custom input key for the EvilSword
            var btn2 = Config.Bind(JotunnTestModConfigSection, "EvilSwordSpecialAttack", KeyCode.Z, new ConfigDescription("Key to unleash evil with the Evil Sword"));
            InputManager.Instance.AddButton(ModGUID, new ButtonConfig
            {
                Name = "EvilSwordSpecialAttack",
                Config = btn2,
                HintToken = "$evilsword_beevil"
            });

            // Test config ordering
            Config.Bind(OrderConfigSection, "Order 1", string.Empty,
                new ConfigDescription("Should be first", null,
                    new ConfigurationManagerAttributes() { Order = 1 }));
            
            Config.Bind(OrderConfigSection, "Order 2", string.Empty,
                new ConfigDescription("Should be second", null,
                    new ConfigurationManagerAttributes() { Order = 2 }));
            
            Config.Bind(OrderConfigSection, "Order 3", string.Empty,
                new ConfigDescription("Should be last", null,
                    new ConfigurationManagerAttributes() { Order = 3 }));

            Config.Bind(OrderConfigSection, "Order undefined aaa", string.Empty, "Should be ordered by name at the end");

            Config.Bind(OrderConfigSection, "Order undefined zzz", string.Empty, "Should be ordered by name at the end");
        }
    }
}
