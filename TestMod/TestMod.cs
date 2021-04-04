using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using BepInEx;
using TestMod.ConsoleCommands;
using JotunnLib.Managers;
using JotunnLib.Utils;
using JotunnLib.Configs;
using System.Collections.Generic;
using JotunnLib.Entities;

namespace TestMod
{
    [BepInPlugin("com.bepinex.plugins.jotunnlib.testmod", "JotunnLib Test Mod", "0.1.0")]
    [BepInDependency(JotunnLib.Main.ModGuid)]
    class TestMod : BaseUnityPlugin
    {
        public static AssetBundle Assets;
        public static Skills.SkillType TestSkillType = 0;

        private bool showMenu = false;
        private Sprite testSkillSprite;
        private bool showGUIButton = false;

        private GameObject TestButton;
        private GameObject TestPanel;

        // Init handlers
        private void Awake()
        {
            ItemManager.Instance.OnItemsRegistered += registerObjects;
            //PieceManager.Instance.PieceRegister += registerPieces;
            InputManager.Instance.InputRegister += registerInputs;
            LocalizationManager.Instance.LocalizationRegister += registerLocalization;

            loadAssets();
            registerCommands();
            registerSkills();
        }

        // Called every frame
        private void Update()
        {
            // Since our Update function in our BepInEx mod class will load BEFORE Valheim loads,
            // we need to check that ZInput is ready to use first.
            if (ZInput.instance != null)
            {
                // Check if our button is pressed. This will only return true ONCE, right after our button is pressed.
                // If we hold the button down, it won't spam toggle our menu.
                if (ZInput.GetButtonDown("TestMod_Menu"))
                {
                    showMenu = !showMenu;
                }

                if (ZInput.GetButtonDown("GUIManagerTest"))
                {
                    showGUIButton = !showGUIButton;
                }
            }

        }

        // Display our GUI if enabled
        private void OnGUI()
        {
            if (showMenu)
            {
                GUI.Box(new Rect(40, 40, 150, 250), "TestMod");
            }

            if (showGUIButton)
            {
                if (TestPanel == null)
                {
                    if (GUIManager.Instance == null)
                    {
                        Logger.LogError("GUIManager instance is null");
                        return;
                    }

                    if (GUIManager.PixelFix == null)
                    {
                        Logger.LogError("GUIManager pixelfix is null");
                        return;
                    }
                    TestPanel = GUIManager.Instance.CreateWoodpanel(GUIManager.PixelFix.transform,new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0,0), 850, 600);

                    GUIManager.Instance.CreateButton("ATest Button long long text", TestPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(0, 0), 250, 100).SetActive(true);
                    if (TestPanel == null)
                    {
                        return;
                    }
                }
                TestPanel.SetActive(!TestPanel.activeSelf);
                showGUIButton = false;
            }
        }

        private void registerInputs(object sender, EventArgs e)
        {
            // Init menu toggle key
            InputManager.Instance.RegisterButton("TestMod_Menu", KeyCode.Insert);
            InputManager.Instance.RegisterButton("GUIManagerTest", KeyCode.F8);
        }

        // Load assets from disk
        private void loadAssets()
        {
            // Load texture
            Texture2D testSkillTex = AssetUtils.LoadTexture("TestMod/Assets/test_skill.jpg");
            testSkillSprite = Sprite.Create(testSkillTex, new Rect(0f, 0f, testSkillTex.width, testSkillTex.height), Vector2.zero);

            // Load asset bundle
            Assets = AssetUtils.LoadAssetBundle("TestMod/Assets/jotunnlibtest");
            JotunnLib.Logger.LogInfo(Assets);
            LoadVLAssets();
        }

        // Register new pieces
        private void registerPieces(object sender, EventArgs e)
        {
            //PieceManager.Instance.RegisterPiece("Hammer", "TestCube");
        }

        // Register new items and recipes
        private void registerObjects(object sender, EventArgs e)
        {
            // Register prefabs using PrefabConfig
            /*PrefabManager.Instance.RegisterPrefab(new TestPrefab());
            PrefabManager.Instance.RegisterPrefab(new TestCubePrefab());
            PrefabManager.Instance.RegisterPrefab(new BundlePrefab());*/

            // Register prefabs
            /*var testprefab = new TestPrefab();
            PrefabManager.Instance.AddPrefab(testprefab.Prefab);
            PrefabManager.Instance.AddEmptyPrefab("TestCube");*/

            /*
            // Items
            ObjectManager.Instance.RegisterItem("TestPrefab");

            // Recipes
            ObjectManager.Instance.RegisterRecipe(new RecipeConfig()
            {
                // Name of the recipe (defaults to "Recipe_YourItem" if null)
                Name = "Recipe_TestPrefab",

                // Name of the prefab for the crafted item
                Item = "TestPrefab",

                // Name of the prefab for the crafting station we wish to use
                // Can set this to null or leave out if you want your recipe to be craftable in your inventory
                CraftingStation = "forge",

                // List of requirements to craft your item
                Requirements = new PieceRequirementConfig[]
                {
                    new PieceRequirementConfig()
                    {
                        // Prefab name of requirement
                        Item = "Blueberries",

                        // Amount required
                        Amount = 2
                    },
                    new PieceRequirementConfig()
                    {
                        // Prefab name of requirement
                        Item = "DeerHide",

                        // Amount required
                        Amount = 1
                    }
                }
            });
            */
        }

        // Registers localizations
        void registerLocalization(object sender, EventArgs e)
        {
            LocalizationManager.Instance.RegisterLocalizationConfig(new LocalizationConfig("English")
            {
                Translations =
                {
                    { "test_prefab_name", "Test Prefab" },
                    { "test_prefab_desc", "We're using this as a test" },

                    { "test_cube_name", "Test Cube" },
                    { "test_cube_desc", "A nice test cube!" },
                }
            });
        }

        // Register new console commands
        private void registerCommands()
        {
            CommandManager.Instance.RegisterConsoleCommand(new PrintItemsCommand());
            CommandManager.Instance.RegisterConsoleCommand(new TpCommand());
            CommandManager.Instance.RegisterConsoleCommand(new ListPlayersCommand());
            CommandManager.Instance.RegisterConsoleCommand(new SkinColorCommand());
            CommandManager.Instance.RegisterConsoleCommand(new RaiseSkillCommand());
            CommandManager.Instance.RegisterConsoleCommand(new BetterSpawnCommand());
        }

        // Register new skills
        void registerSkills()
        {
            // Test adding a skill with a texture
            Texture2D testSkillTex = AssetUtils.LoadTexture("TestMod/Assets/test_skill.jpg");
            Sprite testSkillSprite = Sprite.Create(testSkillTex, new Rect(0f, 0f, testSkillTex.width, testSkillTex.height), Vector2.zero);
            TestSkillType = SkillManager.Instance.RegisterSkill("com.jotunnlib.testmod.testskill", "TestingSkill", "A nice testing skill!", 1f, testSkillSprite);
        }

        private void LoadVLAssets()
        {
            JotunnLib.Logger.LogInfo($"Embedded resources: {string.Join(",", Assembly.GetExecutingAssembly().GetManifestResourceNames())}");
            var asset = Assembly.GetExecutingAssembly().GetManifestResourceStream("TestMod.capeironbackpack");
            if (asset == null) JotunnLib.Logger.LogWarning($"Requested asset stream could not be found.");
            else
            {
                var ab = AssetBundle.LoadFromStream(asset);
                var go = ab.LoadAsset<GameObject>("Assets/Evie/CapeIronBackpack.prefab");
                if (!go) JotunnLib.Logger.LogWarning($"Failed to load asset from bundle: {ab}");
                LoadCraftedItem(go, new List<Piece.Requirement>
                {
                    MockRequirement.Create("LeatherScraps", 10),
                    MockRequirement.Create("DeerHide", 2),
                    MockRequirement.Create("Iron", 4),
                });
            }
        }

        private void LoadCraftedItem(GameObject prefab, List<Piece.Requirement> ingredients, string craftingStation = "piece_workbench")
        {
            if (prefab)
            {
                var CI = new CustomItem(prefab, true);
                var recipe = ScriptableObject.CreateInstance<Recipe>();
                recipe.m_item = prefab.GetComponent<ItemDrop>();
                recipe.m_craftingStation = Mock<CraftingStation>.Create(craftingStation);
                recipe.m_resources = ingredients.ToArray();
                var CR = new CustomRecipe(recipe, true, true);
                JotunnLib.Managers.ItemManager.Instance.AddItem(CI);
                JotunnLib.Managers.ItemManager.Instance.AddRecipe(CR);
                JotunnLib.Logger.LogDebug($"Successfully loaded new CraftedItem {prefab.name} for {craftingStation}.");
            }
        }
    }
}
