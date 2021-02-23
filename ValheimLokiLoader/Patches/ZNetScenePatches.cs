using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace ValheimLokiLoader.Patches
{
    class ZNetScenePatches
    {
        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        public static class AwakePatch
        {
            // TODO: Register new prefabs here

            public static void Prefix(ref ZNetScene __instance, ref Dictionary<int, GameObject> ___m_namedPrefabs)
            {
                Debug.Log("---- ZNetScene Awake ----");

                /*
                Debug.Log("---- Starting creation of bush ----");
                // GameObject carrotSaplingPrefab = __instance.GetPrefab("sapling_carrot");
                GameObject carrotSaplingPrefab = __instance.m_prefabs.Find(p => p.name == "sapling_carrot");

                if (carrotSaplingPrefab == null)
                {
                    Debug.LogError("carrot sapling prefab not found");
                    return;
                }

                // GameObject grownBushPrefab = __instance.GetPrefab("BlueberryBush");
                GameObject grownBushPrefab = __instance.m_prefabs.Find(p => p.name == "BlueberryBush");

                if (grownBushPrefab == null)
                {
                    Debug.LogError("grown bush prefab not found");
                    return;
                }

                GameObject bushPlantPrefab = UnityEngine.Object.Instantiate(carrotSaplingPrefab);
                bushPlantPrefab.name = "blueberry_bush_sapling";
                __instance.m_prefabs.Add(bushPlantPrefab);
                //___m_namedPrefabs.Add(bushPlantPrefab.name.GetStableHashCode(), bushPlantPrefab);

                Piece piece = bushPlantPrefab.GetComponent<Piece>();
                piece.m_name = "blueberry bush sapling";
                piece.m_description = "blueberry bush seed desc";

                Plant plant = bushPlantPrefab.GetComponent<Plant>();
                plant.m_name = "blueberry bush sapling";
                plant.m_grownPrefabs = new GameObject[] { grownBushPrefab };
                plant.m_growTime = 1f;
                plant.m_growTimeMax = 2f;

                Debug.Log("---- Registered prefab ----");
                */
            }
        }
    }
}
