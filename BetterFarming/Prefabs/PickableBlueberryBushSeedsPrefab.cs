using UnityEngine;
using ValheimLokiLoader.Entities;
using ValheimLokiLoader.Managers;

namespace BetterFarming.Prefabs
{
    class PickableBlueberryBushSeedsPrefab : PrefabConfig
    {
        // Create a pickable bush seed prefab that's a copy of the carrot seeds
        public PickableBlueberryBushSeedsPrefab() : base("Pickable_BlueberrySeeds", "Pickable_SeedCarrot")
        {

        }

        // Make changes to the prefab object after it's defined
        public override void Register()
        {
            Prefab.transform.localScale = new Vector3(1f, 1.4f, 1f);

            // Turn all models blue
            foreach (Transform child in Prefab.transform)
            {
                MeshRenderer renderer = child.GetComponent<MeshRenderer>();

                if (renderer)
                {
                    renderer.material.color = new Color(renderer.material.color.r, renderer.material.color.g, 0.9f);
                }
            }

            Pickable pickable = Prefab.GetComponent<Pickable>();
            pickable.m_itemPrefab = PrefabManager.Instance.GetPrefab("BlueberrySeeds");
        }
    }
}
