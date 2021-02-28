using UnityEngine;
using BepInEx;
using ValheimLokiLoader;
using ValheimLokiLoader.ConsoleCommands;
using ValheimLokiLoader.Managers;
using ValheimLokiLoader.Events;
using System;

namespace TestMod
{
    [BepInPlugin("com.bepinex.plugins.better-farming", "Better Farming", "0.0.1")]
    [BepInDependency("com.bepinex.plugins.loki-loader")]
    class BetterFarming : BaseUnityPlugin
    {
        private Skills.SkillType farmingSkillType;

        void Awake()
        {
            initSkills();

            ObjectManager.ObjectLoad += initObjects;
            PrefabManager.PrefabLoad += initPrefabs;
            PieceManager.PieceLoad += initPieces;
            ZoneManager.ZoneLoad += initZone;

            EventManager.PlayerPlacedPiece += onPlayerPlacedPiece;
        }

        private void onPlayerPlacedPiece(object sender, PlayerPlacedPieceEventArgs e)
        {
            if (e.Piece.gameObject.GetComponent<Plant>())
            {
                Debug.Log("Placed a plant, nice");
                e.Player.RaiseSkill(farmingSkillType);
            }
        }

        // Init skills
        void initSkills()
        {
            farmingSkillType = SkillManager.AddSkill("Farming", "Grow and harvest crops").m_skill;
        }

        // Init zone data
        void initZone(object sender, EventArgs e)
        {
            var veg = ZoneSystem.instance.m_vegetation.Find(v => v.m_prefab.name == "Pickable_SeedCarrot");
            veg.m_name = "pickable_blueberry_seeds";
            veg.m_prefab = PrefabManager.GetPrefab("pickable_blueberry_seeds");
            ZoneManager.AddVegetation(veg);

            // Create vegetation item
            /*
            ZoneManager.AddVegetation(new ZoneSystem.ZoneVegetation()
            {
                m_name = "nice_meme",
                m_min = 50f,
                m_max = 100f,
                m_prefab = PrefabManager.GetPrefab("meme_stone"),
                m_biome = Heightmap.Biome.Meadows
            });
            */
        }

        // Init prefabs
        void initPrefabs(object sender, EventArgs e)
        {
            initBushSeedsPrefab();
            initPickableBushSeedsPrefab();
            initBushPrefab();
        }

        // Init objects
        void initObjects(object sender, EventArgs e)
        {
            // Items
            ObjectManager.AddItem("blueberry_seeds");

            // Recipes
            ObjectManager.AddRecipe(new Recipe()
            {
                m_item = PrefabManager.GetPrefab("blueberry_seeds").GetComponent<ItemDrop>(),
                m_craftingStation = PrefabManager.GetPrefab("piece_cauldron").GetComponent<CraftingStation>(),
                m_resources = new Piece.Requirement[]
                {
                    new Piece.Requirement()
                    {
                        m_resItem = PrefabManager.GetPrefab("Blueberries").GetComponent<ItemDrop>(),
                        m_amount = 10
                    }
                }
            });
        }

        // Init pieces after prefabs are created
        void initPieces(object sender, EventArgs e)
        {
            PieceManager.AddToPieceTable("cultivator", "blueberry_bush_sapling");
        }

        // Create bush seeds prefab that's a copy of carrot seeds
        void initBushSeedsPrefab()
        {
            // Bush seeds prefab
            GameObject bushSeedsPrefab = PrefabManager.CreatePrefab("blueberry_seeds", "CarrotSeeds");

            // Turn all models blue
            foreach (Transform child in bushSeedsPrefab.transform)
            {
                MeshRenderer renderer = child.GetComponent<MeshRenderer>();
                renderer.material.color = new Color(renderer.material.color.r, renderer.material.color.g, 0.9f);
            }

            // Configure item drop
            ItemDrop item = bushSeedsPrefab.GetComponent<ItemDrop>();
            item.m_itemData.m_shared.m_name = "Blueberry Seeds";
            item.m_itemData.m_shared.m_description = "Plant these if you like Blueberries...";
            item.m_itemData.m_dropPrefab = bushSeedsPrefab;
        }

        // Create a pickable bush seed prefab that's a copy of the carrot seeds
        void initPickableBushSeedsPrefab()
        {
            GameObject prefab = PrefabManager.CreatePrefab("pickable_blueberry_seeds", "Pickable_SeedCarrot");
            prefab.transform.localScale = new Vector3(1f, 1.2f, 1f);

            // Turn all models blue
            foreach (Transform child in prefab.transform)
            {
                MeshRenderer renderer = child.GetComponent<MeshRenderer>();
                renderer.material.color = new Color(renderer.material.color.r, renderer.material.color.g, 0.9f);
            }

            Pickable pickable = prefab.GetComponent<Pickable>();
            pickable.m_itemPrefab = PrefabManager.GetPrefab("blueberry_seeds");
        }

        // Create a bush prefab that's a copy of the sapling prefab
        void initBushPrefab()
        {
            GameObject bushPlantPrefab = PrefabManager.CreatePrefab("blueberry_bush_sapling", "sapling_carrot");

            // Turn all models blue
            foreach (Transform child in bushPlantPrefab.transform)
            {
                MeshRenderer renderer = child.GetComponent<MeshRenderer>();
                renderer.material.color = new Color(renderer.material.color.r, renderer.material.color.g, 0.9f);
            }

            // Configure piece
            Piece piece = bushPlantPrefab.GetComponent<Piece>();
            piece.m_name = "Blueberry Bush Sapling";
            piece.m_description = "Plant blueberry seeds to grow a blueberry bush";
            piece.m_icon = PrefabManager.GetPrefab("Blueberries").GetComponent<ItemDrop>().m_itemData.GetIcon();
            piece.m_resources = new Piece.Requirement[] {
                new Piece.Requirement()
                {
                    m_amount = 1,
                    m_resItem = PrefabManager.GetPrefab("blueberry_seeds").GetComponent<ItemDrop>()
                }
            };

            // Configure plant growth
            Plant plant = bushPlantPrefab.GetComponent<Plant>();
            plant.m_name = "Blueberry Bush Sapling";
            plant.m_grownPrefabs = new GameObject[] { PrefabManager.GetPrefab("BlueberryBush") };
            plant.m_growTime = 1f;
            plant.m_growTimeMax = 2f;
        }
    }
}
