using UnityEngine;
using ValheimLokiLoader.Managers;
using ValheimLokiLoader.Entities;

namespace BetterFarming.Prefabs
{
    class BlueberryBushSaplingPrefab : PrefabConfig
    {
        // Create a bush prefab that's a copy of the sapling prefab
        public BlueberryBushSaplingPrefab() : base("Sapling_BlueberryBush", "sapling_carrot")
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
                    renderer.material.color = new Color(renderer.material.color.r, renderer.material.color.g, 0.9f);
                }
            }

            // Configure piece
            Piece piece = Prefab.GetComponent<Piece>();
            piece.m_name = "Blueberry Bush Sapling";
            piece.m_description = "Plant blueberry seeds to grow a blueberry bush";
            piece.m_icon = PrefabManager.Instance.GetPrefab("Blueberries").GetComponent<ItemDrop>().m_itemData.GetIcon();
            piece.m_resources = new Piece.Requirement[] {
                new Piece.Requirement()
                {
                    m_amount = 1,
                    m_resItem = PrefabManager.Instance.GetPrefab("BlueberrySeeds").GetComponent<ItemDrop>()
                }
            };

            // Configure plant growth
            Plant plant = Prefab.GetComponent<Plant>();
            plant.m_name = "Blueberry Bush Sapling";
            plant.m_grownPrefabs = new GameObject[] { PrefabManager.Instance.GetPrefab("BlueberryBush") };
        }
    }
}
