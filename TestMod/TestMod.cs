using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using TestMod.ConsoleCommands;
using UnityEngine;
using System.Globalization;

namespace TestMod
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    [BepInDependency(Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Patch)]
    internal class TestMod : BaseUnityPlugin
    {
        private const string ModGUID = "com.jotunn.testmod";
        private const string ModName = "Jotunn Test Mod";
        private const string ModVersion = "0.1.0";
        private const string JotunnTestModConfigSection = "JotunnTest";

        private Sprite testSprite;
        private Texture2D testTex;

        private AssetBundle blueprintRuneBundle;
        private AssetBundle testAssets;
        private AssetBundle steelingot;

        private System.Version currentVersion;

        private bool showButton;
        private bool showMenu;
        private GameObject testPanel;

        private ButtonConfig evilSwordSpecial;
        private CustomStatusEffect evilSwordEffect;

        private Skills.SkillType testSkill;

        private ConfigEntry<bool> EnableVersionMismatch;

        // Load, create and init your custom mod stuff
        private void Awake()
        {
            // Change Logger extra
            Jotunn.Logger.ShowDate = true;
            
            // Create stuff
            CreateConfigValues();
            LoadAssets();
            AddInputs();
            AddLocalizations();
            AddCommands();
            AddSkills();
            AddRecipes();
            AddStatusEffects();
            AddItemConversions();
            AddCustomItemAndConversion();
            AddItemsWithConfigs();
            AddMockedItems();
            AddEmptyItems();
            AddInvalidItems();

            // Add custom items cloned from vanilla items
            ItemManager.OnVanillaItemsAvailable += AddClonedItems;

            // Test config sync event
            SynchronizationManager.OnConfigurationSynchronized += (obj, attr) =>
            {
                if (attr.InitialSynchronization)
                {
                    Jotunn.Logger.LogMessage("Initial Config sync event received");
                }
                else
                {
                    Jotunn.Logger.LogMessage("Config sync event received");
                }
            };

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
                // Check if custom buttons are pressed.
                // Custom buttons need to be added to the InputManager before we can poll them.
                // GetButtonDown will only return true ONCE, right after our button is pressed.
                // If we hold the button down, it won't spam toggle our menu.
                if (ZInput.GetButtonDown("TestMod_Menu"))
                {
                    showMenu = !showMenu;
                }

                if (ZInput.GetButtonDown("TestMod_GUIManagerTest"))
                {
                    showButton = !showButton;
                }

                // Raise the test skill
                if (Player.m_localPlayer != null && ZInput.GetButtonDown("TestMod_RaiseSkill"))
                {
                    Player.m_localPlayer.RaiseSkill(testSkill, 1f);
                }

                // Use the name of the ButtonConfig to identify the button pressed
                if (evilSwordSpecial != null && MessageHud.instance != null)
                {
                    if (ZInput.GetButtonDown(evilSwordSpecial.Name) && MessageHud.instance.m_msgQeue.Count == 0)
                    {
                        MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$evilsword_beevilmessage");
                    }
                }
            }
        }

        // Called every frame for rendering and handling GUI events
        private void OnGUI()
        {
            // Display our GUI if enabled
            if (showMenu)
            {
                GUI.Box(new Rect(40, 40, 150, 250), "TestMod");
            }

            if (showButton)
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
                showButton = false;
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

                    Piece piece = Player.m_localPlayer.m_buildPieces?.GetSelectedPiece();
                    if (piece != null)
                    {
                        bez = bez + ":" + piece.name;
                    }
                }

                GUI.Label(new Rect(10, 10, 500, 25), bez);
            }
        }

        // Create persistent configurations for the mod
        private void CreateConfigValues()
        {
            Config.SaveOnConfigSet = true;

            // Add server config which gets pushed to all clients connecting and can only be edited by admins
            // In local/single player games the player is always considered the admin
            Config.Bind(JotunnTestModConfigSection, "StringValue1", "StringValue",
                new ConfigDescription("Server side string", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            Config.Bind(JotunnTestModConfigSection, "FloatValue1", 750f,
                new ConfigDescription("Server side float", new AcceptableValueRange<float>(500, 1000),
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            Config.Bind(JotunnTestModConfigSection, "IntegerValue1", 200,
                new ConfigDescription("Server side integer", new AcceptableValueRange<int>(5, 25), new ConfigurationManagerAttributes { IsAdminOnly = true }));
            Config.Bind(JotunnTestModConfigSection, "BoolValue1", false,
                new ConfigDescription("Server side bool", null, new ConfigurationManagerAttributes { IsAdminOnly = true, EntryColor = Color.blue, DescriptionColor = Color.yellow }));

            // Add client config to test ModCompatibility
            EnableVersionMismatch = Config.Bind(JotunnTestModConfigSection, nameof(EnableVersionMismatch), false, new ConfigDescription("Enable to test ModCompatibility module"));
            Config.SettingChanged += Config_SettingChanged;

            // Add a client side custom input key for the EvilSword
            Config.Bind(JotunnTestModConfigSection, "EvilSwordSpecialAttack", KeyCode.B, new ConfigDescription("Key to unleash evil with the Evil Sword"));

            Config.Bind(JotunnTestModConfigSection, "Server color", new Color(0f, 1f, 0f, 1f),
                new ConfigDescription("Server side Color", null, new ConfigurationManagerAttributes() {IsAdminOnly = true}));
        }

        // React on changed settings
        private void Config_SettingChanged(object sender, SettingChangedEventArgs e)
        {
            if (e.ChangedSetting.Definition.Section == JotunnTestModConfigSection && e.ChangedSetting.Definition.Key == "EnableVersionMismatch")
            {
                SetVersion();
            }
        }

        // Load assets
        private void LoadAssets()
        {
            // Load texture
            testTex = AssetUtils.LoadTexture("TestMod/Assets/test_tex.jpg");
            testSprite = Sprite.Create(testTex, new Rect(0f, 0f, testTex.width, testTex.height), Vector2.zero);

            // Load asset bundle from filesystem
            testAssets = AssetUtils.LoadAssetBundle("TestMod/Assets/jotunnlibtest");
            Jotunn.Logger.LogInfo(testAssets);

            // Load asset bundle from filesystem
            blueprintRuneBundle = AssetUtils.LoadAssetBundle("TestMod/Assets/testblueprints");
            Jotunn.Logger.LogInfo(blueprintRuneBundle);

            // Load Steel ingot from streamed resource
            steelingot = AssetUtils.LoadAssetBundleFromResources("steel", typeof(TestMod).Assembly);

            // Embedded Resources
            Jotunn.Logger.LogInfo($"Embedded resources: {string.Join(",", typeof(TestMod).Assembly.GetManifestResourceNames())}");
        }

#pragma warning disable CS0618 // Type or member is obsolete
        // Add custom key bindings
        private void AddInputs()
        {
            // Add key bindings on the fly
            InputManager.Instance.AddButton(ModGUID, "TestMod_Menu", KeyCode.Insert);
            InputManager.Instance.AddButton(ModGUID, "TestMod_GUIManagerTest", KeyCode.F8);

            // Add key bindings backed by a config value
            // Create a ButtonConfig to also add it as a custom key hint in AddClonedItem
            evilSwordSpecial = new ButtonConfig
            {
                Name = "EvilSwordSpecialAttack",
                Key = (KeyCode)Config[JotunnTestModConfigSection, "EvilSwordSpecialAttack"].BoxedValue,
                HintToken = "$evilsword_beevil"
            };
            InputManager.Instance.AddButton(ModGUID, evilSwordSpecial);

            // Add a key binding to test skill raising
            InputManager.Instance.AddButton(ModGUID, "TestMod_RaiseSkill", KeyCode.Home);
        }

        // Adds localizations with configs
        private void AddLocalizations()
        {
            // Add translations for the custom item in AddClonedItems
            LocalizationManager.Instance.AddLocalization(new LocalizationConfig("English")
            {
                Translations = {
                    {"item_evilsword", "Sword of Darkness"}, {"item_evilsword_desc", "Bringing the light"},
                    {"evilsword_shwing", "Woooosh"}, {"evilsword_scroll", "*scroll*"},
                    {"evilsword_beevil", "Be evil"}, {"evilsword_beevilmessage", ":reee:"},
                    {"evilsword_effectname", "Evil"}, {"evilsword_effectstart", "You feel evil"},
                    {"evilsword_effectstop", "You feel nice again"}
                }
            });

            // Add translations for the custom piece in AddEmptyItems
            LocalizationManager.Instance.AddLocalization(new LocalizationConfig("English") { Translations = { { "piece_lul", "Lulz" } } });
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
            testSkill = SkillManager.Instance.AddSkill(new SkillConfig()
            {
                Identifier = "com.jotunn.testmod.testskill_code",
                Name = "Testing Skill From Code",
                Description = "A testing skill (but from code)!",
                Icon = testSprite
            });

            // Test adding skills from JSON
            SkillManager.Instance.AddSkillsFromJson("TestMod/Assets/skills.json");
        }

        // Add custom recipes
        private void AddRecipes()
        {
            // Load recipes from JSON file
            ItemManager.Instance.AddRecipesFromJson("TestMod/Assets/recipes.json");
        }

        // Add new status effects
        private void AddStatusEffects()
        {
            // Create a new status effect. The base class "StatusEffect" does not do very much except displaying messages
            // A Status Effect is normally a subclass of StatusEffects which has methods for further coding of the effects (e.g. SE_Stats).
            StatusEffect effect = ScriptableObject.CreateInstance<StatusEffect>();
            effect.name = "EvilStatusEffect";
            effect.m_name = "$evilsword_effectname";
            effect.m_icon = AssetUtils.LoadSpriteFromFile("TestMod/Assets/reee.png");
            effect.m_startMessageType = MessageHud.MessageType.Center;
            effect.m_startMessage = "$evilsword_effectstart";
            effect.m_stopMessageType = MessageHud.MessageType.Center;
            effect.m_stopMessage = "$evilsword_effectstop";

            evilSwordEffect = new CustomStatusEffect(effect, fixReference: false);  // We dont need to fix refs here, because no mocks were used
            ItemManager.Instance.AddStatusEffect(evilSwordEffect);
        }

        // Add item conversions (cooking or smelter recipes)
        private void AddItemConversions()
        {
            // Add an item conversion for the CookingStation. The items must have an "attach" child GameObject to display it on the station.
            var cookConversion = new CustomItemConversion(new CookingConversionConfig
            {
                FromItem = "CookedMeat",
                ToItem = "CookedLoxMeat",
                CookTime = 2f
            });
            ItemManager.Instance.AddItemConversion(cookConversion);

            // Add an item conversion for the Fermenter. You can specify how much new items the conversion yields.
            var fermentConversion = new CustomItemConversion(new FermenterConversionConfig
            {
                FromItem = "Coal",
                ToItem = "CookedLoxMeat",
                ProducedItems = 10
            });
            ItemManager.Instance.AddItemConversion(fermentConversion);

            // Add an item conversion for the smelter
            var smeltConversion = new CustomItemConversion(new SmelterConversionConfig
            {
                //Station = "smelter",  // Use the default from the config
                FromItem = "Stone",
                ToItem = "CookedLoxMeat"
            });
            ItemManager.Instance.AddItemConversion(smeltConversion);

            // Add an item conversion which does not resolve the mock
            var faultConversion = new CustomItemConversion(new SmelterConversionConfig
            {
                //Station = "smelter",  // Use the default from the config
                FromItem = "StonerDude",
                ToItem = "CookedLoxMeat"
            });
            ItemManager.Instance.AddItemConversion(faultConversion);
        }

        // Add custom item conversion (gives a steel ingot to smelter)
        private void AddCustomItemAndConversion()
        {
            var steel_prefab = steelingot.LoadAsset<GameObject>("Steel");
            var ingot = new CustomItem(steel_prefab, fixReference: false);
            var blastConversion = new CustomItemConversion(new SmelterConversionConfig
            {
                Station = "blastfurnace", // Let's specify something other than default here 
                FromItem = "Iron",
                ToItem = "Steel" // This is our custom prefabs name we have loaded just above 
            });
            ItemManager.Instance.AddItem(ingot);
            ItemManager.Instance.AddItemConversion(blastConversion);
        }


        // Add new Items with item Configs
        private void AddItemsWithConfigs()
        {
            // Add a custom piece table
            var table_prefab = blueprintRuneBundle.LoadAsset<GameObject>("_BlueprintTestTable");
            PieceManager.Instance.AddPieceTable(table_prefab);

            // Create and add a custom item
            var rune_prefab = blueprintRuneBundle.LoadAsset<GameObject>("BlueprintTestRune");
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
            var makebp_prefab = blueprintRuneBundle.LoadAsset<GameObject>("make_testblueprint");
            var makebp = new CustomPiece(makebp_prefab,
                new PieceConfig
                {
                    PieceTable = "_BlueprintTestTable"
                });
            PieceManager.Instance.AddPiece(makebp);

            var placebp_prefab = blueprintRuneBundle.LoadAsset<GameObject>("piece_testblueprint");
            var placebp = new CustomPiece(placebp_prefab,
                new PieceConfig
                {
                    PieceTable = "_BlueprintTestTable",
                    AllowedInDungeons = true,
                    Requirements = new[]
                    {
                        new RequirementConfig { Item = "Wood", Amount = 2 }
                    }
                });
            PieceManager.Instance.AddPiece(placebp);

            // Add localizations from the asset bundle
            var textAssets = blueprintRuneBundle.LoadAllAssets<TextAsset>();
            foreach (var textAsset in textAssets)
            {
                var lang = textAsset.name.Replace(".json", null);
                LocalizationManager.Instance.AddJson(lang, textAsset.ToString());
            }

            // Override "default" KeyHint with an empty config
            KeyHintConfig KHC_base = new KeyHintConfig
            {
                Item = "BlueprintTestRune"
            };
            GUIManager.Instance.AddKeyHint(KHC_base);

            // Add custom KeyHints for specific pieces
            KeyHintConfig KHC_make = new KeyHintConfig
            {
                Item = "BlueprintTestRune",
                Piece = "make_testblueprint",
                ButtonConfigs = new[]
                {
                    // Override vanilla "Attack" key text
                    new ButtonConfig { Name = "Attack", HintToken = "$bprune_make" }
                }
            };
            GUIManager.Instance.AddKeyHint(KHC_make);

            KeyHintConfig KHC_piece = new KeyHintConfig
            {
                Item = "BlueprintTestRune",
                Piece = "piece_testblueprint",
                ButtonConfigs = new[]
                {
                    // Override vanilla "Attack" key text
                    new ButtonConfig { Name = "Attack", HintToken = "$bprune_piece" }
                }
            };
            GUIManager.Instance.AddKeyHint(KHC_piece);

            // Add additional localization manually
            LocalizationManager.Instance.AddLocalization(new LocalizationConfig("English")
            {
                Translations = {
                    {"bprune_make", "Capture Blueprint"}, {"bprune_piece", "Place Blueprint"}
                }
            });

            // Don't forget to unload the bundle to free the resources
            blueprintRuneBundle.Unload(false);
        }

        // Add new items with mocked prefabs
        private void AddMockedItems()
        {
            // Load assets from resources
            var assetstream = typeof(TestMod).Assembly.GetManifestResourceStream("TestMod.AssetsEmbedded.capeironbackpack");
            if (assetstream == null)
            {
                Jotunn.Logger.LogWarning("Requested asset stream could not be found.");
            }
            else
            {
                var assetBundle = AssetBundle.LoadFromStream(assetstream);
                var prefab = assetBundle.LoadAsset<GameObject>("Assets/Evie/CapeIronBackpack.prefab");
                if (!prefab)
                {
                    Jotunn.Logger.LogWarning($"Failed to load asset from bundle: {assetBundle}");
                }
                else
                {
                    // Create and add a custom item
                    var CI = new CustomItem(prefab, fixReference: true);  // Mocked refs in prefabs need to be fixed
                    ItemManager.Instance.AddItem(CI);

                    // Create and add a custom recipe
                    var recipe = ScriptableObject.CreateInstance<Recipe>();
                    recipe.name = "Recipe_Backpack";
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
            CustomPiece CP = new CustomPiece("piece_lul", "Hammer");
            if (CP != null)
            {
                var piece = CP.Piece;
                piece.m_icon = testSprite;
                var prefab = CP.PiecePrefab;
                prefab.GetComponent<MeshRenderer>().material.mainTexture = testTex;

                // Test station extension, do it manually cause there is no config on empty pieces atm
                var cfg = new PieceConfig
                {
                    ExtendStation = "piece_workbench"
                };
                cfg.Apply(prefab);
                CP.FixReference = true;

                PieceManager.Instance.AddPiece(CP);
            }
        }

        private void AddInvalidItems()
        {
            CustomItem CI = new CustomItem("item_faulty", false);
            if (CI != null)
            {
                CI.ItemDrop.m_itemData.m_shared.m_icons = new Sprite[]
                {
                    testSprite
                };
                ItemManager.Instance.AddItem(CI);

                CustomRecipe CR = new CustomRecipe(new RecipeConfig
                {
                    Item = "item_faulty",
                    Requirements = new RequirementConfig[]
                    {
                        new RequirementConfig { Item = "NotReallyThereResource", Amount = 99 }
                    }
                });
                ItemManager.Instance.AddRecipe(CR);
            }

            CustomPiece CP = new CustomPiece("piece_fukup", "Hammer");
            if (CP != null)
            {
                var piece = CP.Piece;
                piece.m_icon = testSprite;
                var prefab = CP.PiecePrefab;

                // Test faulty resource, do it manually cause there is no config on empty pieces atm
                var cfg = new PieceConfig
                {
                    Requirements = new RequirementConfig[]
                    {
                        new RequirementConfig { Item = "StillNotThereResource", Amount = 99 }
                    }
                };
                cfg.Apply(prefab);
                CP.FixReference = true;

                PieceManager.Instance.AddPiece(CP);
            }
        }

        // Add new items as copies of vanilla items - just works when vanilla prefabs are already loaded (ObjectDB.CopyOtherDB for example)
        // You can use the Cache of the PrefabManager in here
        private void AddClonedItems()
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

                // Add our custom status effect to it
                itemDrop.m_itemData.m_shared.m_equipStatusEffect = evilSwordEffect.StatusEffect;

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
                        // Override vanilla "Attack" key text
                        new ButtonConfig { Name = "Attack", HintToken = "$evilsword_shwing" },
                        // New custom input
                        evilSwordSpecial,
                        // Override vanilla "Mouse Wheel" text
                        new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$evilsword_scroll" }
                    }
                };
                GUIManager.Instance.AddKeyHint(KHC);
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogError($"Error while adding cloned item: {ex.Message}");
            }
            finally
            {
                // You want that to run only once, Jotunn has the item cached for the game session
                ItemManager.OnVanillaItemsAvailable -= AddClonedItems;
            }
        }

        // Set version of the plugin for the mod compatibility test
        private void SetVersion()
        {
            var propinfo = Info.Metadata.GetType().GetProperty("Version", BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance);

            // Change version number of this module if test is enabled
            if (EnableVersionMismatch.Value)
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
