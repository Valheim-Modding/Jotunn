using System;
using HarmonyLib;
using UnityEngine;

namespace JotunnDoc.Patches
{
    [HarmonyPatch]
    internal class ZRoutedRpcPatches
    {
        [HarmonyPatch(typeof(ZRoutedRpc), nameof(ZRoutedRpc.Register), typeof(string), typeof(Action<long>)), HarmonyPrefix]
        internal static void ZRoutedRpc_Register(string name, Action<long> f)
        {
            Debug.Log("Registered RPC: " + name + " (" + name.GetStableHashCode() + ")");
        }
    }
}
