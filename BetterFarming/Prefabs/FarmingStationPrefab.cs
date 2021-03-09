using UnityEngine;
using JotunnLib.Entities;

namespace BetterFarming.Prefabs
{
    class FarmingStationPrefab : PrefabConfig
    {
        public FarmingStationPrefab() : base("FarmingStation", "piece_workbench")
        {

        }

        public override void Register()
        {
            Debug.Log(Prefab);

            // Turn all models green
            foreach (Transform child in Prefab.transform)
            {
                MeshRenderer renderer = child.GetComponent<MeshRenderer>();

                if (renderer)
                {
                    renderer.material.color = new Color(renderer.material.color.r, 0.9f, renderer.material.color.b);
                }
            }

            // Configure piece
            Piece piece = Prefab.GetComponent<Piece>();
            piece.m_name = "Farming Station";
            piece.m_description = "Build this if you like farming";

            // Configure crafting station
            CraftingStation craftingStation = Prefab.GetComponent<CraftingStation>();
            craftingStation.m_name = "Farming Station";
        }
    }
}
