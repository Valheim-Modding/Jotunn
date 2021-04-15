using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using JotunnLib;
using JotunnLib.Configs;
using JotunnLib.Entities;
using JotunnLib.Managers;
using JotunnLib.Utils;
using TestMod.ConsoleCommands;
using UnityEngine;

namespace TestMod
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    [BepInDependency(Main.ModGuid)]
    [NetworkCompatibilty(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Build)]
    internal class TestMod : BaseUnityPlugin
    {
        private const string ModGUID = "com.jotunn.testmod";
        private const string ModName = "JotunnLib Test Mod";
        private const string ModVersion = "0.1.0";

        public AssetBundle BlueprintRuneBundle;
        private bool clonedItemsAdded;
        private System.Version currentVersion;
        private bool forceVersionMismatch;
        private bool showGUIButton;

        private bool showMenu;
        public AssetBundle TestAssets;
        private GameObject testPanel;
        public Skills.SkillType TestSkillType = 0;
        private Sprite testSprite;
        private Texture2D testTex;

        // Load, create and init your custom mod stuff
        private void Awake()
        {
            InputManager.Instance.InputRegister += registerInputs;

            LoadAssets();
            AddLocalizations();
            AddItemsWithConfigs();
            AddMockedItems();
            AddEmptyItems();
            AddCommands();
            AddSkills();
            CreateConfigValues();


            // Hook ObjectDB.CopyOtherDB to add custom items cloned from vanilla items
            On.ObjectDB.CopyOtherDB += AddClonedItems;

            // Get current version for the mod compatibility test
            currentVersion = new System.Version(Info.Metadata.Version.ToString());
            SetVersion();
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

        private void OnGUI()
        {
            // Display our GUI if enabled
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

                    testPanel = GUIManager.Instance.CreateWoodpanel(GUIManager.PixelFix.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(0, 0), 850, 600);

                    GUIManager.Instance.CreateButton("A Test Button - long dong schlongsen text", testPanel.transform, new Vector2(0.5f, 0.5f),
                        new Vector2(0.5f, 0.5f), new Vector2(0, 0), 250, 100).SetActive(true);
                    if (testPanel == null)
                    {
                        return;
                    }
                }

                testPanel.SetActive(!testPanel.activeSelf);
                showGUIButton = false;
            }

            // Displays the current equiped tool/weapon
            if (Player.m_localPlayer)
            {
                var bez = "nothing";

                var item = Player.m_localPlayer.GetInventory().GetEquipedtems().FirstOrDefault(x => x.IsWeapon() || x.m_shared.m_buildPieces != null);
                if (item != null)
                {
                    if (item.m_dropPrefab)
                    {
                        bez = item.m_dropPrefab.name;
                    }
                    else
                    {
                        bez = item.m_shared.m_name;
                    }
                }

                GUI.Label(new Rect(10, 10, 100, 25), bez);
            }
        }

        // Add custom key bindings
        private void registerInputs(object sender, EventArgs e)
        {
            InputManager.Instance.AddButton(ModGUID, "TestMod_Menu", KeyCode.Insert);
            InputManager.Instance.AddButton(ModGUID, "GUIManagerTest", KeyCode.F8);
        }

        // Load assets
        private void LoadAssets()
        {
            // Load texture
            testTex = AssetUtils.LoadTexture("TestMod/Assets/test_tex.jpg");
            testSprite = Sprite.Create(testTex, new Rect(0f, 0f, testTex.width, testTex.height), Vector2.zero);

            // Load asset bundle from filesystem
            TestAssets = AssetUtils.LoadAssetBundle("TestMod/Assets/jotunnlibtest");
            JotunnLib.Logger.LogInfo(TestAssets);

            // Load asset bundle from filesystem
            BlueprintRuneBundle = AssetUtils.LoadAssetBundle("TestMod/Assets/blueprints");
            JotunnLib.Logger.LogInfo(BlueprintRuneBundle);

            // Embedded Resources
            JotunnLib.Logger.LogInfo($"Embedded resources: {string.Join(",", Assembly.GetExecutingAssembly().GetManifestResourceNames())}");
        }

        // Adds localizations with configs
        private void AddLocalizations()
        {
            // Add translations for the custom item in AddClonedItems
            LocalizationManager.Instance.AddLocalization(new LocalizationConfig("English")
            {
                Translations = {
                    {"item_evilsword", "Sword of Darkness"}, {"item_evilsword_desc", "Bringing the light"},
                    { "evilsword_shwing", "Woooosh" }, {"evilsword_scroll", "*scroll*"}
                }
            });

            // Add translations for the custom piece in AddEmptyItems
            LocalizationManager.Instance.AddLocalization(new LocalizationConfig("English") { Translations = { { "piece_lul", "Lulz" } } });
        }

        // Add new Items with item Configs
        private void AddItemsWithConfigs()
        {
            // Load recipes from JSON file
            ItemManager.Instance.AddRecipesFromJson("TestMod/Assets/recipes.json");

            // Add a custom piece table
            var table_prefab = BlueprintRuneBundle.LoadAsset<GameObject>("_BlueprintPieceTable");
            PieceManager.Instance.AddPieceTable(table_prefab);

            // Create and add a custom item
            var rune_prefab = BlueprintRuneBundle.LoadAsset<GameObject>("BlueprintRune");
            var rune = new CustomItem(rune_prefab, fixReference: false,  // Prefab did not use mocked refs so no need to fix them
                new ItemConfig
                {
                    Amount = 1,
                    Requirements = new[] 
                    { 
                        new RequirementConfig { Item = "Stone", Amount = 1 } 
                    }
                });
            ItemManager.Instance.AddItem(rune);

            // Create and add custom pieces
            var makebp_prefab = BlueprintRuneBundle.LoadAsset<GameObject>("make_blueprint");
            var makebp = new CustomPiece(makebp_prefab, 
                new PieceConfig 
                {
                    PieceTable = "_BlueprintPieceTable"
                });
            PieceManager.Instance.AddPiece(makebp);

            var placebp_prefab = BlueprintRuneBundle.LoadAsset<GameObject>("piece_blueprint");
            var placebp = new CustomPiece(placebp_prefab,
                new PieceConfig
                {
                    PieceTable = "_BlueprintPieceTable",
                    AllowedInDungeons = true,
                    Requirements = new[] 
                    {
                        new RequirementConfig { Item = "Wood", Amount = 2 }
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

            // Don't forget to unload the bundle to free the resources
            BlueprintRuneBundle.Unload(false);
        }

        // Add new items with mocked prefabs
        private void AddMockedItems()
        {
            // Load assets from resources
            var assetstream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TestMod.AssetsEmbedded.capeironbackpack");
            if (assetstream == null)
            {
                JotunnLib.Logger.LogWarning("Requested asset stream could not be found.");
            }
            else
            {
                var assetBundle = AssetBundle.LoadFromStream(assetstream);
                var prefab = assetBundle.LoadAsset<GameObject>("Assets/Evie/CapeIronBackpack.prefab");
                if (!prefab)
                {
                    JotunnLib.Logger.LogWarning($"Failed to load asset from bundle: {assetBundle}");
                }
                else
                {
                    // Create and add a custom item
                    var CI = new CustomItem(prefab, fixReference: true);  // Mocked refs in prefabs need to be fixed
                    ItemManager.Instance.AddItem(CI);

                    // Create and add a custom recipe
                    var recipe = ScriptableObject.CreateInstance<Recipe>();
                    recipe.m_item = prefab.GetComponent<ItemDrop>();
                    recipe.m_craftingStation = Mock<CraftingStation>.Create("piece_workbench");
                    var ingredients = new List<Piece.Requirement>
                    {
                        MockRequirement.Create("LeatherScraps", 10), 
                        MockRequirement.Create("DeerHide", 2), 
                        MockRequirement.Create("Iron", 4)
                    };
                    recipe.m_resources = ingredients.ToArray();
                    var CR = new CustomRecipe(recipe, fixReference: true, fixRequirementReferences: true);  // Mocked main and requirement refs need to be fixed
                    ItemManager.Instance.AddRecipe(CR);

                    // Enable BoneReorder
                    BoneReorder.ApplyOnEquipmentChanged();
                }

                assetBundle.Unload(false);
            }
        }

        // Add a custom item from an "empty" prefab
        private void AddEmptyItems()
        {
            CustomPiece CP = new CustomPiece("$piece_lul", "Hammer");
            if (CP != null)
            {
                var piece = CP.Piece;
                piece.m_icon = testSprite;
                var prefab = CP.PiecePrefab;
                prefab.GetComponent<MeshRenderer>().material.mainTexture = testTex;
                PieceManager.Instance.AddPiece(CP);
            }
        }

        // Add new items as copies of vanilla items - just works when vanilla prefabs are already loaded (ObjectDB.CopyOtherDB for example)
        // You can use the Cache of the PrefabManager in here
        private void AddClonedItems(On.ObjectDB.orig_CopyOtherDB orig, ObjectDB self, ObjectDB other)
        {
            // You want that to run only once, JotunnLib has the item cached for the game session
            if (!clonedItemsAdded)
            {
                try
                {
                    // Create and add a custom item based on SwordBlackmetal
                    var CI = new CustomItem("EvilSword", "SwordBlackmetal");
                    ItemManager.Instance.AddItem(CI);

                    // Replace vanilla properties of the custom item
                    var itemDrop = CI.ItemDrop;
                    itemDrop.m_itemData.m_shared.m_name = "$item_evilsword";
                    itemDrop.m_itemData.m_shared.m_description = "$item_evilsword_desc";

                    // Create and add a recipe for the copied item
                    var recipe = ScriptableObject.CreateInstance<Recipe>();
                    recipe.name = "Recipe_EvilSword";
                    recipe.m_item = itemDrop;
                    recipe.m_craftingStation = PrefabManager.Cache.GetPrefab<CraftingStation>("piece_workbench");
                    recipe.m_resources = new[]
                    {
                        new Piece.Requirement {m_resItem = PrefabManager.Cache.GetPrefab<ItemDrop>("Stone"), m_amount = 1},
                        new Piece.Requirement {m_resItem = PrefabManager.Cache.GetPrefab<ItemDrop>("Wood"), m_amount = 1}
                    };
                    var CR = new CustomRecipe(recipe, fixReference: false, fixRequirementReferences: false);  // no need to fix because the refs from the cache are valid
                    ItemManager.Instance.AddRecipe(CR);

                    // Create custom KeyHints for the item
                    KeyHintConfig KHC = new KeyHintConfig
                    {
                        Item = "EvilSword",
                        ButtonConfigs = new[]
                        {
                            new ButtonConfig { Name = "Shwing", KeyToken = "$KEY_Attack", HintToken = "$evilsword_shwing" },
                            new ButtonConfig { Name = "Scroll", Axis = "Up", HintToken = "$evilsword_scroll" }
                        }
                    };
                    GUIManager.Instance.AddKeyHint(KHC);
                }
                catch (Exception ex)
                {
                    JotunnLib.Logger.LogError($"Error while adding cloned item: {ex.Message}");
                }
                finally
                {
                    clonedItemsAdded = true;
                }
            }

            // Hook is prefix, we just need to be able to get the vanilla prefabs, JotunnLib registers them in ObjectDB
            orig(self, other);
        }

        // Register new console commands
        private void AddCommands()
        {
            CommandManager.Instance.AddConsoleCommand(new PrintItemsCommand());
            CommandManager.Instance.AddConsoleCommand(new TpCommand());
            CommandManager.Instance.AddConsoleCommand(new ListPlayersCommand());
            CommandManager.Instance.AddConsoleCommand(new SkinColorCommand());
            CommandManager.Instance.AddConsoleCommand(new RaiseSkillCommand());
            CommandManager.Instance.AddConsoleCommand(new BetterSpawnCommand());
        }

        // Register new skills
        private void AddSkills()
        {
            // Test adding a skill with a texture
            Texture2D testSkillTex = AssetUtils.LoadTexture("TestMod/Assets/test_tex.jpg");
            Sprite testSkillSprite = Sprite.Create(testSkillTex, new Rect(0f, 0f, testSkillTex.width, testSkillTex.height), Vector2.zero);
            TestSkillType = SkillManager.Instance.AddSkill(new SkillConfig()
            {
                Identifier = "com.jotunn.testmod.testskill_code",
                Name = "Testing Skill From Code",
                Description = "A testing skill (but from code)!",
                Icon = testSkillSprite
            });

            // Test adding skills from JSON
            SkillManager.Instance.AddSkillsFromJson("TestMod/Assets/skills.json");
        }

        // Create some sample configuration values to check server sync
        private void CreateConfigValues()
        {
            Config.SaveOnConfigSet = true;

            Config.Bind("JotunnLibTest", "StringValue1", "StringValue",
                new ConfigDescription("Server side string", null, new ConfigurationManagerAttributes {IsAdminOnly = true}));
            Config.Bind("JotunnLibTest", "FloatValue1", 750f,
                new ConfigDescription("Server side float", new AcceptableValueRange<float>(500, 1000),
                    new ConfigurationManagerAttributes {IsAdminOnly = true}));
            Config.Bind("JotunnLibTest", "IntegerValue1", 200,
                new ConfigDescription("Server side integer", new AcceptableValueRange<int>(5, 25), new ConfigurationManagerAttributes {IsAdminOnly = true}));
            Config.Bind("JotunnLibTest", "BoolValue1", false,
                new ConfigDescription("Server side bool", null, new ConfigurationManagerAttributes {IsAdminOnly = true}));
            Config.Bind("JotunnLibTest", "KeycodeValue", KeyCode.F10,
                new ConfigDescription("Server side Keycode", null, new ConfigurationManagerAttributes {IsAdminOnly = true}));

            // Add client config to test ModCompatibility
            Config.Bind("JotunnLibTest", "EnableVersionMismatch", false, new ConfigDescription("Enable to test ModCompatibility module"));
            forceVersionMismatch = (bool) Config["JotunnLibTest", "EnableVersionMismatch"].BoxedValue;
            Config.SettingChanged += Config_SettingChanged;

            InputManager.Instance.AddButton(ModGUID, "KeycodeValue", (KeyCode) Config["JotunnLibTest", "KeycodeValue"].BoxedValue);
        }

        // React on changed settings
        private void Config_SettingChanged(object sender, SettingChangedEventArgs e)
        {
            if (e.ChangedSetting.Definition.Section == "JotunnLibTest" && e.ChangedSetting.Definition.Key == "EnableVersionMismatch")
            {
                forceVersionMismatch = (bool) e.ChangedSetting.BoxedValue;
                SetVersion();
            }
        }

        // Set version of the plugin for the mod compatibility test
        private void SetVersion()
        {
            var propinfo = Info.Metadata.GetType().GetProperty("Version", BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance);

            // Change version number of this module if test is enabled
            if (forceVersionMismatch)
            {
                var v = new System.Version(0, 0, 0);
                propinfo.SetValue(Info.Metadata, v, null);
            }
            else
            {
                propinfo.SetValue(Info.Metadata, currentVersion, null);
            }
        }
    }
}
