using UnityEngine;
using JotunnLib.Entities;
using JotunnLib.Managers;

namespace BetterFarming.Prefabs
{
    class RaspberrySeedsPrefab : PrefabConfig
    {
        // Create bush seeds prefab that's a copy of carrot seeds
        public RaspberrySeedsPrefab() : base("RaspberrySeeds", "CarrotSeeds")
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

            // Configure item drop
            ItemDrop item = Prefab.GetComponent<ItemDrop>();
            item.m_itemData.m_shared.m_name = "Raspberry Seeds";
            item.m_itemData.m_shared.m_description = "Plant these if you like Raspberries...";
            item.m_itemData.m_dropPrefab = Prefab;
        }
    }
}
