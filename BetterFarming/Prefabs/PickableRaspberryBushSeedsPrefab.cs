using UnityEngine;
using ValheimLokiLoader.Entities;
using ValheimLokiLoader.Managers;

namespace BetterFarming.Prefabs
{
    class PickableRaspberryBushSeedsPrefab : PrefabConfig
    {
        // Create a pickable bush seed prefab that's a copy of the carrot seeds
        public PickableRaspberryBushSeedsPrefab() : base("Pickable_RaspberrySeeds", "Pickable_SeedCarrot")
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
                    renderer.material.color = new Color(0.9f, renderer.material.color.g, renderer.material.color.b);
                }
            }

            Pickable pickable = Prefab.GetComponent<Pickable>();
            pickable.m_itemPrefab = PrefabManager.Instance.GetPrefab("RaspberrySeeds");
        }
    }
}
