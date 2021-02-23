using System.Collections.Generic;
using UnityEngine;

namespace ValheimLokiLoader.ConsoleCommands
{
    public class PieceTestCommand : ConsoleCommand
    {
        public override string Name => "piece_test";

        public override string Help => "Something to do with build pieces?";

        public override void Run(string[] args)
        {
            /*
            ZNetScene __instance = ZNetScene.instance;
            var ___m_namedPrefabs = Util.GetPrivateField<Dictionary<int, GameObject>>(__instance, "m_namedPrefabs");

            Debug.Log("---- Starting creation of bush ----");
            GameObject carrotSaplingPrefab = __instance.GetPrefab("sapling_carrot");

            if (carrotSaplingPrefab == null)
            {
                Debug.LogError("carrot sapling prefab not found");
                return;
            }

            GameObject grownBushPrefab = __instance.GetPrefab("BlueberryBush");

            if (grownBushPrefab == null)
            {
                Debug.LogError("grown bush prefab not found");
                return;
            }

            // GameObject bushPlantPrefab = UnityEngine.Object.Instantiate(carrotSaplingPrefab);
            // GameObject.DontDestroyOnLoad(bushPlantPrefab);

            GameObject bushPlantPrefab = UnityEngine.Object.Instantiate<GameObject>(carrotSaplingPrefab, Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 2f + Vector3.up, Quaternion.identity);

            if (bushPlantPrefab == null)
            {
                Debug.LogError("prefab null");
                return;
            }

            bushPlantPrefab.name = "blueberry_bush_sapling";

            Piece piece = bushPlantPrefab.GetComponent<Piece>();
            piece.m_name = "blueberry bush sapling";
            piece.m_description = "blueberry bush seed desc";

            Plant plant = bushPlantPrefab.GetComponent<Plant>();
            plant.m_name = "blueberry bush sapling";
            plant.m_grownPrefabs = new GameObject[] { grownBushPrefab };
            plant.m_growTime = 1f;
            plant.m_growTimeMax = 2f;

            // __instance.m_prefabs.Add(bushPlantPrefab);
            // ___m_namedPrefabs.Add(bushPlantPrefab.name.GetStableHashCode(), bushPlantPrefab);

            Debug.Log("---- Registered prefab ----");
            Debug.Log("Prefab: " + bushPlantPrefab.name);
            Debug.Log(bushPlantPrefab);
            */
        }
    }
}
