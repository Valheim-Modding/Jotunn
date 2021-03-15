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
#if DEBUG
                Debug.Log("----> ZInput Initialize");
#endif
                InputManager.Instance.Register();
            }
        }

        [HarmonyPatch(typeof(ZInput), "Reset")]
        public static class ResetPatch
        {
            public static void Postfix(ref ZInput __instance)
            {
#if DEBUG
                Debug.Log("----> ZInput Reset");
#endif
                InputManager.Instance.Load(__instance);
            }
        }
    }
}
