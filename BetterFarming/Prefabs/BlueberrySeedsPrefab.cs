using UnityEngine;
using JotunnLib.Entities;
using JotunnLib.Managers;

namespace BetterFarming.Prefabs
{
    class BlueberrySeedsPrefab : PrefabConfig
    {
        // Create bush seeds prefab that's a copy of carrot seeds
        public BlueberrySeedsPrefab() : base("BlueberrySeeds", "CarrotSeeds")
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

            // Configure item drop
            ItemDrop item = Prefab.GetComponent<ItemDrop>();
            item.m_itemData.m_shared.m_name = "Blueberry Seeds";
            item.m_itemData.m_shared.m_description = "Plant these if you like Blueberries...";
            item.m_itemData.m_dropPrefab = Prefab;
        }
    }
}
