using System;
using UnityEngine;
using BepInEx;
using ValheimLokiLoader;
using ValheimLokiLoader.ConsoleCommands;
using ValheimLokiLoader.Managers;
using ValheimLokiLoader.Events;
using ValheimLokiLoader.Entities;

namespace SimpleMounts
{
    [BepInPlugin("com.bepinex.plugins.simple-mounts", "Simple Mounts", "0.0.1")]
    [BepInDependency("com.bepinex.plugins.loki-loader")]
    class BetterFarming : BaseUnityPlugin
    {
        void Awake()
        {
            PrefabManager.Instance.PrefabRegister += initPrefabs;
            PrefabManager.Instance.PrefabsLoaded += modifyPrefabs;
            ObjectManager.Instance.ObjectRegister += initObjects;
            InputManager.Instance.InputRegister += initInputs;

            SkillManager.Instance.RegisterSkill("riding", "Riding", "Ride animals");
        }

        private void initInputs(object sender, EventArgs e)
        {
            // Init unmount key
            InputManager.Instance.RegisterButton("Unmount", KeyCode.V);
        }

        private void initPrefabs(object sender, EventArgs e)
        {
            // Init saddle
            PrefabManager.Instance.RegisterPrefab(new SaddlePrefab());
        }

        private void modifyPrefabs(object sender, EventArgs e)
        {
            // Add rideable component to animals
            PrefabManager.Instance.GetPrefab("Deer").AddComponent<Rideable>();
        }

        private void initObjects(object sender, EventArgs e)
        {
            // Objects
            ObjectManager.Instance.RegisterItem("Saddle");

            // Recipes
            ObjectManager.Instance.RegisterRecipe(new RecipeConfig()
            {
                Item = "Saddle",
                CraftingStation = "forge",
                Requirements = new PieceRequirementConfig[]
                {
                    new PieceRequirementConfig()
                    {
                        Item = "Iron",
                        Amount = 4
                    },
                    new PieceRequirementConfig()
                    {
                        Item = "DeerHide",
                        Amount = 10
                    }
                }
            });
        }
    }
}
