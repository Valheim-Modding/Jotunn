using System;
using UnityEngine;
using BepInEx;
using ValheimLokiLoader;
using ValheimLokiLoader.ConsoleCommands;
using ValheimLokiLoader.Managers;
using ValheimLokiLoader.Events;

namespace SimpleMounts
{
    [BepInPlugin("com.bepinex.plugins.simple-mounts", "Simple Mounts", "0.0.1")]
    [BepInDependency("com.bepinex.plugins.loki-loader")]
    class BetterFarming : BaseUnityPlugin
    {
        void Awake()
        {
            PrefabManager.PrefabLoad += initPrefabs;
            PrefabManager.PrefabsLoaded += modifyPrefabs;
            ObjectManager.ObjectLoad += initObjects;

            SkillManager.AddSkill("riding", "Riding", "Ride animals");
        }

        private void initPrefabs(object sender, EventArgs e)
        {
            // Init saddle
            PrefabManager.RegisterPrefab(new SaddlePrefab());
        }

        private void modifyPrefabs(object sender, EventArgs e)
        {
            // Add rideable component to animals
            PrefabManager.GetPrefab("Deer").AddComponent<Rideable>();
        }

        private void initObjects(object sender, EventArgs e)
        {
            // Objects
            ObjectManager.RegisterItem("Saddle");

            // Recipes
            ObjectManager.RegisterRecipe(new Recipe()
            {
                m_item = PrefabManager.GetPrefab("Saddle").GetComponent<ItemDrop>(),
                m_craftingStation = PrefabManager.GetPrefab("forge").GetComponent<CraftingStation>(),
                m_resources = new Piece.Requirement[]
                {
                    new Piece.Requirement()
                    {
                        m_resItem = PrefabManager.GetPrefab("Iron").GetComponent<ItemDrop>(),
                        m_amount = 4
                    },
                    new Piece.Requirement()
                    {
                        m_resItem = PrefabManager.GetPrefab("DeerHide").GetComponent<ItemDrop>(),
                        m_amount = 10
                    }
                }
            });
        }
    }
}
