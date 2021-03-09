using UnityEngine;
using JotunnLib.Managers;
using JotunnLib.Entities;

namespace BetterFarming.Prefabs
{
    class RaspberryBushSaplingPrefab : PrefabConfig
    {
        // Create a bush prefab that's a copy of the sapling prefab
        public RaspberryBushSaplingPrefab() : base("Sapling_RaspberryBush", "sapling_carrot")
        {
            
        }

        // Make changes to the prefab object after it's defined
        public override void Register()
        {
            // Turn all models blue
            foreach (Transform child in Prefab.transform)
            {
                MeshRenderer renderer = child.GetComponent<MeshRenderer>();

                if (renderer)
                {
                    renderer.material.color = new Color(0.9f, renderer.material.color.g, renderer.material.color.b);
                }
            }

            // Configure piece
            Piece piece = Prefab.GetComponent<Piece>();
            piece.m_name = "Raspberry Bush Sapling";
            piece.m_description = "Plant raspberry seeds to grow a blueberry bush";
            piece.m_icon = PrefabManager.Instance.GetPrefab("Raspberry").GetComponent<ItemDrop>().m_itemData.GetIcon();
            piece.m_resources = new Piece.Requirement[] {
                new Piece.Requirement()
                {
                    m_amount = 1,
                    m_resItem = PrefabManager.Instance.GetPrefab("RaspberrySeeds").GetComponent<ItemDrop>()
                }
            };

            // Configure plant growth
            Plant plant = Prefab.GetComponent<Plant>();
            plant.m_name = "Raspberry Bush Sapling";
            plant.m_grownPrefabs = new GameObject[] { PrefabManager.Instance.GetPrefab("RaspberryBush") };
        }
    }
}
