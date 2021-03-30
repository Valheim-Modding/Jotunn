using UnityEngine;
using JotunnLib.Managers;
using JotunnLib.Utils;
using UnityEngine.UI;

namespace JotunnLib.Patches
{
    internal class ZInputPatches : PatchInitializer
    {
        internal override void Init()
        {
            On.ZInput.Initialize += ZInput_Initialize;
            On.ZInput.Reset += ZInput_Reset;
        }

        private static void ZInput_Reset(On.ZInput.orig_Reset orig, ZInput self)
        {
            orig(self);
#if DEBUG
            Debug.Log("----> ZInput Reset");
#endif
            InputManager.Instance.Load(self);
        }

        private static void ZInput_Initialize(On.ZInput.orig_Initialize orig)
        {
#if DEBUG
            Debug.Log("----> ZInput Initialize");
#endif
            InputManager.Instance.Register();
            
            orig();
        }
    }
}
