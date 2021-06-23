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
using UnityEngine.SceneManagement;

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
        }
    }
}
