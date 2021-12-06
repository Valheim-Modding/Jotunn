// JotunnLib
// a Valheim mod
// 
// File:    InGameConfig.cs
// Project: JotunnLib

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
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
        ///     Cached prefab of the vanilla Settings window
        /// </summary>
        private static GameObject SettingsPrefab;

        /// <summary>
        ///     Our own Settings window
        /// </summary>
        private static GameObject SettingsRoot;

        /// <summary>
        ///     Hook into settings setup
        /// </summary>
        [PatchInit(0)]
        public static void HookOnSettings()
        {
            PrefabManager.OnVanillaPrefabsAvailable += PrefabManager_OnVanillaPrefabsAvailable;
            On.FejdStartup.SetupGui += FejdStartup_SetupGui;
            On.Menu.Start += Menu_Start;
        }

        /// <summary>
        ///     Load the mod settings prefab and apply Valheim style to it
        /// </summary>
        private static void PrefabManager_OnVanillaPrefabsAvailable()
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

            GUIManager.Instance.ApplyButtonStyle(settings.CancelButton);
            settings.CancelButton.GetComponentInChildren<Text>().text = LocalizationManager.Instance.TryTranslate(CancelToken);

            GUIManager.Instance.ApplyButtonStyle(settings.OKButton);
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

            PrefabManager.OnVanillaPrefabsAvailable -= PrefabManager_OnVanillaPrefabsAvailable;
        }

        /// <summary>
        ///     Adding a MonoBehaviour to close the mod settings here.
        ///     The Unity project does not know about BepInEx...
        /// </summary>
        private class CloseBehaviour : MonoBehaviour
        {
            private void Awake()
            {
                var settings = GetComponent<ModSettings>();

                settings.CancelButton.onClick.AddListener(() =>
                {
                    try { ColorPicker.Cancel(); } catch (Exception) { }
                    ZInput.instance.Load();
                    Destroy(gameObject);
                });

                settings.OKButton.onClick.AddListener(() =>
                {
                    try { ColorPicker.Done(); } catch (Exception) { }
                    ZInput.instance.Save();
                    SaveConfiguration();
                    Destroy(gameObject);

                });
            }

            private void Update()
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    GetComponent<ModSettings>().CancelButton.onClick.Invoke();
                }
            }
        }

        /// <summary>
        ///     Add default localization and instantiate the mod settings button in Fejd.
        /// </summary>
        private static void FejdStartup_SetupGui(On.FejdStartup.orig_SetupGui orig, FejdStartup self)
        {
            // Fallback english translation
            LocalizationManager.Instance.JotunnLocalization.AddTranslation(MenuToken, "Mod Settings");
            LocalizationManager.Instance.JotunnLocalization.AddTranslation(CancelToken, "Cancel");
            LocalizationManager.Instance.JotunnLocalization.AddTranslation(OKToken, "OK");
            LocalizationManager.Instance.JotunnLocalization.AddTranslation(KeybindToken, "Press a key");

            orig(self);

            try
            {
                Instantiate(self.m_mainMenu.transform.Find("MenuList"));
            }
            catch (Exception ex)
            {
                SettingsRoot = null;
                Logger.LogWarning($"Exception caught while creating the Mod Settings entry: {ex}");
            }
        }

        /// <summary>
        ///     Cache current configuration values for possible sync and instantiate
        ///     the mod settings button on first in-game menu start.
        /// </summary>
        private static void Menu_Start(On.Menu.orig_Start orig, Menu self)
        {
            orig(self);

            try
            {
                SynchronizationManager.Instance.CacheConfigurationValues();
                Instantiate(self.m_menuDialog);
            }
            catch (Exception ex)
            {
                SettingsRoot = null;
                Logger.LogWarning($"Exception caught while creating the Mod Settings entry: {ex}");
            }
        }

        /// <summary>
        ///     Create our own menu list entry when mod config is available
        /// </summary>
        /// <param name="menuList"></param>
        private static void Instantiate(Transform menuList)
        {
            var anyConfig = BepInExUtils.GetDependentPlugins(true).Any(x => GetConfigurationEntries(x.Value).Any());

            if (!anyConfig)
            {
                return;
            }

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
                            modSettingsButton.StartCoroutine(CreateWindow(menuList));
                        }
                        catch (Exception ex)
                        {
                            SettingsRoot = null;
                            Logger.LogWarning($"Exception caught while creating the Mod Settings window: {ex}");
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

            if (Menu.instance)
            {
                Menu.instance.m_settingsInstance = SettingsRoot;
            }

            var settings = SettingsRoot.GetComponent<ModSettings>();

            // Iterate over all dependent plugins (including Jotunn itself)
            foreach (var mod in BepInExUtils.GetDependentPlugins(true).OrderBy(x => x.Value.Info.Metadata.Name))
            {
                if (!GetConfigurationEntries(mod.Value).Any(x => x.Value.IsVisible() && x.Value.IsWritable()))
                {
                    continue;
                }

                yield return null;

                settings.AddPlugin(mod.Key, $"{mod.Value.Info.Metadata.Name} {mod.Value.Info.Metadata.Version}");

                foreach (var kv in GetConfigurationEntries(mod.Value)
                    .Where(x => x.Value.IsVisible() && x.Value.IsWritable())
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

                            if (entry.Value.GetButtonConfig().GamepadConfig != null)
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

            // Scroll back to top
            // scrollView.GetComponentInChildren<ScrollRect>().normalizedPosition = new Vector2(0, 1);

            // Show the window and fake that we are finished loading
            SettingsRoot.SetActive(true);
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

        /// <summary>
        ///     Save our settings
        /// </summary>
        private static void SaveConfiguration()
        {
            var settings = SettingsRoot.GetComponent<ModSettings>();

            // Iterate over all configs
            foreach (var comp in settings.Configs
                         .SelectMany(config => config.GetComponents<MonoBehaviour>()
                         .Where(x => x.GetType().HasImplementedRawGeneric(typeof(ConfigBound<>)))))
            {
                ((IConfigBound)comp).WriteBack();
            }

            // Sync changed config
            SynchronizationManager.Instance.SynchronizeChangedConfig();
        }

        /// <summary>
        ///     Interface for the generic config bind class used in <see cref="SaveConfiguration"/>
        /// </summary>
        internal interface IConfigBound
        {
            public void WriteBack();
        }

        /// <summary>
        ///     Generic abstract version of the config binding class
        /// </summary>
        /// <typeparam name="T"></typeparam>
        internal abstract class ConfigBound<T> : MonoBehaviour, IConfigBound
        {
            public ModSettingConfig Config { get; set; }

            public string ModGUID { get; set; }

            public ConfigEntryBase Entry { get; set; }

            public AcceptableValueBase Clamp { get; set; }

            public ConfigurationManagerAttributes Attributes { get; set; }

            public T Default { get; set; }

            public T Value
            {
                get => GetValue();
                set => SetValue(value);
            }

            public abstract T GetValue();
            public abstract void SetValue(T value);

            public void WriteBack()
            {
                Entry.BoxedValue = Value;
            }

            public void SetData(string modGuid, ConfigEntryBase entry)
            {
                Config = gameObject.GetComponent<ModSettingConfig>();

                ModGUID = modGuid;
                Entry = entry;

                Register();

                Value = (T)Entry.BoxedValue;
                Clamp = Entry.Description.AcceptableValues;
                Attributes =
                    Entry.Description.Tags.FirstOrDefault(x =>
                        x is ConfigurationManagerAttributes) as ConfigurationManagerAttributes;

                if (Attributes != null)
                {
                    SetReadOnly(Attributes.ReadOnly == true);

                    if (Attributes.IsAdminOnly && !Attributes.IsUnlocked)
                    {
                        SetEnabled(false);
                    }
                    else
                    {
                        SetEnabled(true);
                    }

                    Default = (T)Entry.DefaultValue;
                }
            }

            public abstract void Register();

            public abstract void SetEnabled(bool enabled);

            public abstract void SetReadOnly(bool readOnly);

            public void Reset()
            {
                SetValue(Default);
            }

            // Wrap AcceptableValueBase's IsValid
            public bool IsValid()
            {
                if (Clamp != null)
                {
                    return Clamp.IsValid(Value);
                }

                return true;
            }
        }

        /// <summary>
        ///     Boolean Binding
        /// </summary>
        internal class ConfigBoundBoolean : ConfigBound<bool>
        {
            public override void Register()
            {
                Config.Toggle.gameObject.SetActive(true);
            }

            public override bool GetValue()
            {
                return Config.Toggle.isOn;
            }

            public override void SetValue(bool value)
            {
                Config.Toggle.isOn = value;
            }

            public override void SetEnabled(bool enabled)
            {
                Config.Toggle.enabled = enabled;
            }

            public override void SetReadOnly(bool readOnly)
            {
                Config.Toggle.enabled = !readOnly;
            }
        }

        /// <summary>
        ///     Integer binding
        /// </summary>
        internal class ConfigBoundInt : ConfigBound<int>
        {
            public override void Register()
            {
                Config.InputField.gameObject.SetActive(true);
                Config.InputField.characterValidation = InputField.CharacterValidation.Integer;

                if (Entry.Description.AcceptableValues is AcceptableValueRange<int> acceptableValueRange)
                {
                    Config.Slider.gameObject.SetActive(true);
                    Config.Slider.minValue = acceptableValueRange.MinValue;
                    Config.Slider.maxValue = acceptableValueRange.MaxValue;
                    Config.Slider.onValueChanged.AddListener(value =>
                        Config.InputField.SetTextWithoutNotify(((int)value)
                            .ToString(CultureInfo.CurrentCulture)));
                    Config.InputField.onValueChanged.AddListener(text =>
                    {
                        if (int.TryParse(text, out var value))
                        {
                            Config.Slider.SetValueWithoutNotify(value);
                        }
                    });
                }
                Config.InputField.onValueChanged.AddListener(x =>
                {
                    Config.InputField.textComponent.color = IsValid() ? Color.white : Color.red;
                });
            }

            public override int GetValue()
            {
                if (!int.TryParse(Config.InputField.text, out var temp))
                {
                    temp = Default;
                }

                return temp;
            }

            public override void SetValue(int value)
            {
                Config.InputField.text = value.ToString();
            }

            public override void SetEnabled(bool enabled)
            {
                Config.InputField.enabled = enabled;
            }

            public override void SetReadOnly(bool readOnly)
            {
                Config.InputField.readOnly = readOnly;
                Config.InputField.textComponent.color = readOnly ? Color.grey : Color.white;
            }
        }

        /// <summary>
        ///     Float binding
        /// </summary>
        internal class ConfigBoundFloat : ConfigBound<float>
        {
            public override void Register()
            {
                Config.InputField.gameObject.SetActive(true);
                Config.InputField.characterValidation = InputField.CharacterValidation.Decimal;

                if (Entry.Description.AcceptableValues is AcceptableValueRange<float> acceptableValueRange)
                {
                    Config.Slider.gameObject.SetActive(true);
                    Config.Slider.minValue = acceptableValueRange.MinValue;
                    Config.Slider.maxValue = acceptableValueRange.MaxValue;
                    var step = Mathf.Clamp(Config.Slider.minValue / Config.Slider.maxValue, 0.1f, 1f);
                    Config.Slider.onValueChanged.AddListener(value =>
                        Config.InputField.SetTextWithoutNotify((Mathf.Round(value / step) * step)
                            .ToString("F3", CultureInfo.CurrentCulture)));
                    Config.InputField.onValueChanged.AddListener(text =>
                    {
                        if (float.TryParse(text, out var value))
                        {
                            Config.Slider.SetValueWithoutNotify(value);
                        }
                    });
                }
                Config.InputField.onValueChanged.AddListener(x =>
                {
                    Config.InputField.textComponent.color = IsValid() ? Color.white : Color.red;
                });
            }

            public override float GetValue()
            {
                if (!float.TryParse(Config.InputField.text, NumberStyles.Number,
                    CultureInfo.CurrentCulture.NumberFormat, out var temp))
                {
                    temp = Default;
                }

                return temp;
            }

            public override void SetValue(float value)
            {
                Config.InputField.text = value.ToString("F3");
            }

            public override void SetEnabled(bool enabled)
            {
                Config.InputField.enabled = enabled;
            }

            public override void SetReadOnly(bool readOnly)
            {
                Config.InputField.readOnly = readOnly;
                Config.InputField.textComponent.color = readOnly ? Color.grey : Color.white;
            }
        }

        /// <summary>
        ///     Double binding
        /// </summary>
        internal class ConfigBoundDouble : ConfigBound<double>
        {
            public override void Register()
            {
                Config.InputField.gameObject.SetActive(true);
                Config.InputField.characterValidation = InputField.CharacterValidation.Decimal;

                if (Entry.Description.AcceptableValues is AcceptableValueRange<double> acceptableValueRange)
                {
                    Config.Slider.gameObject.SetActive(true);
                    Config.Slider.minValue = (float)acceptableValueRange.MinValue;
                    Config.Slider.maxValue = (float)acceptableValueRange.MaxValue;
                    var step = Mathf.Clamp(Config.Slider.minValue / Config.Slider.maxValue, 0.1f, 1f);
                    Config.Slider.onValueChanged.AddListener(value =>
                        Config.InputField.SetTextWithoutNotify((Mathf.Round(value / step) * step)
                            .ToString("F3", CultureInfo.CurrentCulture)));
                    Config.InputField.onValueChanged.AddListener(text =>
                    {
                        if (double.TryParse(text, out var value))
                        {
                            Config.Slider.SetValueWithoutNotify((float)value);
                        }
                    });
                }
                Config.InputField.onValueChanged.AddListener(x =>
                {
                    Config.InputField.textComponent.color = IsValid() ? Color.white : Color.red;
                });
            }

            public override double GetValue()
            {
                if (!double.TryParse(Config.InputField.text, NumberStyles.Number,
                    CultureInfo.CurrentCulture.NumberFormat, out var temp))
                {
                    temp = Default;
                }

                return temp;
            }

            public override void SetValue(double value)
            {
                Config.InputField.text = value.ToString("F3");
            }

            public override void SetEnabled(bool enabled)
            {
                Config.InputField.enabled = enabled;
            }

            public override void SetReadOnly(bool readOnly)
            {
                Config.InputField.readOnly = readOnly;
                Config.InputField.textComponent.color = readOnly ? Color.grey : Color.white;
            }
        }

        /// <summary>
        ///     String binding
        /// </summary>
        internal class ConfigBoundString : ConfigBound<string>
        {
            public override void Register()
            {
                Config.InputField.gameObject.SetActive(true);
                Config.InputField.characterValidation = InputField.CharacterValidation.None;
                Config.InputField.contentType = InputField.ContentType.Standard;
            }

            public override string GetValue()
            {
                return Config.InputField.text;
            }

            public override void SetValue(string value)
            {
                Config.InputField.text = value;
            }

            public override void SetEnabled(bool enabled)
            {
                Config.InputField.enabled = enabled;
            }

            public override void SetReadOnly(bool readOnly)
            {
                Config.InputField.readOnly = readOnly;
                Config.InputField.textComponent.color = readOnly ? Color.grey : Color.white;
            }
        }

        /// <summary>
        ///     KeyCode binding
        /// </summary>
        internal class ConfigBoundKeyCode : ConfigBound<KeyCode>
        {
            private Text Text;

            public override void Register()
            {
                Config.Button.gameObject.SetActive(true);
                Text = Config.Button.transform.Find("Text").GetComponent<Text>();
            }

            public override KeyCode GetValue()
            {
                if (Enum.TryParse(Text.text, out KeyCode temp))
                {
                    return temp;
                }

                Logger.LogError($"Error parsing Keycode {Text.text}");
                return KeyCode.None;
            }

            public override void SetValue(KeyCode value)
            {
                Text.text = value.ToString();
            }

            public void Start()
            {
                var buttonName = Entry.GetBoundButtonName();
                Config.Button.onClick.AddListener(() =>
                {
                    SettingsRoot.GetComponent<ModSettings>().OpenBindDialog(buttonName, KeyBindCheck);
                });
            }

            private bool KeyBindCheck()
            {
                foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
                {
                    if (Input.GetKeyDown(key))
                    {
                        SetValue(key);
                        if (ZInput.m_binding != null)
                        {
                            ZInput.m_binding.m_key = key;
                        }
                        return true;
                    }
                }

                return false;
            }

            public override void SetEnabled(bool enabled)
            {
                Config.Button.enabled = enabled;
            }

            public override void SetReadOnly(bool readOnly)
            {
                Config.Button.enabled &= readOnly;
                Text.color = readOnly ? Color.grey : Color.white;
            }
        }

        /// <summary>
        ///     KeyboardShortcut binding
        /// </summary>
        internal class ConfigBoundKeyboardShortcut : ConfigBound<KeyboardShortcut>
        {
            private static readonly IEnumerable<KeyCode> KeysToCheck = KeyboardShortcut.AllKeyCodes.Except(new[] { KeyCode.Mouse0, KeyCode.None }).ToArray();

            private Text Text;

            public override void Register()
            {
                Config.Button.gameObject.SetActive(true);
                Text = Config.Button.transform.Find("Text").GetComponent<Text>();
            }

            public override KeyboardShortcut GetValue()
            {
                return KeyboardShortcut.Deserialize(Text.text);
            }

            public override void SetValue(KeyboardShortcut value)
            {
                Text.text = value.ToString();
            }

            public void Start()
            {
                var buttonName = Entry.GetBoundButtonName();
                Config.Button.onClick.AddListener(() =>
                {
                    SettingsRoot.GetComponent<ModSettings>().OpenBindDialog(buttonName, KeyBindCheck);
                });
            }

            private bool KeyBindCheck()
            {
                foreach (var key in KeysToCheck)
                {
                    if (Input.GetKeyUp(key))
                    {
                        SetValue(new KeyboardShortcut(key, KeysToCheck.Where(Input.GetKey).ToArray()));
                        if (ZInput.m_binding != null)
                        {
                            ZInput.m_binding.m_key = key;
                        }
                        return true;
                    }
                }

                return false;
            }

            public override void SetEnabled(bool enabled)
            {
                Config.Button.enabled = enabled;
            }

            public override void SetReadOnly(bool readOnly)
            {
                Config.Button.enabled &= readOnly;
                Text.color = readOnly ? Color.grey : Color.white;
            }
        }

        /// <summary>
        ///     GamepadButton binding
        /// </summary>
        internal class ConfigBoundGamepadButton : ConfigBound<InputManager.GamepadButton>
        {
            public override void Register()
            {
                Config.Dropdown.gameObject.SetActive(true);
                Config.Dropdown.AddOptions(Enum.GetNames(typeof(InputManager.GamepadButton)).ToList());
            }

            public override InputManager.GamepadButton GetValue()
            {
                if (Enum.TryParse<InputManager.GamepadButton>(Config.Dropdown.options[Config.Dropdown.value].text, out var ret))
                {
                    return ret;
                }

                return InputManager.GamepadButton.None;
            }

            public override void SetValue(InputManager.GamepadButton value)
            {
                Config.Dropdown.value = Config.Dropdown.options
                    .IndexOf(Config.Dropdown.options.FirstOrDefault(x =>
                        x.text.Equals(Enum.GetName(typeof(InputManager.GamepadButton), value))));
                Config.Dropdown.RefreshShownValue();
            }

            public void Start()
            {
                var buttonName = $"Joy!{Entry.GetBoundButtonName()}";
                Config.Dropdown.onValueChanged.AddListener(index =>
                {
                    if (Enum.TryParse<InputManager.GamepadButton>(Config.Dropdown.options[index].text, out var btn) &&
                        ZInput.instance.m_buttons.TryGetValue(buttonName, out var def))
                    {
                        KeyCode keyCode = InputManager.GetGamepadKeyCode(btn);
                        string axis = InputManager.GetGamepadAxis(btn);

                        if (!string.IsNullOrEmpty(axis))
                        {
                            def.m_key = KeyCode.None;
                            bool invert = axis.StartsWith("-");
                            def.m_axis = axis.TrimStart('-');
                            def.m_inverted = invert;
                        }
                        else
                        {
                            def.m_axis = null;
                            def.m_key = keyCode;
                        }
                    }
                });
            }

            public override void SetEnabled(bool enabled)
            {
                Config.Dropdown.enabled = enabled;
            }

            public override void SetReadOnly(bool readOnly)
            {
                Config.Dropdown.enabled = !readOnly;
                Config.Dropdown.itemText.color = readOnly ? Color.grey : Color.white;
            }
        }

        internal class ConfigBoundColor : ConfigBound<Color>
        {
            public override void Register()
            {
                Config.ColorInput.transform.parent.gameObject.SetActive(true);

                Config.ColorInput.onEndEdit.AddListener(SetButtonColor);
                Config.ColorInput.characterValidation = InputField.CharacterValidation.None;
                Config.ColorInput.contentType = InputField.ContentType.Alphanumeric;

                Config.ColorButton.onClick.AddListener(ShowColorPicker);
            }

            public override Color GetValue()
            {
                var col = Config.ColorInput.text;
                try
                {
                    return ColorFromString(col);
                }
                catch (Exception e)
                {
                    Logger.LogWarning(e);
                    Logger.LogWarning($"Using default value ({(Color)Entry.DefaultValue}) instead.");
                    return (Color)Entry.DefaultValue;
                }
            }

            public override void SetValue(Color value)
            {
                Config.ColorInput.text = StringFromColor(value);
                Config.ColorButton.targetGraphic.color = value;
            }

            public override void SetEnabled(bool enabled)
            {
                Config.ColorInput.enabled = enabled;
                Config.ColorButton.enabled = enabled;
                if (enabled)
                {
                    Config.ColorInput.onEndEdit.AddListener(SetButtonColor);
                    Config.ColorButton.onClick.AddListener(ShowColorPicker);
                }
                else
                {
                    Config.ColorInput.onEndEdit.RemoveAllListeners();
                    Config.ColorButton.onClick.RemoveAllListeners();
                }
            }

            public override void SetReadOnly(bool readOnly)
            {
                Config.ColorInput.readOnly = readOnly;
                Config.ColorInput.textComponent.color = readOnly ? Color.grey : Color.white;
                Config.ColorButton.enabled = !readOnly;
            }

            private void SetButtonColor(string value)
            {
                if (string.IsNullOrEmpty(value))
                {
                    return;
                }
                Config.ColorButton.targetGraphic.color = ColorFromString(value);
            }

            private void ShowColorPicker()
            {
                if (!ColorPicker.done)
                {
                    ColorPicker.Cancel();
                }

                GUIManager.Instance.CreateColorPicker(
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    GetValue(), Entry.Definition.Key, SetValue, (c) => Config.ColorButton.targetGraphic.color = c,
                    true);
            }

            private string StringFromColor(Color col)
            {
                var r = (int)(col.r * 255f);
                var g = (int)(col.g * 255f);
                var b = (int)(col.b * 255f);
                var a = (int)(col.a * 255f);

                return $"{r:x2}{g:x2}{b:x2}{a:x2}".ToUpper();
            }

            private Color ColorFromString(string str)
            {
                if (long.TryParse(str.Trim().ToLower(), NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out var fromHex))
                {
                    var r = (int)(fromHex >> 24);
                    var g = (int)(fromHex >> 16 & 0xff);
                    var b = (int)(fromHex >> 8 & 0xff);
                    var a = (int)(fromHex & 0xff);
                    var result = new Color(r / 255f, g / 255f, b / 255f, a / 255f);
                    return result;
                }

                throw new ArgumentException($"'{str}' is no valid color value");
            }
        }

        /// <summary>
        ///     Vector2 binding
        /// </summary>
        internal class ConfigBoundVector2 : ConfigBound<Vector2>
        {
            public override void Register()
            {
                Config.Vector2InputX.transform.parent.gameObject.SetActive(true);
            }

            public override Vector2 GetValue()
            {
                if (!(float.TryParse(Config.Vector2InputX.text, NumberStyles.Number,
                        CultureInfo.CurrentCulture.NumberFormat, out var tempX) &&
                      float.TryParse(Config.Vector2InputY.text, NumberStyles.Number,
                        CultureInfo.CurrentCulture.NumberFormat, out var tempY)))
                {
                    return Default;
                }

                return new Vector2(tempX, tempY);
            }

            public override void SetValue(Vector2 value)
            {
                Config.Vector2InputX.text = value.x.ToString("F1");
                Config.Vector2InputY.text = value.y.ToString("F1");
            }

            public override void SetEnabled(bool enabled)
            {
                Config.Vector2InputX.enabled = enabled;
                Config.Vector2InputY.enabled = enabled;
            }

            public override void SetReadOnly(bool readOnly)
            {
                Config.Vector2InputX.readOnly = readOnly;
                Config.Vector2InputX.textComponent.color = readOnly ? Color.grey : Color.white;
                Config.Vector2InputY.readOnly = readOnly;
                Config.Vector2InputY.textComponent.color = readOnly ? Color.grey : Color.white;
            }
        }

        /// <summary>
        ///     GamepadButton binding
        /// </summary>
        internal class ConfigBoundEnum : ConfigBound<Enum>
        {
            public override void Register()
            {
                Config.Dropdown.gameObject.SetActive(true);
                Config.Dropdown.AddOptions(Enum.GetNames(Entry.SettingType).ToList());
            }

            public override Enum GetValue()
            {
                return (Enum)Enum.Parse(Entry.SettingType, Config.Dropdown.options[Config.Dropdown.value].text);
            }

            public override void SetValue(Enum value)
            {
                Config.Dropdown.value = Config.Dropdown.options
                    .IndexOf(Config.Dropdown.options.FirstOrDefault(x =>
                        x.text.Equals(Enum.GetName(Entry.SettingType, value))));
                Config.Dropdown.RefreshShownValue();
            }

            public override void SetEnabled(bool enabled)
            {
                Config.Dropdown.enabled = enabled;
            }

            public override void SetReadOnly(bool readOnly)
            {
                Config.Dropdown.enabled = !readOnly;
                Config.Dropdown.itemText.color = readOnly ? Color.grey : Color.white;
            }
        }
    }
}
