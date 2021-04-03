using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using JotunnLib.Managers;
using JotunnLib.Utils;

namespace JotunnLib.Patches
{
    // TODO: Probably not needed anymore, someone please check - Algorithman
    class ObjectDBPatches 
    {
        [PatchInit(0)]
        public static void Init()
        {
            //On.ObjectDB.Awake += ObjectDB_Awake;

        }

        private static void ObjectDB_Awake(On.ObjectDB.orig_Awake orig, ObjectDB self)
        {
            orig(self);
            Logger.LogDebug("----> ObjectDB Awake");

            if (SceneManager.GetActiveScene().name == "main")
            {
                ItemManager.Instance.Register();
                ItemManager.Instance.Load();

                PieceManager.Instance.Register();
                PieceManager.Instance.Load();
            }
        }
    }
}
