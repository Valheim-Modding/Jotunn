using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using ValheimLokiLoader.Managers;

namespace ValheimLokiLoader.Patches
{
    class ZNetScenePatches
    {

        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        public static class AwakePatch
        {
            public static void Postfix(ref ZNetScene __instance, ref Dictionary<int, GameObject> ___m_namedPrefabs)
            {
                Debug.Log("---- Registering custom prefabs ----");

                // Call event handlers to load prefabs
                PrefabManager.LoadPrefabs();

                // Load prefabs into game
                foreach (var pair in PrefabManager.Prefabs)
                {
                    GameObject prefab = pair.Value;

                    __instance.m_prefabs.Add(prefab);
                    ___m_namedPrefabs.Add(prefab.name.GetStableHashCode(), prefab);

                    Debug.Log("Added prefab: " + pair.Key);
                }
            }
        }
    }
}
