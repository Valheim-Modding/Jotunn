// JotunnLib
// a Valheim mod
// 
// File:    InGameConfig.cs
// Project: JotunnLib

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Jotunn.GUI
{
    /// <summary>
    ///     An ingame GUI for BepInEx config files
    /// </summary>
    internal static class InGameConfig
    {
        /// <summary>
        ///     Name of the menu entry
        /// </summary>
        private const string MenuToken = "$jotunn_modsettings";

        /// <summary>
        ///     Text of the Cancel button
        /// </summary>
        private const string CancelToken = "$jotunn_modsettings_cancel";

        /// <summary>
        ///     Text of the OK button
        /// </summary>
        private const string OKToken = "$jotunn_modsettings_ok";

        /// <summary>
        ///     Text of the keybind dialogue
        /// </summary>
        private const string KeybindToken = "$jotunn_keybind";

        /// <summary>
        ///     Mod settings Prefab
        /// </summary>
        private static GameObject SettingsPrefab;

        /// <summary>
        ///     Current mod settings instance
        /// </summary>
        internal static GameObject SettingsRoot;

        /// <summary>
        ///     Load and init hooks on client instances
        /// </summary>
        public static void Init()
        {
            // Dont init on a headless server
            if (GUIManager.IsHeadless())
            {
                return;
            }

            // Dont init when mod settings are disabled in the config
            if (!Main.ModSettingsEnabledConfig.Value)
            {
                return;
            }

            LoadDefaultLocalization();
            GUIManager.OnCustomGUIAvailable += LoadModSettingsPrefab;
            Main.Harmony.PatchAll(typeof(InGameConfig));
        }

        /// <summary>
        ///     Register Jötunn's default localization
        /// </summary>
        private static void LoadDefaultLocalization()
        {
            LocalizationManager.Instance.JotunnLocalization.AddTranslation(MenuToken, "Mod Settings");
            LocalizationManager.Instance.JotunnLocalization.AddTranslation(CancelToken, "Cancel");
            LocalizationManager.Instance.JotunnLocalization.AddTranslation(OKToken, "OK");
            LocalizationManager.Instance.JotunnLocalization.AddTranslation(KeybindToken, "Press a key");
        }

        /// <summary>
        ///     Load the mod settings prefab and apply Valheim style to it
        /// </summary>
        private static void LoadModSettingsPrefab()
        {
            AssetBundle bundle = AssetUtils.LoadAssetBundleFromResources("modsettings", typeof(Main).Assembly);
            SettingsPrefab = bundle.LoadAsset<GameObject>("ModSettings");
            PrefabManager.Instance.AddPrefab(SettingsPrefab, Main.Instance.Info.Metadata);
            bundle.Unload(false);

            SettingsPrefab.AddComponent<CloseBehaviour>();

            var settings = SettingsPrefab.GetComponent<ModSettings>();
            settings.Panel.sprite = GUIManager.Instance.GetSprite("woodpanel_settings");
            settings.Panel.type = Image.Type.Sliced;
            settings.Panel.material = PrefabManager.Cache.GetPrefab<Material>("litpanel");

            GUIManager.Instance.ApplyTextStyle(settings.Header, GUIManager.Instance.AveriaSerifBold, GUIManager.Instance.ValheimOrange, 32);
            settings.Header.text = LocalizationManager.Instance.TryTranslate(MenuToken);

            GUIManager.Instance.ApplyButtonStyle(settings.CurrentPluginButton);
            var currentPluginButtonImage = settings.CurrentPluginButton.GetComponent<Image>();
            currentPluginButtonImage.sprite = GUIManager.Instance.GetSprite("crafting_panel_bkg");
            currentPluginButtonImage.type = Image.Type.Sliced;
            currentPluginButtonImage.material = new Material(PrefabManager.Cache.GetPrefab<Material>("litpanel"));
            currentPluginButtonImage.material.SetFloat("_Brightness", 1f);
            settings.CurrentPluginButton.GetComponentInChildren<Text>(true).fontSize = 20;

            GUIManager.Instance.ApplyScrollRectStyle(settings.ScrollRect);
            settings.ScrollRect.GetComponent<Image>().sprite = GUIManager.Instance.GetSprite("panel_interior_bkg_128");

            GUIManager.Instance.ApplyButtonStyle(settings.CancelButton, 20);
            settings.CancelButton.GetComponentInChildren<Text>().text = LocalizationManager.Instance.TryTranslate(CancelToken);

            GUIManager.Instance.ApplyButtonStyle(settings.OKButton, 20);
            settings.OKButton.GetComponentInChildren<Text>().text = LocalizationManager.Instance.TryTranslate(OKToken);

            var keybindPanel = settings.BindDialog.GetComponentInChildren<Image>(true);
            keybindPanel.sprite = GUIManager.Instance.GetSprite("woodpanel_password");
            keybindPanel.type = Image.Type.Sliced;
            keybindPanel.material = PrefabManager.Cache.GetPrefab<Material>("litpanel");

            var keybindText = settings.BindDialog.GetComponentInChildren<Text>(true);
            GUIManager.Instance.ApplyTextStyle(keybindText, GUIManager.Instance.AveriaSerifBold, GUIManager.Instance.ValheimOrange, 20);
            keybindText.text = LocalizationManager.Instance.TryTranslate(KeybindToken);

            var plugin = settings.PluginPrefab.GetComponent<ModSettingPlugin>();
            GUIManager.Instance.ApplyButtonStyle(plugin.Button);
            var pluginButtonImage = plugin.Button.GetComponent<Image>();
            pluginButtonImage.sprite = GUIManager.Instance.GetSprite("crafting_panel_bkg");
            pluginButtonImage.type = Image.Type.Sliced;
            pluginButtonImage.material = new Material(PrefabManager.Cache.GetPrefab<Material>("litpanel"));
            pluginButtonImage.material.SetFloat("_Brightness", 1f);
            plugin.Text.fontSize = 20;

            var section = settings.SectionPrefab.GetComponent<Text>();
            section.font = GUIManager.Instance.AveriaSerifBold;

            var config = settings.ConfigPrefab.GetComponent<ModSettingConfig>();
            config.Header.font = GUIManager.Instance.AveriaSerifBold;
            config.Description.font = GUIManager.Instance.AveriaSerifBold;
            GUIManager.Instance.ApplyButtonStyle(config.Button, 14);
            config.Button.GetComponent<Image>().sprite = GUIManager.Instance.GetSprite("text_field");
            config.Button.GetComponentInChildren<Text>(true).color = Color.white;
            GUIManager.Instance.ApplyInputFieldStyle(config.InputField, 14);
            GUIManager.Instance.ApplyToogleStyle(config.Toggle);
            GUIManager.Instance.ApplyDropdownStyle(config.Dropdown, 14);
            config.Dropdown.ClearOptions();
            GUIManager.Instance.ApplySliderStyle(config.Slider, new Vector2(15f, -10f));
            GUIManager.Instance.ApplyInputFieldStyle(config.ColorInput, 14);
            GUIManager.Instance.ApplyButtonStyle(config.ColorButton);
            var vector2 = config.Vector2InputX.transform.parent.gameObject;
            foreach (var txt in vector2.GetComponentsInChildren<Text>(true))
            {
                GUIManager.Instance.ApplyTextStyle(txt, 14);
            }
            foreach (var inp in vector2.GetComponentsInChildren<InputField>(true))
            {
                GUIManager.Instance.ApplyInputFieldStyle(inp, 14);
            }

            GUIManager.OnCustomGUIAvailable -= LoadModSettingsPrefab;
        }

        /// <summary>
        ///     Adding a MonoBehaviour to close the mod settings here.
        ///     The Unity project does not know about BepInEx...
        /// </summary>
        private class CloseBehaviour : MonoBehaviour
        {
            private ModSettings settings;

            private void Awake()
            {
                settings = GetComponent<ModSettings>();

                settings.CancelButton.onClick.AddListener(() =>
                {
                    try { ColorPicker.Cancel(); } catch (Exception) { }
                    ZInput.instance.Load();
                    HideWindow();
                });

                settings.OKButton.onClick.AddListener(() =>
                {
                    try { ColorPicker.Done(); } catch (Exception) { }
                    ZInput.instance.Save();
                    SaveConfiguration();
                    HideWindow();
                });
            }

            private void Update()
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    if (settings.BindWasActive)
                    {
                        settings.BindWasActive = false;
                        return;
                    }
                    settings.CancelButton.onClick.Invoke();
                }
            }
        }

        /// <summary>
        ///     Add default localization and instantiate the mod settings button in Fejd.
        /// </summary>
        [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.SetupGui)), HarmonyPostfix]
        private static void FejdStartup_SetupGui(FejdStartup __instance)
        {
            try
            {
                var menuList = __instance.m_mainMenu.transform.Find("MenuList");
                CreateMenu(menuList);
            }
            catch (Exception ex)
            {
                SettingsRoot = null;
                Logger.LogWarning($"Exception caught while creating the Mod Settings: {ex}");
            }
        }

        /// <summary>
        ///     Cache current configuration values for possible sync and instantiate
        ///     the mod settings button on first in-game menu start.
        /// </summary>
        [HarmonyPatch(typeof(Menu), nameof(Menu.Start)), HarmonyPostfix]
        private static void Menu_Start(Menu __instance)
        {
            try
            {
                CreateMenu(__instance.m_menuDialog);
            }
            catch (Exception ex)
            {
                SettingsRoot = null;
                Logger.LogWarning($"Exception caught while creating the Mod Settings: {ex}");
            }
        }

        /// <summary>
        ///     Create our own menu list entry when mod config is available
        /// </summary>
        /// <param name="menuList"></param>
        private static void CreateMenu(Transform menuList)
        {
            var anyConfig = BepInExUtils.GetPlugins(true).Any(x => GetConfigurationEntries(x.Value).Any());

            if (!anyConfig)
            {
                return;
            }

            Logger.LogDebug("Instantiating Mod Settings");

            var settingsFound = false;
            var mainMenuButtons = new List<Button>();
            for (int i = 0; i < menuList.childCount; i++)
            {
                if (menuList.GetChild(i).gameObject.activeInHierarchy &&
                    menuList.GetChild(i).name != "ModSettings" &&
                    menuList.GetChild(i).TryGetComponent<Button>(out var menuButton))
                {
                    mainMenuButtons.Add(menuButton);
                }

                if (menuList.GetChild(i).name == "Settings")
                {
                    Transform modSettings = Object.Instantiate(menuList.GetChild(i), menuList);
                    modSettings.name = "ModSettings";
                    modSettings.GetComponentInChildren<Text>().text = LocalizationManager.Instance.TryTranslate(MenuToken);
                    Button modSettingsButton = modSettings.GetComponent<Button>();
                    for (int j = 0; j < modSettingsButton.onClick.GetPersistentEventCount(); ++j)
                    {
                        modSettingsButton.onClick.SetPersistentListenerState(j, UnityEventCallState.Off);
                    }
                    modSettingsButton.onClick.RemoveAllListeners();
                    modSettingsButton.onClick.AddListener(() =>
                    {
                        try
                        {
                            Main.Instance.StartCoroutine(CreateWindow(menuList));
                            //ShowWindow();
                        }
                        catch (Exception ex)
                        {
                            SettingsRoot = null;
                            Logger.LogWarning($"Exception caught while showing the Mod Settings window: {ex}");
                        }
                    });
                    mainMenuButtons.Add(modSettingsButton);

                    Transform left = modSettings.Find("LeftKnot");
                    if (left != null)
                    {
                        left.localPosition = new Vector2(left.localPosition.x - 10f, left.localPosition.y);
                    }
                    Transform right = modSettings.Find("RightKnot");
                    if (right != null)
                    {
                        right.localPosition = new Vector2(right.localPosition.x + 10f, right.localPosition.y);
                    }

                    settingsFound = true;
                }
                else if (settingsFound)
                {
                    RectTransform rectTransform = menuList.GetChild(i).GetComponent<RectTransform>();
                    rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x,
                        rectTransform.anchoredPosition.y - 40);
                }
            }

            if (FejdStartup.instance != null)
            {
                FejdStartup.instance.m_menuButtons = mainMenuButtons.ToArray();
            }
        }

        /// <summary>
        ///     Create custom configuration window
        /// </summary>
        private static IEnumerator CreateWindow(Transform menuList)
        {
            // Create settings window
            SettingsRoot = Object.Instantiate(SettingsPrefab, menuList.parent);
            SettingsRoot.SetActive(false);

            var settings = SettingsRoot.GetComponent<ModSettings>();

            // When in game, offset panel
            if (FejdStartup.instance == null)
            {
                var rect = settings.GetComponent<RectTransform>();
                rect.anchoredPosition += new Vector2(0f, 90f);
            }

            // Cache admin config values for sync check when playing as a client
            if (ZNet.instance != null && ZNet.instance.IsClientInstance())
            {
                SynchronizationManager.Instance.CacheConfigurationValues();
            }

            // Iterate over all dependent plugins (including Jotunn itself)
            foreach (var mod in BepInExUtils.GetPlugins(true)
                         .OrderBy(x => x.Value.Info.Metadata.Name))
            {
                if (!GetConfigurationEntries(mod.Value).Any(x => x.Value.IsVisible()))
                {
                    continue;
                }

                try
                {
                    CreatePlugin(settings, mod);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Exception caught while creating mod settings for {mod.Key}: {ex}");
                }

                yield return null;
            }

            if (Menu.instance)
            {
                Menu.instance.m_settingsInstance = SettingsRoot;
            }

            // Actually show the window
            SettingsRoot.SetActive(true);
        }

        /// <summary>
        ///     Create settings for a plugin
        /// </summary>
        private static void CreatePlugin(ModSettings settings, KeyValuePair<string, BaseUnityPlugin> mod)
        {
            settings.AddPlugin(mod.Key, $"{mod.Value.Info.Metadata.Name} {mod.Value.Info.Metadata.Version}");

            foreach (var kv in GetConfigurationEntries(mod.Value)
                         .Where(x => x.Value.IsVisible())
                         .GroupBy(x => x.Key.Section))
            {
                settings.AddSection(mod.Key, kv.Key);

                foreach (var entry in kv.OrderBy(x =>
                {
                    if (x.Value.Description.Tags.FirstOrDefault(y => y is ConfigurationManagerAttributes) is
                        ConfigurationManagerAttributes cma)
                    {
                        return cma.Order ?? int.MaxValue;
                    }

                    return int.MaxValue;
                }).ThenBy(x => x.Key.Key))
                {
                    // Skip actual GamepadConfigs, those are combined with ButtonConfig entries
                    if (entry.Value.SettingType == typeof(InputManager.GamepadButton))
                    {
                        continue;
                    }

                    // Get Attributes or instantiate default
                    var entryAttributes =
                        entry.Value.Description.Tags.FirstOrDefault(x => x is ConfigurationManagerAttributes) as
                            ConfigurationManagerAttributes ?? new ConfigurationManagerAttributes();

                    // Build description
                    var description = entry.Value.Description.Description;

                    var buttonName = entry.Value.GetBoundButtonName();
                    if (!string.IsNullOrEmpty(buttonName))
                    {
                        description += $"{Environment.NewLine}This key is bound to button '{buttonName.Split('!')[0]}'.";
                    }

                    if (entry.Value.Description.AcceptableValues != null)
                    {
                        description += Environment.NewLine + "(" +
                                       entry.Value.Description.AcceptableValues.ToDescriptionString()
                                           .TrimStart('#')
                                           .Trim() + ")";
                    }

                    if (entryAttributes.IsAdminOnly)
                    {
                        description += $"{Environment.NewLine}(Server side setting)";
                    }

                    // Add new Config GO and add config bound component by type
                    if (entry.Value.SettingType == typeof(bool))
                    {
                        var go = settings.AddConfig(mod.Key, $"{entry.Key.Key}:", entryAttributes.EntryColor,
                            description, entryAttributes.DescriptionColor);
                        var conf = go.AddComponent<ConfigBoundBoolean>();
                        conf.SetData(mod.Value.Info.Metadata.GUID, entry.Value);
                    }
                    else if (entry.Value.SettingType == typeof(int))
                    {
                        var go = settings.AddConfig(mod.Key, $"{entry.Key.Key}:", entryAttributes.EntryColor,
                            description, entryAttributes.DescriptionColor);
                        var conf = go.AddComponent<ConfigBoundInt>();
                        conf.SetData(mod.Value.Info.Metadata.GUID, entry.Value);
                    }
                    else if (entry.Value.SettingType == typeof(float))
                    {
                        var go = settings.AddConfig(mod.Key, $"{entry.Key.Key}:", entryAttributes.EntryColor,
                            description, entryAttributes.DescriptionColor);
                        var conf = go.AddComponent<ConfigBoundFloat>();
                        conf.SetData(mod.Value.Info.Metadata.GUID, entry.Value);
                    }
                    else if (entry.Value.SettingType == typeof(double))
                    {
                        var go = settings.AddConfig(mod.Key, $"{entry.Key.Key}:", entryAttributes.EntryColor,
                            description, entryAttributes.DescriptionColor);
                        var conf = go.AddComponent<ConfigBoundDouble>();
                        conf.SetData(mod.Value.Info.Metadata.GUID, entry.Value);
                    }
                    else if (entry.Value.SettingType == typeof(string))
                    {
                        var go = settings.AddConfig(mod.Key, $"{entry.Key.Key}:", entryAttributes.EntryColor,
                            description, entryAttributes.DescriptionColor);
                        var conf = go.AddComponent<ConfigBoundString>();
                        conf.SetData(mod.Value.Info.Metadata.GUID, entry.Value);
                    }
                    else if (entry.Value.SettingType == typeof(KeyCode))
                    {
                        var go = settings.AddConfig(mod.Key, $"{entry.Key.Key}:", entryAttributes.EntryColor,
                            description, entryAttributes.DescriptionColor);
                        var conf = go.AddComponent<ConfigBoundKeyCode>();
                        conf.SetData(mod.Value.Info.Metadata.GUID, entry.Value);

                        if (entry.Value.GetButtonConfig()?.GamepadConfig != null)
                        {
                            var conf2 = go.AddComponent<ConfigBoundGamepadButton>();
                            conf2.SetData(mod.Value.Info.Metadata.GUID,
                                entry.Value.GetButtonConfig().GamepadConfig);
                        }
                    }
                    else if (entry.Value.SettingType == typeof(KeyboardShortcut))
                    {
                        var go = settings.AddConfig(mod.Key, $"{entry.Key.Key}:", entryAttributes.EntryColor,
                            description, entryAttributes.DescriptionColor);
                        var conf = go.AddComponent<ConfigBoundKeyboardShortcut>();
                        conf.SetData(mod.Value.Info.Metadata.GUID, entry.Value);
                    }
                    else if (entry.Value.SettingType == typeof(Color))
                    {
                        var go = settings.AddConfig(mod.Key, $"{entry.Key.Key}:", entryAttributes.EntryColor,
                            description, entryAttributes.DescriptionColor);
                        var conf = go.AddComponent<ConfigBoundColor>();
                        conf.SetData(mod.Value.Info.Metadata.GUID, entry.Value);
                    }
                    else if (entry.Value.SettingType == typeof(Vector2))
                    {
                        var go = settings.AddConfig(mod.Key, $"{entry.Key.Key}:", entryAttributes.EntryColor,
                            description, entryAttributes.DescriptionColor);
                        var conf = go.AddComponent<ConfigBoundVector2>();
                        conf.SetData(mod.Value.Info.Metadata.GUID, entry.Value);
                    }
                    else if (entry.Value.SettingType.IsEnum)
                    {
                        var go = settings.AddConfig(mod.Key, $"{entry.Key.Key}:", entryAttributes.EntryColor,
                            description, entryAttributes.DescriptionColor);
                        var conf = go.AddComponent<ConfigBoundEnum>();
                        conf.SetData(mod.Value.Info.Metadata.GUID, entry.Value);
                    }
                }
            }
        }

        /// <summary>
        ///     Get all config entries of a module by GUID
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        private static IEnumerable<KeyValuePair<ConfigDefinition, ConfigEntryBase>> GetConfigurationEntries(string guid)
        {
            return GetConfigurationEntries(
                BepInExUtils.GetDependentPlugins(true)
                    .FirstOrDefault(x => x.Key == guid).Value);
        }

        /// <summary>
        ///     Get all config entries of a module
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        private static IEnumerable<KeyValuePair<ConfigDefinition, ConfigEntryBase>> GetConfigurationEntries(BaseUnityPlugin module)
        {
            using var enumerator = module.Config.GetEnumerator();
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }

        private static void HideWindow()
        {
            if (Menu.instance)
            {
                Menu.instance.m_settingsInstance = null;
            }

            Object.Destroy(SettingsRoot);
        }

        /// <summary>
        ///     Write all displayed values back to the config files
        /// </summary>
        private static void SaveConfiguration()
        {
            var settings = SettingsRoot.GetComponent<ModSettings>();

            // Iterate over all configs
            foreach (var comp in settings.Configs
                         .SelectMany(config => config.GetComponents<MonoBehaviour>()
                            .Where(x => x.GetType().HasImplementedRawGeneric(typeof(ConfigBound<>)))))
            {
                ((IConfigBound)comp).Write();
            }

            // Sync changed admin config when playing as a client
            if (ZNet.instance != null && ZNet.instance.IsClientInstance())
            {
                SynchronizationManager.Instance.SynchronizeChangedConfig();
            }
        }
    }
}
