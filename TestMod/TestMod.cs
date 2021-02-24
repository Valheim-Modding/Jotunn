using UnityEngine;
using BepInEx;
using TestMod.ConsoleCommands;
using ValheimLokiLoader;
using ValheimLokiLoader.ConsoleCommands;
using ValheimLokiLoader.Managers;
using System;

namespace TestMod
{
    [BepInPlugin("com.bepinex.plugins.loki-loader.testmod", "Loki Loader Test Mod", "0.0.1")]
    [BepInDependency("com.bepinex.plugins.loki-loader")]
    class Loader : BaseUnityPlugin
    {
        void Awake()
        {
            initCommands();
            // initSkills();

            PrefabManager.PrefabLoad += initPrefabs;
            PieceManager.LoadPieces += initPieces;
        }

        void initCommands()
        {
            CommandManager.AddConsoleCommand(new TestCommand());
            CommandManager.AddConsoleCommand(new TpCommand());
            CommandManager.AddConsoleCommand(new ListPlayersCommand());
            CommandManager.AddConsoleCommand(new SkinColorCommand());
            CommandManager.AddConsoleCommand(new RaiseSkillCommand());
        }

        void initSkills()
        {
            // Test adding a skill
            SkillManager.AddSkill("Farming", "This probably has something to do with farming");
        }

        // Init prefabs
        void initPrefabs(object sender, EventArgs e)
        {
            initBushSeedsPrefab();
            initBushPrefab();
        }

        // Init pieces after prefabs are created
        void initPieces(object sender, EventArgs e)
        {
            PieceManager.AddToPieceTable("_CultivatorPieceTable", "blueberry_bush_sapling");
        }

        // Create bush seeds prefab that's a copy of carrot seeds
        void initBushSeedsPrefab()
        {
            // Base object
            GameObject carrotSeedsPrefab = PrefabManager.GetPrefab("CarrotSeeds");

            // Bush seeds prefab
            GameObject bushSeedsPrefab = UnityEngine.Object.Instantiate(carrotSeedsPrefab);

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

            PrefabManager.AddPrefab(bushSeedsPrefab, "blueberry_seeds");
            ObjectDB.instance.m_items.Add(bushSeedsPrefab);

            Util.InvokePrivate(ObjectDB.instance, "UpdateItemHashes");
        }

        // Create a bush prefab that's a copy of the sapling prefab
        void initBushPrefab()
        {
            // Base objects
            GameObject carrotSaplingPrefab = PrefabManager.GetPrefab("sapling_carrot");
            GameObject grownBushPrefab = PrefabManager.GetPrefab("BlueberryBush");

            // Bush plant prefab
            GameObject bushPlantPrefab = UnityEngine.Object.Instantiate(carrotSaplingPrefab);

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
            plant.m_grownPrefabs = new GameObject[] { grownBushPrefab };
            plant.m_growTime = 1f;
            plant.m_growTimeMax = 2f;

            PrefabManager.AddPrefab(bushPlantPrefab, "blueberry_bush_sapling");
        }
    }
}
