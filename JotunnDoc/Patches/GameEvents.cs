using System;
using HarmonyLib;

namespace JotunnDoc.Patches
{
    [HarmonyPatch]
    public static class GameEvents
    {
        public static event Action<Player> OnPlayerSpawned;
        public static event Action<Game> OnGameDestory;

        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned)), HarmonyPostfix]
        private static void PlayerOnSpawn(Player __instance)
        {
            OnPlayerSpawned?.Invoke(__instance);
        }

        [HarmonyPatch(typeof(Game), nameof(Game.OnDestroy)), HarmonyPostfix]
        private static void GameOnDestroy(Game __instance)
        {
            OnGameDestory?.Invoke(__instance);
        }
    }
}
