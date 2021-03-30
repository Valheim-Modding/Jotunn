using System;
using System.Collections.Generic;
using UnityEngine;
using JotunnLib.Managers;
using JotunnLib.Utils;

namespace JotunnLib.Patches
{
    class ZNetScenePatches : PatchInitializer
    {

        internal override void Init()
        {
            On.ZNetScene.Awake += ZNetScene_Awake;
        }

        private static void ZNetScene_Awake(On.ZNetScene.orig_Awake orig, ZNetScene self)
        {
            orig(self);

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
