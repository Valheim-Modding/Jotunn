using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using ValheimLokiLoader.Managers;

namespace ValheimLokiLoader.Patches
{
    class ZoneSystemPatches
    {

        [HarmonyPatch(typeof(ZoneSystem), "Awake")]
        public static class AwakePatch
        {
            public static void Postfix()
            {
                Debug.Log("----> ZoneSystem Awake");
                // ZoneManager.Instance.Register();
            }
        }

        [HarmonyPatch(typeof(ZoneSystem), "SpawnZone")]
        public static class SpawnZonePatch
        {
            public static void Prefix(Vector2i zoneID)
            {
                // Debug.Log("-> Spawning zone: " + zoneID);
            }
        }
    }
}
