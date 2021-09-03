using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using BepInEx;
using BepInEx.Configuration;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.GUI;
using Jotunn.Managers;
using Jotunn.Utils;
using TestMod.ConsoleCommands;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TestMod
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    [BepInDependency(Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Patch)]
    internal class TestMod : BaseUnityPlugin
    {
        private const string ModGUID = "com.jotunn.testmod";
        private const string ModName = "Jotunn Test Mod #1";
        private const string ModVersion = "0.1.0";
        private const string JotunnTestModConfigSection = "JotunnTest";

        private Sprite TestSprite;
        private Texture2D TestTex;

        private AssetBundle BlueprintRuneBundle;
        private AssetBundle TestAssets;
        private AssetBundle Steelingot;

        private System.Version CurrentVersion;

        private ButtonConfig CreateColorPickerButton;
        private ButtonConfig CreateGradientPickerButton;
        private ButtonConfig TogglePanelButton;
        private GameObject TestPanel;

        private ButtonConfig RaiseSkillButton;
        private Skills.SkillType TestSkill;

        private ConfigEntry<KeyCode> EvilSwordSpecialConfig;
        private ButtonConfig EvilSwordSpecialButton;
        private CustomStatusEffect EvilSwordEffect;

        private ConfigEntry<bool> EnableVersionMismatch;
        private ConfigEntry<bool> EnableExtVersionMismatch;

        // Load, create and init your custom mod stuff
        private void Awake()
        {
            // Show DateTime on Logs
            //Jotunn.Logger.ShowDate = true;

            // Create stuff
            CreateConfigValues();
            LoadAssets();
            AddInputs();
            AddLocalizations();
            AddCommands();
            AddSkills();
            AddRecipes();
            AddStatusEffects();
            AddVanillaItemConversions();
            AddCustomItemConversion();
            AddItemsWithConfigs();
            AddMockedItems();
            AddKitbashedPieces();
            AddPieceCategories();
            AddInvalidEntities();

            // Add custom items cloned from vanilla items
            ItemManager.OnVanillaItemsAvailable += AddClonedItems;

            // Clone an item with variants and replace them
            ItemManager.OnVanillaItemsAvailable += AddVariants;

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

            // Test admin status sync event
            SynchronizationManager.OnAdminStatusChanged += () =>
            {
                Jotunn.Logger.LogMessage($"Admin status sync event received: {(SynchronizationManager.Instance.PlayerIsAdmin ? "Youre admin now" : "Downvoted, boy")}");
            };


            // Get current version for the mod compatibility test
            CurrentVersion = new System.Version(Info.Metadata.Version.ToString());
            SetVersion();

            // Hook GetVersionString for ext version string compat test
            On.Version.GetVersionString += Version_GetVersionString;
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
                if (ZInput.GetButtonDown(TogglePanelButton.Name))
                {
                    TogglePanel();
                }

                // Show ColorPicker or GradientPicker via GUIManager
                if (ZInput.GetButtonDown(CreateColorPickerButton.Name))
                {
                    CreateColorPicker();
                }
                if (ZInput.GetButtonDown(CreateGradientPickerButton.Name))
                {
                    CreateGradientPicker();
                }

                // Raise the test skill
                if (Player.m_localPlayer != null && ZInput.GetButtonDown(RaiseSkillButton.Name))
                {
                    Player.m_localPlayer.RaiseSkill(TestSkill, 1f);
                }

                // Use the name of the ButtonConfig to identify the button pressed
                if (EvilSwordSpecialButton != null && MessageHud.instance != null)
                {
                    if (ZInput.GetButtonDown(EvilSwordSpecialButton.Name) && MessageHud.instance.m_msgQeue.Count == 0)
                    {
                        MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$evilsword_beevilmessage");
                    }
                }
            }
        }

        // Called every frame for rendering GUI
        private void OnGUI()
        {
            // Displays the current equiped tool/weapon and hover object
            if (Player.m_localPlayer)
            {
                var bez = "Tool: ";

                var item = Player.m_localPlayer.GetInventory().GetEquipedtems().FirstOrDefault(x => x.IsWeapon() || x.m_shared.m_buildPieces != null);
                if (item != null)
                {
                    if (item.m_dropPrefab)
                    {
                        bez += item.m_dropPrefab.name;
                    }
                    else
                    {
                        bez += item.m_shared.m_name;
                    }

                    Piece piece = Player.m_localPlayer.m_buildPieces?.GetSelectedPiece();
                    if (piece != null)
                    {
                        bez += ":" + piece.name;
                    }
                }

                bez += " | Hover: ";

                var hover = Player.m_localPlayer.GetHoverObject();
                if (hover && hover.name != null)
                {
                    bez += hover.name;
                }

                GUI.Label(new Rect(10, 10, 500, 25), bez);
            }
        }

        // Toggle our test panel with button
        private void TogglePanel()
        {
            // Create the panel if it does not exist
            if (!TestPanel)
            {
                if (GUIManager.Instance == null)
                {
                    Logger.LogError("GUIManager instance is null");
                    return;
                }

                if (!GUIManager.CustomGUIFront)
                {
                    Logger.LogError("GUIManager CustomGUI is null");
                    return;
                }

                // Create the panel object
                TestPanel = GUIManager.Instance.CreateWoodpanel(
                    parent: GUIManager.CustomGUIFront.transform,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(0, 0),
                    width: 850,
                    height: 600,
                    draggable: false);
                TestPanel.SetActive(false);

                // Add the Jötunn draggable Component to the panel
                // Note: This is normally automatically added when using CreateWoodpanel()
                DragWindowCntrl.ApplyDragWindowCntrl(TestPanel);

                // Create the text object
                GUIManager.Instance.CreateText(
                    text: "Jötunn, the Valheim Lib",
                    parent: TestPanel.transform,
                    anchorMin: new Vector2(0.5f, 1f),
                    anchorMax: new Vector2(0.5f, 1f),
                    position: new Vector2(0f, -50f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 30,
                    color: GUIManager.Instance.ValheimOrange,
                    outline: true,
                    outlineColor: Color.black,
                    width: 350f,
                    height: 40f,
                    addContentSizeFitter: false);

                // Create the button object
                GameObject buttonObject = GUIManager.Instance.CreateButton(
                    text: "A Test Button - long dong schlongsen text",
                    parent: TestPanel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(0, -250f),
                    width: 250f,
                    height: 60f);
                buttonObject.SetActive(true);

                // Add a listener to the button to close the panel again
                Button button = buttonObject.GetComponent<Button>();
                button.onClick.AddListener(TogglePanel);
                
                // Create a dropdown
                var dd = GUIManager.Instance.CreateDropDown(
                    parent: TestPanel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(200f, -250f),
                    width: 100f,
                    height: 30f);
                dd.GetComponent<Dropdown>().ClearOptions();
                dd.GetComponent<Dropdown>().AddOptions(new List<string>
                {
                    "bla", "blubb", "börks", "blarp", "harhar"
                });

                // Create PluginInfo
                GameObject scrollView = GUIManager.Instance.CreateScrollView(
                    TestPanel.transform,
                    false, true, 10f, 5f,
                    GUIManager.Instance.ValheimScrollbarHandleColorBlock, Color.black, 
                    700f, 400f);
                
                RectTransform viewport =
                    scrollView.transform.Find("Scroll View/Viewport/Content") as RectTransform;

                foreach (var mod in ModRegistry.GetMods().OrderBy(x => x.GUID))
                {
                    // Mod GUID
                    GUIManager.Instance.CreateText(
                        text: mod.GUID,
                        parent: viewport,
                        anchorMin: new Vector2(0.5f, 1f),
                        anchorMax: new Vector2(0.5f, 1f),
                        position: new Vector2(0f, 0f),
                        font: GUIManager.Instance.AveriaSerifBold,
                        fontSize: 30,
                        color: GUIManager.Instance.ValheimOrange,
                        outline: true,
                        outlineColor: Color.black,
                        width: 650f,
                        height: 40f,
                        addContentSizeFitter: false);

                    if (mod.Pieces.Any())
                    {
                        // Pieces title
                        GUIManager.Instance.CreateText(
                            text: "Pieces:",
                            parent: viewport,
                            anchorMin: new Vector2(0.5f, 1f),
                            anchorMax: new Vector2(0.5f, 1f),
                            position: new Vector2(0f, 0f),
                            font: GUIManager.Instance.AveriaSerifBold,
                            fontSize: 20,
                            color: GUIManager.Instance.ValheimOrange,
                            outline: true,
                            outlineColor: Color.black,
                            width: 650f,
                            height: 30f,
                            addContentSizeFitter: false);

                        foreach (var piece in mod.Pieces)
                        {
                            // Piece name
                            GUIManager.Instance.CreateText(
                                text: $"{piece}",
                                parent: viewport,
                                anchorMin: new Vector2(0.5f, 1f),
                                anchorMax: new Vector2(0.5f, 1f),
                                position: new Vector2(0f, 0f),
                                font: GUIManager.Instance.AveriaSerifBold,
                                fontSize: 20,
                                color: Color.white,
                                outline: true,
                                outlineColor: Color.black,
                                width: 650f,
                                height: 30f,
                                addContentSizeFitter: false);
                        }
                    }

                    if (mod.Items.Any())
                    {
                        // Items title
                        GUIManager.Instance.CreateText(
                            text: "Items:",
                            parent: viewport,
                            anchorMin: new Vector2(0.5f, 1f),
                            anchorMax: new Vector2(0.5f, 1f),
                            position: new Vector2(0f, 0f),
                            font: GUIManager.Instance.AveriaSerifBold,
                            fontSize: 20,
                            color: GUIManager.Instance.ValheimOrange,
                            outline: true,
                            outlineColor: Color.black,
                            width: 650f,
                            height: 30f,
                            addContentSizeFitter: false);

                        foreach (var item in mod.Items)
                        {
                            // Piece name
                            GUIManager.Instance.CreateText(
                                text: $"{item}",
                                parent: viewport,
                                anchorMin: new Vector2(0.5f, 1f),
                                anchorMax: new Vector2(0.5f, 1f),
                                position: new Vector2(0f, 0f),
                                font: GUIManager.Instance.AveriaSerifBold,
                                fontSize: 20,
                                color: Color.white,
                                outline: true,
                                outlineColor: Color.black,
                                width: 650f,
                                height: 30f,
                                addContentSizeFitter: false);
                        }
                    }
                }
            }

            // Switch the current state
            bool state = !TestPanel.activeSelf;

            // Set the active state of the panel
            TestPanel.SetActive(state);

            // Toggle input for the player and camera while displaying the GUI
            GUIManager.BlockInput(state);
        }

        // Create a new ColorPicker when hovering a piece
        private void CreateColorPicker()
        {
            if (GUIManager.Instance == null)
            {
                Jotunn.Logger.LogError("GUIManager instance is null");
                return;
            }

            if (SceneManager.GetActiveScene().name == "start")
            {
                GUIManager.Instance.CreateColorPicker(
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0),
                    GUIManager.Instance.ValheimOrange, "Choose your poison", null, null, true);
            }

            if (SceneManager.GetActiveScene().name == "main" && ColorPicker.done)
            {
                var hovered = Player.m_localPlayer.GetHoverObject();
                var current = hovered?.GetComponentInChildren<Renderer>();
                if (current != null)
                {
                    current.gameObject.AddComponent<ColorChanger>();
                }
                else
                {
                    var parent = hovered?.transform.parent.gameObject.GetComponentInChildren<Renderer>();
                    if (parent != null)
                    {
                        parent.gameObject.AddComponent<ColorChanger>();
                    }
                }
            }

        }

        // Create a new GradientPicker
        private void CreateGradientPicker()
        {
            if (GUIManager.Instance == null)
            {
                Jotunn.Logger.LogError("GUIManager instance is null");
                return;
            }

            if (SceneManager.GetActiveScene().name == "start")
            {
                GUIManager.Instance.CreateGradientPicker(
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0),
                    new Gradient(), "Gradiwut?", null, null);
            }

            if (SceneManager.GetActiveScene().name == "main" && GradientPicker.done)
            {
                var hovered = Player.m_localPlayer.GetHoverObject();
                var current = hovered?.GetComponentInChildren<Renderer>();
                if (current != null)
                {
                    current.gameObject.AddComponent<GradientChanger>();
                }
                else
                {
                    var parent = hovered?.transform.parent.gameObject.GetComponentInChildren<Renderer>();
                    if (parent != null)
                    {
                        parent.gameObject.AddComponent<GradientChanger>();
                    }
                }
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

            // Test Color value support
            Config.Bind(JotunnTestModConfigSection, "Server color", new Color(0f, 1f, 0f, 1f),
                new ConfigDescription("Server side Color", null, new ConfigurationManagerAttributes() { IsAdminOnly = true }));

            // Test colored text configs
            Config.Bind(JotunnTestModConfigSection, "BoolValue1", false,
                new ConfigDescription("Server side bool", null, new ConfigurationManagerAttributes { IsAdminOnly = true, EntryColor = Color.blue, DescriptionColor = Color.yellow }));

            // Test invisible configs
            Config.Bind(JotunnTestModConfigSection, "InvisibleInt", 150,
                new ConfigDescription("Invisible int, testing browsable=false", null, new ConfigurationManagerAttributes() { Browsable = false }));

            // Add client config to test ModCompatibility
            EnableVersionMismatch = Config.Bind(JotunnTestModConfigSection, nameof(EnableVersionMismatch), false, new ConfigDescription("Enable to test ModCompatibility module"));
            EnableExtVersionMismatch = Config.Bind(JotunnTestModConfigSection, nameof(EnableExtVersionMismatch), false, new ConfigDescription("Enable to test external version mismatch"));
            Config.SettingChanged += Config_SettingChanged;

            // Add a client side custom input key for the EvilSword
            EvilSwordSpecialConfig = Config.Bind(JotunnTestModConfigSection, "EvilSwordSpecialAttack", KeyCode.B, new ConfigDescription("Key to unleash evil with the Evil Sword"));

            // Test KeyboardShortcut
            Config.Bind<KeyboardShortcut>(JotunnTestModConfigSection, "KeyboardShortcutValue",
                new KeyboardShortcut(KeyCode.A, KeyCode.LeftControl), "Testing how KeyboardShortcut behaves");

        }

        // React on changed settings
        private void Config_SettingChanged(object sender, SettingChangedEventArgs e)
        {
            if (e.ChangedSetting.Definition.Section == JotunnTestModConfigSection && e.ChangedSetting.Definition.Key == nameof(EnableVersionMismatch))
            {
                SetVersion();
            }
        }

        // Load assets
        private void LoadAssets()
        {
            // Load texture
            TestTex = AssetUtils.LoadTexture("TestMod/Assets/test_tex.jpg");
            TestSprite = Sprite.Create(TestTex, new Rect(0f, 0f, TestTex.width, TestTex.height), Vector2.zero);

            // Load asset bundle from filesystem
            TestAssets = AssetUtils.LoadAssetBundle("TestMod/Assets/jotunnlibtest");
            Jotunn.Logger.LogInfo(TestAssets);

            // Load asset bundle from filesystem
            BlueprintRuneBundle = AssetUtils.LoadAssetBundle("TestMod/Assets/testblueprints");
            Jotunn.Logger.LogInfo(BlueprintRuneBundle);

            // Load Steel ingot from streamed resource
            Steelingot = AssetUtils.LoadAssetBundleFromResources("steel", typeof(TestMod).Assembly);

            // Embedded Resources
            Jotunn.Logger.LogInfo($"Embedded resources: {string.Join(",", typeof(TestMod).Assembly.GetManifestResourceNames())}");
        }

        // Add custom key bindings
        private void AddInputs()
        {
            // Add key bindings on the fly
            TogglePanelButton = new ButtonConfig { Name = "TestMod_Menu", Key = KeyCode.Home, ActiveInCustomGUI = true };
            InputManager.Instance.AddButton(ModGUID, TogglePanelButton);

            CreateColorPickerButton = new ButtonConfig { Name = "TestMod_Color", Key = KeyCode.PageUp };
            InputManager.Instance.AddButton(ModGUID, CreateColorPickerButton);
            CreateGradientPickerButton = new ButtonConfig { Name = "TestMod_Gradient", Key = KeyCode.PageDown };
            InputManager.Instance.AddButton(ModGUID, CreateGradientPickerButton);

            // Add key bindings backed by a config value
            // The HintToken is used for the custom KeyHint of the EvilSword
            EvilSwordSpecialButton = new ButtonConfig
            {
                Name = "EvilSwordSpecialAttack",
                Config = EvilSwordSpecialConfig,
                HintToken = "$evilsword_beevil"
            };
            InputManager.Instance.AddButton(ModGUID, EvilSwordSpecialButton);

            // Add a key binding to test skill raising
            RaiseSkillButton = new ButtonConfig { Name = "TestMod_RaiseSkill", Key = KeyCode.Insert, ActiveInGUI = true, ActiveInCustomGUI = true };
            InputManager.Instance.AddButton(ModGUID, RaiseSkillButton);
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

            // Add translations for the custom piece in AddPieceCategories
            LocalizationManager.Instance.AddLocalization(new LocalizationConfig("English")
            {
                Translations = {
                    { "piece_lul", "Lulz" }, { "piece_lul_description", "Do it for them" },
                    { "piece_lel", "Lölz" }, { "piece_lel_description", "Härhärhär" }
                }
            });

            // Add translations for the custom variant in AddClonedItems
            LocalizationManager.Instance.AddLocalization(new LocalizationConfig("English")
            {
                Translations = {
                    { "lulz_shield", "Lulz Shield" }, { "lulz_shield_desc", "Lough at your enemies" }
                }
            });
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
            TestSkill = SkillManager.Instance.AddSkill(new SkillConfig()
            {
                Identifier = "com.jotunn.testmod.testskill_code",
                Name = "Testing Skill From Code",
                Description = "A testing skill (but from code)!",
                Icon = TestSprite
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

            EvilSwordEffect = new CustomStatusEffect(effect, fixReference: false);  // We dont need to fix refs here, because no mocks were used
            ItemManager.Instance.AddStatusEffect(EvilSwordEffect);
        }

        // Add item conversions (cooking or smelter recipes)
        private void AddVanillaItemConversions()
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
        private void AddCustomItemConversion()
        {
            var steel_prefab = Steelingot.LoadAsset<GameObject>("Steel");
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
            // Add a custom piece table with custom categories
            var table_prefab = BlueprintRuneBundle.LoadAsset<GameObject>("_BlueprintTestTable");
            CustomPieceTable rune_table = new CustomPieceTable(table_prefab,
                new PieceTableConfig
                {
                    CanRemovePieces = false,
                    UseCategories = false,
                    UseCustomCategories = true,
                    CustomCategories = new string[]
                    {
                        "Make", "Place"
                    }
                }
            );
            PieceManager.Instance.AddPieceTable(rune_table);

            // Create and add a custom item
            var rune_prefab = BlueprintRuneBundle.LoadAsset<GameObject>("BlueprintTestRune");
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
            var makebp_prefab = BlueprintRuneBundle.LoadAsset<GameObject>("make_testblueprint");
            var makebp = new CustomPiece(makebp_prefab,
                new PieceConfig
                {
                    PieceTable = "_BlueprintTestTable",
                    Category = "Make"
                });
            PieceManager.Instance.AddPiece(makebp);

            var placebp_prefab = BlueprintRuneBundle.LoadAsset<GameObject>("piece_testblueprint");
            var placebp = new CustomPiece(placebp_prefab,
                new PieceConfig
                {
                    PieceTable = "_BlueprintTestTable",
                    Category = "Place",
                    AllowedInDungeons = true,
                    Requirements = new[]
                    {
                        new RequirementConfig { Item = "Wood", Amount = 2 }
                    }
                });
            PieceManager.Instance.AddPiece(placebp);

            // Add localizations from the asset bundle
            var textAssets = BlueprintRuneBundle.LoadAllAssets<TextAsset>();
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
            BlueprintRuneBundle.Unload(false);
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

        // Adds Kitbashed pieces
        private void AddKitbashedPieces()
        {
            // A simple kitbash piece, we will begin with the "empty" prefab as the base
            var simpleKitbashPiece = new CustomPiece("piece_simple_kitbash", true, "Hammer");
            simpleKitbashPiece.FixReference = true;
            simpleKitbashPiece.Piece.m_icon = TestSprite;
            PieceManager.Instance.AddPiece(simpleKitbashPiece);

            // Now apply our Kitbash to the piece
            KitbashManager.Instance.AddKitbash(simpleKitbashPiece.PiecePrefab, new KitbashConfig
            {
                Layer = "piece",
                KitbashSources = new List<KitbashSourceConfig>
                {
                    new KitbashSourceConfig
                    {
                        Name = "eye_1",
                        SourcePrefab = "Ruby",
                        SourcePath = "attach/model",
                        Position = new Vector3(0.528f, 0.1613345f, -0.253f),
                        Rotation = Quaternion.Euler(0, 180, 0f),
                        Scale = new Vector3(0.02473f, 0.05063999f, 0.05064f)
                    },
                    new KitbashSourceConfig
                    {
                        Name = "eye_2",
                        SourcePrefab = "Ruby",
                        SourcePath = "attach/model",
                        Position = new Vector3(0.528f, 0.1613345f, 0.253f),
                        Rotation = Quaternion.Euler(0, 180, 0f),
                        Scale = new Vector3(0.02473f, 0.05063999f, 0.05064f)
                    },
                    new KitbashSourceConfig
                    {
                        Name = "mouth",
                        SourcePrefab = "draugr_bow",
                        SourcePath = "attach/bow",
                        Position = new Vector3(0.53336f, -0.315f, -0.001953f),
                        Rotation = Quaternion.Euler(-0.06500001f, -2.213f, -272.086f),
                        Scale = new Vector3(0.41221f, 0.41221f, 0.41221f)
                    }
                }
            });

            // A more complex Kitbash piece, this has a prepared GameObject for Kitbash to build upon
            AssetBundle kitbashAssetBundle = AssetUtils.LoadAssetBundleFromResources("kitbash", typeof(TestMod).Assembly);
            try
            {
                KitbashObject kitbashObject = KitbashManager.Instance.AddKitbash(kitbashAssetBundle.LoadAsset<GameObject>("piece_odin_statue"), new KitbashConfig
                {
                    Layer = "piece",
                    KitbashSources = new List<KitbashSourceConfig>
                    {
                        new KitbashSourceConfig
                        {
                            SourcePrefab = "piece_artisanstation",
                            SourcePath = "ArtisanTable_Destruction/ArtisanTable_Destruction.007_ArtisanTable.019",
                            TargetParentPath = "new",
                            Position = new Vector3(-1.185f, -0.465f, 1.196f),
                            Rotation = Quaternion.Euler(-90f, 0, 0),
                            Scale = Vector3.one,Materials = new string[]{
                                "obsidian_nosnow",
                                "bronze"
                            }
                        },
                        new KitbashSourceConfig
                        {
                            SourcePrefab = "guard_stone",
                            SourcePath = "new/default",
                            TargetParentPath = "new/pivot",
                            Position = new Vector3(0, 0.0591f ,0),
                            Rotation = Quaternion.identity,
                            Scale = Vector3.one * 0.2f,
                            Materials = new string[]{
                                "bronze",
                                "obsidian_nosnow"
                            }
                        },
                    }
                });
                kitbashObject.OnKitbashApplied += () =>
                {
                    // We've added a CapsuleCollider to the skeleton, this is no longer needed
                    Object.Destroy(kitbashObject.Prefab.transform.Find("new/pivot/default").GetComponent<MeshCollider>());
                };
                PieceManager.Instance.AddPiece(new CustomPiece(kitbashObject.Prefab, new PieceConfig
                {
                    PieceTable = "Hammer",
                    Requirements = new RequirementConfig[]
                    {
                        new RequirementConfig { Item = "Obsidian" , Recover = true},
                        new RequirementConfig { Item = "Bronze", Recover = true }
                    }
                }));
            }
            finally
            {
                kitbashAssetBundle.Unload(false);
            }
        }

        // Add custom pieces from an "empty" prefab with new piece categories
        private void AddPieceCategories()
        {
            CustomPiece CP = new CustomPiece("piece_lul", true, new PieceConfig
            {
                Name = "$piece_lul",
                Description = "$piece_lul_description",
                Icon = TestSprite,
                PieceTable = "Hammer",
                ExtendStation = "piece_workbench", // Test station extension
                Category = "Lulzies."  // Test custom category
            });

            if (CP != null)
            {
                var prefab = CP.PiecePrefab;
                prefab.GetComponent<MeshRenderer>().material.mainTexture = TestTex;

                PieceManager.Instance.AddPiece(CP);
            }

            CP = new CustomPiece("piece_lel", true, new PieceConfig
            {
                Name = "$piece_lel",
                Description = "$piece_lel_description",
                Icon = TestSprite,
                PieceTable = "Hammer",
                ExtendStation = "piece_workbench", // Test station extension
                Category = "Lulzies."  // Test custom category
            });

            if (CP != null)
            {
                var prefab = CP.PiecePrefab;
                prefab.GetComponent<MeshRenderer>().material.mainTexture = TestTex;
                prefab.GetComponent<MeshRenderer>().material.color = Color.grey;

                PieceManager.Instance.AddPiece(CP);
            }
        }

        // Add items / pieces with errors on purpose to test error handling
        private void AddInvalidEntities()
        {
            CustomItem CI = new CustomItem("item_faulty", false);
            if (CI != null)
            {
                CI.ItemDrop.m_itemData.m_shared.m_icons = new Sprite[]
                {
                    TestSprite
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

            CustomPiece CP = new CustomPiece("piece_fukup", false, new PieceConfig
            {
                Icon = TestSprite,
                PieceTable = "Hammer",
                Requirements = new RequirementConfig[]
                {
                    new RequirementConfig { Item = "StillNotThereResource", Amount = 99 }
                }
            });

            if (CP != null)
            {
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
                itemDrop.m_itemData.m_shared.m_equipStatusEffect = EvilSwordEffect.StatusEffect;

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
                        EvilSwordSpecialButton,
                        // Override vanilla "Mouse Wheel" text
                        new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$evilsword_scroll" }
                    }
                };
                GUIManager.Instance.AddKeyHint(KHC);
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogError($"Error while adding cloned item: {ex}");
            }
            finally
            {
                // You want that to run only once, Jotunn has the item cached for the game session
                ItemManager.OnVanillaItemsAvailable -= AddClonedItems;
            }
        }

        // Test the variant config for items
        private void AddVariants()
        {
            try
            {
                Sprite var1 = AssetUtils.LoadSpriteFromFile("TestMod/Assets/test_var1.png");
                Sprite var2 = AssetUtils.LoadSpriteFromFile("TestMod/Assets/test_var2.png");
                Texture2D styleTex = AssetUtils.LoadTexture("TestMod/Assets/test_varpaint.png");
                CustomItem CI = new CustomItem("item_lulvariants", "ShieldWood", new ItemConfig
                {
                    Name = "$lulz_shield",
                    Description = "$lulz_shield_desc",
                    Requirements = new RequirementConfig[]
                    {
                        new RequirementConfig{ Item = "Wood", Amount = 1 }
                    },
                    Icons = new Sprite[]
                    {
                        var1, var2
                    },
                    StyleTex = styleTex
                });
                ItemManager.Instance.AddItem(CI);
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogError($"Error while adding variant item: {ex}");
            }
            finally
            {
                // You want that to run only once, Jotunn has the item cached for the game session
                ItemManager.OnVanillaItemsAvailable -= AddVariants;
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
                propinfo.SetValue(Info.Metadata, CurrentVersion, null);
            }
        }

        private string Version_GetVersionString(On.Version.orig_GetVersionString orig)
        {
            if (EnableExtVersionMismatch.Value)
            {
                return "Non.Business.You";
            }
            else
            {
                return orig();
            }

        }
    }
}
