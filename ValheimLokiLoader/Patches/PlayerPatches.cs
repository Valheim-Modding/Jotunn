using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;

namespace ValheimLokiLoader.Patches
{
    class PlayerPatches
    {
        [HarmonyPatch(typeof(Player), "OnSpawned")]
        public static class OnSpawnedPatch
        {
            public static void Prefix(ref Player __instance, ref bool ___m_firstSpawn)
            {
                // Disable valkyrie animation during testing for sanity reasons
                ___m_firstSpawn = false;
            }
        }
    }
}
