using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;

namespace SimpleMounts
{
    class PlayerPatch
    {
        [HarmonyPatch(typeof(Player), "TakeInput")]
        public static class TakeInputPatch
        {
            public static void Postfix(ref Player __instance, ref bool __result)
            {
                if (Rideable.IsRiding)
                {
                    // __result = false;
                }
            }
        }
    }
}
