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
                new GameObject("JVLmock_Wood"),
                PrefabManager.Instance.GetPrefab("Wood")
            };

            private List<GameObject> GameObjectList = new List<GameObject>
            {
                new GameObject("JVLmock_Stone"),
                PrefabManager.Instance.GetPrefab("Stone")
            };
            
            private HashSet<StatusEffect> StatusEffectHashSet = new HashSet<StatusEffect>();

            private GameObject[] GameObjectArrayProperty { get; set; } =
            {
                new GameObject("JVLmock_Wood"),
                PrefabManager.Instance.GetPrefab("Wood")
            };

            private List<GameObject> GameObjectListProperty { get; set; } = new List<GameObject>
            {
                new GameObject("JVLmock_Stone"),
                PrefabManager.Instance.GetPrefab("Stone")
            };

            private HashSet<StatusEffect> StatusEffectHashSetProperty { get; set; } = new HashSet<StatusEffect>();
            
            public FixMe()
            {
                var semock = ScriptableObject.CreateInstance<StatusEffect>();
                semock.name = "JVLmock_Burning";
                StatusEffectHashSet.Add(semock);
                StatusEffectHashSet.Add(PrefabManager.Cache.GetPrefab<StatusEffect>("Frost"));
                StatusEffectHashSetProperty.Add(semock);
                StatusEffectHashSetProperty.Add(PrefabManager.Cache.GetPrefab<StatusEffect>("Frost"));
            }
        }
    }
}
