using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using JotunnLib.Utils;
using UnityEngine;

namespace JotunnDoc.Patches
{
    internal class ZRoutedRpcPatches : PatchInitializer
    {
        public override void Init()
        {
            On.ZRoutedRpc.Register += ZRoutedRpc_Register;
        }

        private static void ZRoutedRpc_Register(On.ZRoutedRpc.orig_Register orig, ZRoutedRpc self, string name, Action<long> f)
        {
            Debug.Log("Registered RPC: " + name + " (" + name.GetStableHashCode() + ")");
            orig(self, name, f);
        }

    }
}
