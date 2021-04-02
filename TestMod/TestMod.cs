using System;
using UnityEngine;
using BepInEx;
using TestMod.ConsoleCommands;
using JotunnLib.Entities;
using JotunnLib.Managers;
using JotunnLib.Utils;
using TestMod.Prefabs;

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

        // Init handlers
        private void Awake()
        {
            ObjectManager.Instance.ObjectRegister += registerObjects;
            PrefabManager.Instance.PrefabRegister += registerPrefabs;
            PieceManager.Instance.PieceRegister += registerPieces;
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
            }
        }

        // Display our GUI if enabled
        private void OnGUI()
        {
            if (showMenu)
            {
                GUI.Box(new Rect(40, 40, 150, 250), "TestMod");
            }
        }

        private void registerInputs(object sender, EventArgs e)
        {
            // Init menu toggle key
            InputManager.Instance.RegisterButton("TestMod_Menu", KeyCode.Insert);
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
        }

        // Register new prefabs
        private void registerPrefabs(object sender, EventArgs e)
        {
            // Register prefabs using PrefabConfig
            /*PrefabManager.Instance.RegisterPrefab(new TestPrefab());
            PrefabManager.Instance.RegisterPrefab(new TestCubePrefab());
            PrefabManager.Instance.RegisterPrefab(new BundlePrefab());*/

            // Register prefabs
            var testprefab = new TestPrefab();
            PrefabManager.Instance.AddPrefab(testprefab.Prefab);
            PrefabManager.Instance.AddEmptyPrefab("TestCube");
        }

        // Register new pieces
        private void registerPieces(object sender, EventArgs e)
        {
            PieceManager.Instance.RegisterPiece("Hammer", "TestCube");
        }

        // Register new items and recipes
        private void registerObjects(object sender, EventArgs e)
        {
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
    }
}
