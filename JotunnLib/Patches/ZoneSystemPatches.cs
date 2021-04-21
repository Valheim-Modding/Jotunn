using System;
using System.Collections.Generic;
using UnityEngine;
using Jotunn.Managers;
using Jotunn.Utils;

namespace Jotunn.Patches
{
    internal class ZoneSystemPatches
    {
        [PatchInit(0)]
        public static void Init()
        {
            On.ZoneSystem.Awake += ZoneSystem_Awake;
            On.ZoneSystem.SpawnZone += ZoneSystem_SpawnZone;
        }

        private static void ZoneSystem_Awake(On.ZoneSystem.orig_Awake orig, ZoneSystem self)
        {
            orig(self);

            Logger.LogDebug("----> ZoneSystem Awake");

            // ZoneManager.instance.Register();
        }

        private static bool ZoneSystem_SpawnZone(On.ZoneSystem.orig_SpawnZone orig, ZoneSystem self, Vector2i zoneID, ZoneSystem.SpawnMode mode, out GameObject root)
        {
            // Logger.LogInfo("-> Spawning zone: " + zoneID);
            return orig(self, zoneID, mode, out root);
        }
    }
}
