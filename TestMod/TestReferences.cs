using System.Collections.Generic;
using BepInEx;
using Jotunn;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace TestMod
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    [BepInDependency(Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Patch)]
    internal class TestReferences : BaseUnityPlugin
    {
        private const string ModGUID = "com.jotunn.testreferences";
        private const string ModName = "Jotunn Test Ref Fixer";
        private const string ModVersion = "0.1.0";

        private void Awake()
        {
            PrefabManager.OnVanillaPrefabsAvailable += TestReferenceFix;
        }

        private void TestReferenceFix()
        {
            new FixMe().FixReferences();
            
            PrefabManager.OnVanillaPrefabsAvailable -= TestReferenceFix;
        }

        private class FixMe
        {
            private GameObject[] GameObjectArray =
            {
                new GameObject("JVLmock_Wood")
            };

            private List<GameObject> GameObjectList = new List<GameObject>
            {
                new GameObject("JVLmock_Stone")
            };

            private GameObject[] GameObjectArrayProperty { get; set; } =
            {
                new GameObject("JVLmock_Wood")
            };

            private List<GameObject> GameObjectListProperty { get; set; } = new List<GameObject>
            {
                new GameObject("JVLmock_Stone")
            };
        }
    }
}
