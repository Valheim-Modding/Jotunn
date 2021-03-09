using UnityEngine;
using HarmonyLib;
using JotunnLib.Managers;

namespace JotunnLib.Patches
{
    internal class ZInputPatches
    {
        [HarmonyPatch(typeof(ZInput), "Initialize")]
        public static class InitializePatch
        {
            public static void Prefix()
            {
                Debug.Log("----> ZInput Initialize");
                InputManager.Instance.Register();
            }
        }

        [HarmonyPatch(typeof(ZInput), "Reset")]
        public static class ResetPatch
        {
            public static void Postfix(ref ZInput __instance)
            {
                Debug.Log("----> ZInput Reset");
                InputManager.Instance.Load(__instance);
            }
        }
    }
}
