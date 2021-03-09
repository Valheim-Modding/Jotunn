using System;
using UnityEngine;
using BepInEx;
using ValheimLokiLoader;
using ValheimLokiLoader.ConsoleCommands;
using ValheimLokiLoader.Managers;
using ValheimLokiLoader.Events;
using BetterFarming.Prefabs;

namespace BetterFarming
{
    [BepInPlugin("com.bepinex.plugins.better-farming", "Better Farming", "0.0.1")]
    [BepInDependency("com.bepinex.plugins.loki-loader")]
    class BetterFarming : BaseUnityPlugin
    {
        void Awake()
        {
            initSkills();

            ObjectManager.Instance.ObjectRegister += initObjects;
            PrefabManager.Instance.PrefabRegister += initPrefabs;
            PieceManager.Instance.PieceRegister += initPieces;
            ZoneManager.Instance.ZoneLoad += initZone;

            EventManager.PlayerPlacedPiece += onPlayerPlacedPiece;
        }

        // Callback for when a player places a piece
        private void onPlayerPlacedPiece(object sender, PlayerPlacedPieceEventArgs e)
        {
            // If player successfully plants something
            if (e.Success && e.Piece.gameObject.GetComponent<Plant>())
            {
                // Give player farming experience
                e.Player.RaiseSkill(SkillManager.Instance.GetSkill("farming").m_skill);

                // Decerease time it takes to grow
                float factor = getGrowTimeDecreaseFactor();
                Plant plant = e.Piece.GetComponent<Plant>();
                plant.m_growTimeMax *= factor;
                plant.m_growTime *= factor;
            }
        }

        // Compute by what factor to decrease plant grow time based on current farming skill level
        private float getGrowTimeDecreaseFactor()
        {
            float skillLevel = Player.m_localPlayer.GetSkillFactor(SkillManager.Instance.GetSkill("farming").m_skill);
            float maxDecreaseFactor = 0.5f;

            return (skillLevel / 100f) * maxDecreaseFactor;
        }

        // Init skills
        void initSkills()
        {
            SkillManager.Instance.RegisterSkill("farming", "Farming", "Grow and harvest crops");
        }

        // Init zone data
        void initZone(object sender, EventArgs e)
        {
            // Copy the Pickable_SeedCarrot vegetation and create pickable blueberry seeds
            var seedVeg = ZoneSystem.instance.m_vegetation.Find(v => v.m_prefab.name == "Pickable_SeedCarrot");
            seedVeg.m_name = "Pickable_BlueberrySeeds";
            seedVeg.m_prefab = PrefabManager.Instance.GetPrefab("Pickable_BlueberrySeeds");
            ZoneManager.Instance.RegisterVegetation(seedVeg);
        }

        // Init prefabs
        void initPrefabs(object sender, EventArgs e)
        {
            // Blueberry growing
            PrefabManager.Instance.RegisterPrefab(new BlueberrySeedsPrefab());
            PrefabManager.Instance.RegisterPrefab(new PickableBlueberryBushSeedsPrefab());
            PrefabManager.Instance.RegisterPrefab(new BlueberryBushSaplingPrefab());

            // Raspberry growing
            PrefabManager.Instance.RegisterPrefab(new RaspberrySeedsPrefab());
            PrefabManager.Instance.RegisterPrefab(new PickableRaspberryBushSeedsPrefab());
            PrefabManager.Instance.RegisterPrefab(new RaspberryBushSaplingPrefab());

            // Farming station
            PrefabManager.Instance.RegisterPrefab(new FarmingStationPrefab());
        }

        // Init objects
        void initObjects(object sender, EventArgs e)
        {
            // Items
            ObjectManager.Instance.RegisterItem("BlueberrySeeds");
            ObjectManager.Instance.RegisterItem("RaspberrySeeds");

            // Recipes
            ObjectManager.Instance.RegisterRecipe(new Recipe()
            {
                m_item = PrefabManager.Instance.GetPrefab("BlueberrySeeds").GetComponent<ItemDrop>(),
                m_craftingStation = PrefabManager.Instance.GetPrefab("FarmingStation").GetComponent<CraftingStation>(),
                m_resources = new Piece.Requirement[]
                {
                    new Piece.Requirement()
                    {
                        m_resItem = PrefabManager.Instance.GetPrefab("Blueberries").GetComponent<ItemDrop>(),
                        m_amount = 10
                    }
                }
            });

            ObjectManager.Instance.RegisterRecipe(new Recipe()
            {
                m_item = PrefabManager.Instance.GetPrefab("RaspberrySeeds").GetComponent<ItemDrop>(),
                m_craftingStation = PrefabManager.Instance.GetPrefab("FarmingStation").GetComponent<CraftingStation>(),
                m_resources = new Piece.Requirement[]
                {
                    new Piece.Requirement()
                    {
                        m_resItem = PrefabManager.Instance.GetPrefab("Raspberry").GetComponent<ItemDrop>(),
                        m_amount = 10
                    }
                }
            });
        }

        // Init pieces after prefabs are created
        void initPieces(object sender, EventArgs e)
        {
            // Hammer pieces
            PieceManager.Instance.RegisterPiece("Hammer", "FarmingStation");

            // Cultivator pieces
            PieceManager.Instance.RegisterPiece("Cultivator", "Sapling_BlueberryBush");
            PieceManager.Instance.RegisterPiece("Cultivator", "Sapling_RaspberryBush");
        }
    }
}
