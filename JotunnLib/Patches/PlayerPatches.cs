using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using HarmonyLib;
using JotunnLib.Managers;

namespace JotunnLib.Patches
{
    class PlayerPatches
    {
        [HarmonyPatch(typeof(Player), "OnSpawned")]
        public static class OnSpawnedPatch
        {
            public static void Prefix(ref Player __instance, ref bool ___m_firstSpawn)
            {
                // Temp: disable valkyrie animation during testing for sanity reasons
                ___m_firstSpawn = false;

                EventManager.OnPlayerSpawned(__instance);
            }
        }

        [HarmonyPatch(typeof(Player), "Load")]
        public static class LoadPatch
        {
            public static void Prefix(ref Player __instance)
            {
                Debug.Log("----> Loading player: " + __instance.m_name);
            }
        }


        [HarmonyPatch(typeof(Player), "PlacePiece")]
        public static class PlacePiecePatch
        {
            public static void Postfix(ref Player __instance, Piece piece, bool __result, GameObject ___m_placementGhost)
            {
                EventManager.OnPlayerPlacedPiece(new Events.PlayerPlacedPieceEventArgs()
                {
                    Player = __instance,
                    Piece = piece,
                    Position = ___m_placementGhost.transform.position,
                    Rotation = ___m_placementGhost.transform.rotation,
                    Success = __result
                });
            }
        }
    }
}
