using System;
using System.Collections.Generic;
using UnityEngine;
using JotunnLib.Managers;
using JotunnLib.Utils;

namespace JotunnLib.Patches
{
    class ZoneSystemPatches : PatchInitializer
    {
        internal override void Init()
        {
            On.ZoneSystem.Awake += ZoneSystem_Awake;
            On.ZoneSystem.SpawnZone += ZoneSystem_SpawnZone;
        }

        private static void ZoneSystem_Awake(On.ZoneSystem.orig_Awake orig, ZoneSystem self)
        {
            orig(self);
#if DEBUG
            Debug.Log("----> ZoneSystem Awake");
#endif
            // ZoneManager.Instance.Register();
        }

        private static bool ZoneSystem_SpawnZone(On.ZoneSystem.orig_SpawnZone orig, ZoneSystem self, Vector2i zoneID, ZoneSystem.SpawnMode mode, out GameObject root)
        {
            // Debug.Log("-> Spawning zone: " + zoneID);
            return orig(self, zoneID, mode, out root);
        }
    }
}
