using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using JotunnLib.Managers;

namespace JotunnLib.Patches
{
    class ZNetScenePatches
    {

        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        public static class AwakePatch
        {
            public static void Postfix()
            {
#if DEBUG
                Debug.Log("----> ZNetScene Awake");
#endif
                PrefabManager.Instance.Register();
                PrefabManager.Instance.Load();

                ZoneManager.Instance.Register();
                ZoneManager.Instance.Load();
            }
        }
    }
}
