using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using BepInEx;
using TestMod.ConsoleCommands;
using JotunnLib.Managers;
using JotunnLib.Utils;
using JotunnLib.Configs;
using JotunnLib.Entities;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace TestMod
{
    [BepInPlugin("com.jotunn.testmod", "JotunnLib Test Mod", "0.1.0")]
    [BepInDependency(JotunnLib.Main.ModGuid)]
    [NetworkCompatibilty(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    class TestMod : BaseUnityPlugin
    {
        public AssetBundle TestAssets;
        public AssetBundle BlueprintRuneBundle;
        public Skills.SkillType TestSkillType = 0;

        private bool showMenu = false;
        private bool showGUIButton = false;
        private Sprite testSkillSprite;
        private GameObject testButton, testPanel;

        // Init handlers
        private void Awake()
        {
            InputManager.Instance.InputRegister += registerInputs;
            LocalizationManager.Instance.LocalizationRegister += registerLocalization;

            loadAssets();
            addItemsWithConfigs();
            addMockedItems();
            addCommands();
            addSkills();
            createConfigValues();

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
                if (testPanel == null)
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
                    testPanel = GUIManager.Instance.CreateWoodpanel(GUIManager.PixelFix.transform,new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0,0), 850, 600);

                    GUIManager.Instance.CreateButton("A Test Button - long dong schlongsen text", testPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(0, 0), 250, 100).SetActive(true);
                    if (testPanel == null)
                    {
                        return;
                    }
                }
                testPanel.SetActive(!testPanel.activeSelf);
                showGUIButton = false;
            }
        }

        // Add custom key bindings
        private void registerInputs(object sender, EventArgs e)
        {
            InputManager.Instance.RegisterButton("TestMod_Menu", KeyCode.Insert);
            InputManager.Instance.RegisterButton("GUIManagerTest", KeyCode.F8);
        }

        // Load assets
        private void loadAssets()
        {
            // Load texture
            Texture2D testSkillTex = AssetUtils.LoadTexture("TestMod/Assets/test_skill.jpg");
            testSkillSprite = Sprite.Create(testSkillTex, new Rect(0f, 0f, testSkillTex.width, testSkillTex.height), Vector2.zero);

            // Load asset bundle from filesystem
            TestAssets = AssetUtils.LoadAssetBundle("TestMod/Assets/jotunnlibtest");
            JotunnLib.Logger.LogInfo(TestAssets);

            // Load asset bundle from filesystem
            BlueprintRuneBundle = AssetUtils.LoadAssetBundle("TestMod/Assets/blueprints");
            JotunnLib.Logger.LogInfo(BlueprintRuneBundle);

            // Embedded Resources
            JotunnLib.Logger.LogInfo($"Embedded resources: {string.Join(",", Assembly.GetExecutingAssembly().GetManifestResourceNames())}");
        }

        // Add new Items with item Configs
        private void addItemsWithConfigs()
        {
            // Add a custom piece table
            PieceManager.Instance.AddPieceTable(BlueprintRuneBundle.LoadAsset<GameObject>("_BlueprintPieceTable"));

            // Create and add a custom item and custom recipe for it
            CustomItem rune = new CustomItem(BlueprintRuneBundle, "BlueprintRune", false);
            CustomRecipe runeRecipe = new CustomRecipe(new RecipeConfig()
            {
                Item = "BlueprintRune",
                Amount = 1,
                Requirements = new PieceRequirementConfig[]
                {
                    new PieceRequirementConfig {Item = "Stone", Amount = 1}
                }
            });
            ItemManager.Instance.AddItem(rune);
            ItemManager.Instance.AddRecipe(runeRecipe);

            // Create and add custom pieces
            GameObject makebp_prefab = BlueprintRuneBundle.LoadAsset<GameObject>("make_blueprint");
            CustomPiece makebp = new CustomPiece(makebp_prefab, new PieceConfig
            {
                PieceTable = "_BlueprintPieceTable"
            });
            PieceManager.Instance.AddPiece(makebp);
            GameObject placebp_prefab = BlueprintRuneBundle.LoadAsset<GameObject>("piece_blueprint");
            CustomPiece placebp = new CustomPiece(placebp_prefab, new PieceConfig
            {
                PieceTable = "_BlueprintPieceTable",
                AllowedInDungeons = true,
                Requirements = new PieceRequirementConfig[]
                {
                    new PieceRequirementConfig {Item = "Wood", Amount = 2}
                }
            });
            PieceManager.Instance.AddPiece(placebp);

            // Add localizations
            var textAssets = BlueprintRuneBundle.LoadAllAssets<TextAsset>();
            foreach (var textAsset in textAssets)
            {
                var lang = textAsset.name.Replace(".json", null);
                LocalizationManager.Instance.AddJson(lang, textAsset.ToString());
            }

            BlueprintRuneBundle.Unload(false);
        }

        // Add new items with mocked prefabs
        private void addMockedItems()
        {
            // Load assets from resources
            var asset = Assembly.GetExecutingAssembly().GetManifestResourceStream("TestMod.AssetsEmbedded.capeironbackpack");
            if (asset == null) JotunnLib.Logger.LogWarning($"Requested asset stream could not be found.");
            else
            {
                var assetBundle = AssetBundle.LoadFromStream(asset);
                var prefab = assetBundle.LoadAsset<GameObject>("Assets/Evie/CapeIronBackpack.prefab");
                if (!prefab) JotunnLib.Logger.LogWarning($"Failed to load asset from bundle: {assetBundle}");
                else
                {
                    // Create and add a custom item
                    var CI = new CustomItem(prefab, true);
                    ItemManager.Instance.AddItem(CI);

                    // Create and add a custom recipe
                    var recipe = ScriptableObject.CreateInstance<Recipe>();
                    recipe.m_item = prefab.GetComponent<ItemDrop>();
                    recipe.m_craftingStation = Mock<CraftingStation>.Create("piece_workbench");
                    var ingredients = new List<Piece.Requirement>
                    {
                        MockRequirement.Create("LeatherScraps", 10),
                        MockRequirement.Create("DeerHide", 2),
                        MockRequirement.Create("Iron", 4),
                    };
                    recipe.m_resources = ingredients.ToArray();
                    var CR = new CustomRecipe(recipe, true, true);
                    ItemManager.Instance.AddRecipe(CR);

                    // Enable BoneReorder
                    BoneReorder.ApplyOnEquipmentChanged();
                }
                assetBundle.Unload(false);
            }
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

        // Registers localizations with configs
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
        private void addCommands()
        {
            CommandManager.Instance.RegisterConsoleCommand(new PrintItemsCommand());
            CommandManager.Instance.RegisterConsoleCommand(new TpCommand());
            CommandManager.Instance.RegisterConsoleCommand(new ListPlayersCommand());
            CommandManager.Instance.RegisterConsoleCommand(new SkinColorCommand());
            CommandManager.Instance.RegisterConsoleCommand(new RaiseSkillCommand());
            CommandManager.Instance.RegisterConsoleCommand(new BetterSpawnCommand());
        }

        // Register new skills
        void addSkills()
        {
            // Test adding a skill with a texture
            Texture2D testSkillTex = AssetUtils.LoadTexture("TestMod/Assets/test_skill.jpg");
            Sprite testSkillSprite = Sprite.Create(testSkillTex, new Rect(0f, 0f, testSkillTex.width, testSkillTex.height), Vector2.zero);
            TestSkillType = SkillManager.Instance.RegisterSkill("com.jotunnlib.testmod.testskill", "TestingSkill", "A nice testing skill!", 1f, testSkillSprite);
        }

        // Create some sample configuration values to check server sync
        private void createConfigValues()
        {
            Config.SaveOnConfigSet = true;

            Config.Bind("JotunnLibTest", "StringValue1", "StringValue", new ConfigDescription("Server side string", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            Config.Bind("JotunnLibTest", "FloatValue1", 750f, new ConfigDescription("Server side float", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            Config.Bind("JotunnLibTest", "IntegerValue1", 200, new ConfigDescription("Server side integer", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            Config.Bind("JotunnLibTest", "BoolValue1", false, new ConfigDescription("Server side bool", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            Config.Bind("JotunnLibTest", "KeycodeValue", KeyCode.F10,
                new ConfigDescription("Server side Keycode", null, new ConfigurationManagerAttributes() { IsAdminOnly = true }));
        }
    }
}
