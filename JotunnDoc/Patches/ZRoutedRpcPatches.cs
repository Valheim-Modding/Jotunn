using System;
using JotunnLib.Utils;
using UnityEngine;

namespace JotunnDoc.Patches
{
    internal class ZRoutedRpcPatches
    {
        [PatchInit(0)]
        public static void Init()
        {
            On.ZRoutedRpc.Register += ZRoutedRpc_Register;
        }

        internal static void ZRoutedRpc_Register(On.ZRoutedRpc.orig_Register orig, ZRoutedRpc self, string name, Action<long> f)
        {
            Debug.Log("Registered RPC: " + name + " (" + name.GetStableHashCode() + ")");
            orig(self, name, f);
        }

    }
}
