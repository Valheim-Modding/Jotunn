using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using JotunnLib.Managers;

namespace JotunnLib.Patches
{
    class ObjectDBPatches
    {

        [HarmonyPatch(typeof(ObjectDB), "Awake")]
        public static class AwakePatch
        {
            public static void Postfix()
            {
#if DEBUG
                Debug.Log("----> ObjectDB Awake");
#endif

                if (SceneManager.GetActiveScene().name == "main")
                {
                    ObjectManager.Instance.Register();
                    ObjectManager.Instance.Load();

                    PieceManager.Instance.Register();
                    PieceManager.Instance.Load();
                }
            }
        }
    }
}
