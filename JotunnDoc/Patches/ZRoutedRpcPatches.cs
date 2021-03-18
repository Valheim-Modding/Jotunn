using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using HarmonyLib;

namespace JotunnDoc.Patches
{
    class ZRoutedRpcPatches
    {
        [HarmonyPatch(typeof(ZRoutedRpc), "Register", typeof(string), typeof(Action))]
        public static class Register
        {
            public static void Postfix(string name)
            {
                Debug.Log("Registered RPC: " + name + " (" + name.GetStableHashCode() + ")");
            }
        }
    }
}
