using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using ValheimLokiLoader.Managers;

namespace ValheimLokiLoader.Patches
{
    class ObjectDBPatches
    {

        [HarmonyPatch(typeof(ObjectDB), "Awake")]
        public static class AwakePatch
        {
            public static void Postfix()
            {
                Debug.Log("----> ObjectDB Awake");

                if (SceneManager.GetActiveScene().name == "main")
                {
                    ObjectManager.Instance.Load();
                    PieceManager.Instance.Load();
                }
            }
        }
    }
}
