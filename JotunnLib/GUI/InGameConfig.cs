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
        ///     Cached transform of the vanilla menu list
        /// </summary>
        private static Transform MenuList;

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

            var settings = SettingsPrefab.GetComponent<ModSettings>();
            settings.Panel.sprite = GUIManager.Instance.GetSprite("woodpanel_settings");
            settings.Panel.type = Image.Type.Sliced;
            settings.Panel.material = PrefabManager.Cache.GetPrefab<Material>("litpanel");
            settings.Panel.color = Color.white;
            settings.Header.text = LocalizationManager.Instance.TryTranslate(MenuToken);
            GUIManager.Instance.ApplyTextStyle(settings.Header, GUIManager.Instance.AveriaSerifBold, GUIManager.Instance.ValheimOrange, 32);
            GUIManager.Instance.ApplyButtonStyle(settings.CurrentPluginButton);
            settings.CurrentPluginButton.colors = new ColorBlock
            {
                normalColor = new Color(0.824f, 0.824f, 0.824f, 1f),
                highlightedColor = new Color(0.824f, 0.824f, 0.824f, 1f),
                pressedColor = new Color(0.537f, 0.556f, 0.556f, 1f),
                selectedColor = new Color(0.824f, 0.824f, 0.824f, 1f),
                disabledColor = new Color(0.566f, 0.566f, 0.566f, 1f),
                colorMultiplier = 1f,
                fadeDuration = 0.1f
            };
            settings.CurrentPluginButton.GetComponent<Image>().sprite = GUIManager.Instance.GetSprite("panel_bkg_128");
            settings.CurrentPluginButton.GetComponentInChildren<Text>(true).fontSize = 20;
            GUIManager.Instance.ApplyScrollRectStyle(settings.ScrollRect);
            settings.ScrollRect.GetComponent<Image>().sprite = GUIManager.Instance.GetSprite("panel_interior_bkg_128");
            settings.CancelButton.GetComponentInChildren<Text>().text = LocalizationManager.Instance.TryTranslate(CancelToken);
            GUIManager.Instance.ApplyButtonStyle(settings.CancelButton);
            settings.OKButton.GetComponentInChildren<Text>().text = LocalizationManager.Instance.TryTranslate(OKToken);
            GUIManager.Instance.ApplyButtonStyle(settings.OKButton);
            var keybindPanel = settings.BindDialogue.GetComponentInChildren<Image>(true);
            keybindPanel.sprite = GUIManager.Instance.GetSprite("woodpanel_password");
            keybindPanel.type = Image.Type.Sliced;
            keybindPanel.material = PrefabManager.Cache.GetPrefab<Material>("litpanel");
            var keybindText = settings.BindDialogue.GetComponentInChildren<Text>(true);
            GUIManager.Instance.ApplyTextStyle(keybindText, GUIManager.Instance.AveriaSerifBold, GUIManager.Instance.ValheimOrange, 20);
            keybindText.text = LocalizationManager.Instance.TryTranslate(KeybindToken);

            var plugin = settings.PluginPrefab.GetComponent<ModSettingPlugin>();
            GUIManager.Instance.ApplyButtonStyle(plugin.Button);
            plugin.Button.colors = new ColorBlock
            {
                normalColor = new Color(0.824f, 0.824f, 0.824f, 1f),
                highlightedColor = new Color(0.824f, 0.824f, 0.824f, 1f),
                pressedColor = new Color(0.537f, 0.556f, 0.556f, 1f),
                selectedColor = new Color(0.824f, 0.824f, 0.824f, 1f),
                disabledColor = new Color(0.566f, 0.566f, 0.566f, 1f),
                colorMultiplier = 1f,
                fadeDuration = 0.1f
            };
            plugin.Button.GetComponent<Image>().sprite = GUIManager.Instance.GetSprite("panel_bkg_128");
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
            config.ColorButton.GetComponent<Image>().sprite = GUIManager.Instance.GetSprite("UISprite");

            PrefabManager.OnVanillaPrefabsAvailable -= PrefabManager_OnVanillaPrefabsAvailable;
        }

        /// <summary>
        ///     After SetupGui
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
                Instantiate(self.m_mainMenu.transform.Find("MenuList"), SettingsPrefab);
            }
            catch (Exception ex)
            {
                SettingsRoot = null;
                Logger.LogWarning($"Exception caught while creating the Mod Settings entry: {ex}");
            }
        }

        /// <summary>
        ///     After first menu start
        /// </summary>
        private static void Menu_Start(On.Menu.orig_Start orig, Menu self)
        {
            orig(self);

            try
            {
                SynchronizationManager.Instance.CacheConfigurationValues();
                Instantiate(self.m_menuDialog, SettingsPrefab);
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
        /// <param name="settingsPrefab"></param>
        private static void Instantiate(Transform menuList, GameObject settingsPrefab)
        {
            var anyConfig = BepInExUtils.GetDependentPlugins(true).Any(x => GetConfigurationEntries(x.Value).Any());

            if (!anyConfig)
            {
                return;
            }

            MenuList = menuList;
            //SettingsPrefab = settingsPrefab;

            bool settingsFound = false;
            for (int i = 0; i < menuList.childCount; i++)
            {
                if (menuList.GetChild(i).name == "Settings")
                {
                    Transform modSettings = Object.Instantiate(menuList.GetChild(i), menuList);
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
                            modSettingsButton.StartCoroutine(CreateWindow());
                        }
                        catch (Exception ex)
                        {
                            SettingsRoot = null;
                            Logger.LogWarning($"Exception caught while creating the Mod Settings window: {ex}");
                        }
                    });
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
        }

        /// <summary>
        ///     Create custom configuration window
        /// </summary>
        private static IEnumerator CreateWindow()
        {
            // Create settings window
            SettingsRoot = Object.Instantiate(SettingsPrefab, MenuList.parent);

            var settings = SettingsRoot.GetComponent<ModSettings>();

            settings.CancelButton.onClick.AddListener(() =>
            {
                try { ColorPicker.Cancel(); } catch (Exception) { }
                Object.Destroy(SettingsRoot);
            });

            settings.OKButton.onClick.AddListener(() =>
            {
                try { ColorPicker.Done(); } catch (Exception) { }
                SaveConfiguration();
                Object.Destroy(SettingsRoot);

            });

            SettingsRoot.AddComponent<EscBehaviour>();

            // Iterate over all dependent plugins (including Jotunn itself)
            foreach (var mod in BepInExUtils.GetDependentPlugins(true).OrderBy(x => x.Value.Info.Metadata.Name))
            {
                if (!GetConfigurationEntries(mod.Value).Any(x => x.Value.IsVisible() && x.Value.IsWritable()))
                {
                    continue;
                }

                settings.AddPlugin(mod.Key, $"{mod.Value.Info.Metadata.Name} {mod.Value.Info.Metadata.Version}");

                foreach (var kv in GetConfigurationEntries(mod.Value)
                    .Where(x => x.Value.IsVisible() && x.Value.IsWritable())
                    .GroupBy(x => x.Key.Section))
                {
                    settings.AddSection(mod.Key, $"Section {kv.Key}");

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
                        var go = settings.AddConfig(mod.Key, $"{entry.Key.Key}:", entryAttributes.EntryColor,
                            description, entryAttributes.DescriptionColor);

                        if (entry.Value.SettingType == typeof(bool))
                        {
                            var conf = go.AddComponent<ConfigBoundBoolean>();
                            conf.SetData(mod.Value.Info.Metadata.GUID, entry.Key.Section, entry.Key.Key);
                        }
                        else if (entry.Value.SettingType == typeof(int))
                        {
                            var conf = go.AddComponent<ConfigBoundInt>();
                            conf.SetData(mod.Value.Info.Metadata.GUID, entry.Key.Section, entry.Key.Key);
                        }
                        else if (entry.Value.SettingType == typeof(float))
                        {
                            var conf = go.AddComponent<ConfigBoundFloat>();
                            conf.SetData(mod.Value.Info.Metadata.GUID, entry.Key.Section, entry.Key.Key);
                        }
                        else if (entry.Value.SettingType == typeof(double))
                        {
                            var conf = go.AddComponent<ConfigBoundDouble>();
                            conf.SetData(mod.Value.Info.Metadata.GUID, entry.Key.Section, entry.Key.Key);
                        }
                        else if (entry.Value.SettingType == typeof(string))
                        {
                            var conf = go.AddComponent<ConfigBoundString>();
                            conf.SetData(mod.Value.Info.Metadata.GUID, entry.Key.Section, entry.Key.Key);
                        }
                        else if (entry.Value.SettingType == typeof(KeyCode) &&
                                 ZInput.instance.m_buttons.ContainsKey(entry.Value.GetBoundButtonName()))
                        {
                            var conf = go.AddComponent<ConfigBoundKeyCode>();
                            conf.SetData(mod.Value.Info.Metadata.GUID, entry.Key.Section, entry.Key.Key);

                            if (entry.Value.GetButtonConfig().GamepadConfig != null)
                            {
                                var conf2 = go.AddComponent<ConfigBoundGamepadButton>();
                                conf2.SetData(mod.Value.Info.Metadata.GUID,
                                    entry.Value.GetButtonConfig().GamepadConfig.Definition.Section,
                                    entry.Value.GetButtonConfig().GamepadConfig.Definition.Key);
                            }
                        }
                        else if (entry.Value.SettingType == typeof(KeyboardShortcut) &&
                                 ZInput.instance.m_buttons.ContainsKey(entry.Value.GetBoundButtonName()))
                        {
                            var conf = go.AddComponent<ConfigBoundKeyboardShortcut>();
                            conf.SetData(mod.Value.Info.Metadata.GUID, entry.Key.Section, entry.Key.Key);
                        }
                        else if (entry.Value.SettingType == typeof(Color))
                        {
                            var conf = go.AddComponent<ConfigBoundColor>();
                            conf.SetData(mod.Value.Info.Metadata.GUID, entry.Key.Section, entry.Key.Key);
                        }
                    }
                }
            }

            /*
            // Scroll back to top
            scrollView.GetComponentInChildren<ScrollRect>().normalizedPosition = new Vector2(0, 1);

            // Show the window and fake that we are finished loading (whole thing needs a rework...)
            SettingsRoot.SetActive(true);
            */

            yield return null;
        }

        private class EscBehaviour : MonoBehaviour
        {
            private void Update()
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    try { ColorPicker.Cancel(); } catch (Exception) { }
                    Destroy(SettingsRoot);
                }
            }

            private void OnDestroy()
            {
                try { ColorPicker.Cancel(); } catch (Exception) { }
                Destroy(SettingsRoot);
            }
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
            foreach (var config in settings.Configs.Values)
            {
                var childBoolean = config.GetComponent<ConfigBoundBoolean>();
                if (childBoolean != null)
                {
                    childBoolean.WriteBack();
                    continue;
                }

                var childInt = config.GetComponent<ConfigBoundInt>();
                if (childInt != null)
                {
                    childInt.WriteBack();
                    continue;
                }

                var childFloat = config.GetComponent<ConfigBoundFloat>();
                if (childFloat != null)
                {
                    childFloat.WriteBack();
                    continue;
                }

                var childDouble = config.GetComponent<ConfigBoundDouble>();
                if (childDouble != null)
                {
                    childDouble.WriteBack();
                    continue;
                }

                var childKeyCode = config.GetComponent<ConfigBoundKeyCode>();
                if (childKeyCode != null)
                {
                    childKeyCode.WriteBack();
                    var childGamepadButton = config.GetComponentInChildren<ConfigBoundGamepadButton>();
                    if (childGamepadButton != null)
                    {
                        childGamepadButton.WriteBack();
                    }
                    continue;
                }

                var childShortcut = config.GetComponent<ConfigBoundKeyboardShortcut>();
                if (childShortcut != null)
                {
                    childShortcut.WriteBack();
                    continue;
                }

                var childString = config.GetComponent<ConfigBoundString>();
                if (childString != null)
                {
                    childString.WriteBack();
                    continue;
                }

                var childColor = config.GetComponent<ConfigBoundColor>();
                if (childColor != null)
                {
                    childColor.WriteBack();
                    continue;
                }
            }

            // Sync changed config
            SynchronizationManager.Instance.SynchronizeChangedConfig();
        }

        /// <summary>
        ///     Create a text input field (used for string, int, float)
        /// </summary>
        /// <param name="parent">parent transform</param>
        /// <param name="labelname">Label text</param>
        /// <param name="labelColor">Color of the label</param>
        /// <param name="description">Description text</param>
        /// <param name="descriptionColor">Color of the description text</param>
        /// <param name="width">Width</param>
        /// <returns></returns>
        private static GameObject CreateTextInputField(Transform parent, string labelname, Color labelColor, string description, Color descriptionColor, float width)
        {
            // Create the outer gameobject first
            var result = new GameObject("TextField", typeof(RectTransform), typeof(LayoutElement));
            result.SetWidth(width);
            result.transform.SetParent(parent, false);

            // create the label text
            var label = GUIManager.Instance.CreateText(labelname, result.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0),
                GUIManager.Instance.AveriaSerifBold, 16, labelColor, true, Color.black, width - 150f, 0, false);
            label.SetUpperLeft().SetToTextHeight();

            // create the description text
            var desc = GUIManager.Instance.CreateText(description, result.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0),
                GUIManager.Instance.AveriaSerifBold, 12, descriptionColor, true, Color.black, width - 150f, 0, false).SetUpperLeft();
            desc.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -(label.GetTextHeight() + 3f));
            desc.SetToTextHeight();

            // calculate combined height
            result.SetHeight(label.GetTextHeight() + 3f + desc.GetTextHeight() + 15f);

            // Add the input field element
            var field = new GameObject("Input", typeof(RectTransform), typeof(Image), typeof(InputField)).SetUpperRight().SetSize(140f, label.GetTextHeight() + 6f);
            field.GetComponent<Image>().sprite = GUIManager.Instance.GetSprite("text_field");
            field.GetComponent<Image>().type = Image.Type.Sliced;
            field.transform.SetParent(result.transform, false);

            var inputField = field.GetComponent<InputField>();

            var text = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Outline)).SetMiddleLeft().SetHeight(label.GetTextHeight() + 6f)
                .SetWidth(130f);
            inputField.textComponent = text.GetComponent<Text>();
            text.transform.SetParent(field.transform, false);
            text.GetComponent<RectTransform>().anchoredPosition = new Vector2(5, 0);
            text.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
            text.GetComponent<Text>().font = GUIManager.Instance.AveriaSerifBold;

            // create the placeholder element
            var placeholder = new GameObject("Placeholder", typeof(RectTransform), typeof(Text)).SetMiddleLeft().SetHeight(label.GetTextHeight() + 6f)
                .SetWidth(130f);
            inputField.placeholder = placeholder.GetComponent<Text>();
            placeholder.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
            placeholder.GetComponent<Text>().text = "";
            placeholder.GetComponent<Text>().font = GUIManager.Instance.AveriaSerifBold;
            placeholder.GetComponent<Text>().fontStyle = FontStyle.Italic;
            placeholder.GetComponent<Text>().color = Color.gray;
            placeholder.transform.SetParent(field.transform, false);
            placeholder.GetComponent<RectTransform>().anchoredPosition = new Vector2(5, 0);

            // create the slider
            var slider = DefaultControls.CreateSlider(GUIManager.Instance.ValheimControlResources);
            slider.transform.SetParent(field.transform, false);
            ((RectTransform)slider.transform).anchoredPosition = new Vector2(0, -25);
            ((RectTransform)slider.transform).sizeDelta = new Vector2(140, 30);
            GUIManager.Instance.ApplySliderStyle(slider.GetComponent<Slider>());
            slider.SetActive(false);

            // set the preferred height on the layout element
            result.GetComponent<LayoutElement>().preferredHeight = result.GetComponent<RectTransform>().rect.height;
            return result;
        }

        /// <summary>
        ///     Create a text input field and a ColorPicker button (used for Color)
        /// </summary>
        /// <param name="parent">parent transform</param>
        /// <param name="labelname">Label text</param>
        /// <param name="labelColor">Color of the label</param>
        /// <param name="description">Description text</param>
        /// <param name="descriptionColor">Color of the description text</param>
        /// <param name="width">Width</param>
        /// <returns></returns>
        private static GameObject CreateColorInputField(Transform parent, string labelname, Color labelColor, string description, Color descriptionColor, float width)
        {
            // Create the outer gameobject first
            var result = new GameObject("TextField", typeof(RectTransform), typeof(LayoutElement));
            result.SetWidth(width);
            result.transform.SetParent(parent, false);

            // create the label text
            var label = GUIManager.Instance.CreateText(labelname, result.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0),
                GUIManager.Instance.AveriaSerifBold, 16, labelColor, true, Color.black, width - 150f, 0, false);
            label.SetUpperLeft().SetToTextHeight();

            // create the description text
            var desc = GUIManager.Instance.CreateText(description, result.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0),
                GUIManager.Instance.AveriaSerifBold, 12, descriptionColor, true, Color.black, width - 150f, 0, false).SetUpperLeft();
            desc.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -(label.GetTextHeight() + 3f));
            desc.SetToTextHeight();

            // calculate combined height
            result.SetHeight(label.GetTextHeight() + 3f + desc.GetTextHeight() + 15f);

            // Add a layout component
            var layout = new GameObject("Layout", typeof(RectTransform), typeof(LayoutElement)).SetUpperRight().SetSize(140f, label.GetTextHeight() + 6f);
            layout.transform.SetParent(result.transform, false);

            // Add the input field element
            var field = new GameObject("Input", typeof(RectTransform), typeof(Image), typeof(InputField)).SetUpperLeft().SetSize(100f, label.GetTextHeight() + 6f);
            field.GetComponent<Image>().sprite = GUIManager.Instance.GetSprite("text_field");
            field.GetComponent<Image>().type = Image.Type.Sliced;
            field.transform.SetParent(layout.transform, false);

            var inputField = field.GetComponent<InputField>();

            var text = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Outline)).SetMiddleLeft().SetHeight(label.GetTextHeight() + 6f)
                .SetWidth(130f);
            inputField.textComponent = text.GetComponent<Text>();
            text.transform.SetParent(field.transform, false);
            text.GetComponent<RectTransform>().anchoredPosition = new Vector2(5, 0);
            text.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
            text.GetComponent<Text>().font = GUIManager.Instance.AveriaSerifBold;

            // create the placeholder element
            var placeholder = new GameObject("Placeholder", typeof(RectTransform), typeof(Text)).SetMiddleLeft().SetHeight(label.GetTextHeight() + 6f)
                .SetWidth(130f);
            inputField.placeholder = placeholder.GetComponent<Text>();
            placeholder.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
            placeholder.GetComponent<Text>().text = "";
            placeholder.GetComponent<Text>().font = GUIManager.Instance.AveriaSerifBold;
            placeholder.GetComponent<Text>().fontStyle = FontStyle.Italic;
            placeholder.GetComponent<Text>().color = Color.gray;
            placeholder.transform.SetParent(field.transform, false);
            placeholder.GetComponent<RectTransform>().anchoredPosition = new Vector2(5, 0);

            // Add the ColorPicker button
            var button = new GameObject("Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(ButtonSfx))
                .SetUpperRight().SetSize(30f, label.GetTextHeight() + 6f);
            button.transform.SetParent(layout.transform, false);

            // Image
            var image = button.GetComponent<Image>();
            var sprite = GUIManager.Instance.GetSprite("UISprite");
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2f;
            button.GetComponent<Button>().image = image;

            // SFX
            var sfx = button.GetComponent<ButtonSfx>();
            sfx.m_sfxPrefab = PrefabManager.Cache.GetPrefab<GameObject>("sfx_gui_button");
            sfx.m_selectSfxPrefab = PrefabManager.Cache.GetPrefab<GameObject>("sfx_gui_select");

            // Colors
            var tinter = new ColorBlock()
            {
                disabledColor = new Color(0.566f, 0.566f, 0.566f, 0.502f),
                fadeDuration = 0.1f,
                normalColor = new Color(0.824f, 0.824f, 0.824f, 1f),
                highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f),
                pressedColor = new Color(0.537f, 0.556f, 0.556f, 1f),
                selectedColor = new Color(0.824f, 0.824f, 0.824f, 1f),
                colorMultiplier = 1f
            };
            button.GetComponent<Button>().colors = tinter;

            // set the preferred height on the layout element
            result.GetComponent<LayoutElement>().preferredHeight = result.GetComponent<RectTransform>().rect.height;
            return result;
        }

        /// <summary>
        ///     Create a toggle element
        /// </summary>
        /// <param name="parent">parent transform</param>
        /// <param name="labelname">label text</param>
        /// <param name="labelColor">Color of the label</param>
        /// <param name="description">Description text</param>
        /// <param name="descriptionColor">Color of the description text</param>
        /// <param name="width">width</param>
        /// <returns></returns>
        private static GameObject CreateToggleElement(Transform parent, string labelname, Color labelColor, string description, Color descriptionColor, float width)
        {
            // Create the outer gameobject first
            var result = new GameObject("Toggler", typeof(RectTransform));
            result.transform.SetParent(parent, false);
            result.SetWidth(width);

            // and now the toggle itself
            GUIManager.Instance.CreateToggle(result.transform, 20f, 20f).SetUpperRight();

            // create the label text element
            var label = GUIManager.Instance.CreateText(labelname, result.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0),
                GUIManager.Instance.AveriaSerifBold, 16, labelColor, true, Color.black, width - 45f, 0, true).SetUpperLeft().SetToTextHeight();
            label.SetWidth(width - 45f);
            label.SetToTextHeight();
            label.transform.SetParent(result.transform, false);

            // create the description text element (easy mode, just copy the label element and change some properties)
            var desc = Object.Instantiate(result.transform.Find("Text").gameObject, result.transform);
            desc.name = "Description";
            desc.GetComponent<Text>().color = descriptionColor;
            desc.GetComponent<Text>().fontSize = 12;
            desc.GetComponent<Text>().text = description;
            desc.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -(result.transform.Find("Text").gameObject.GetTextHeight() + 3f));
            desc.SetToTextHeight();

            result.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                -desc.GetComponent<RectTransform>().anchoredPosition.y + desc.GetComponent<Text>().preferredHeight + 15f);

            // and add a layout element
            var layoutElement = result.AddComponent<LayoutElement>();
            layoutElement.preferredHeight =
                Math.Max(38f, -desc.GetComponent<RectTransform>().anchoredPosition.y + desc.GetComponent<Text>().preferredHeight) + 15f;
            result.SetHeight(layoutElement.preferredHeight);

            return result;
        }

        /// <summary>
        ///     Create a keybinding element
        /// </summary>
        /// <param name="parent">parent transform</param>
        /// <param name="labelname">label text</param>
        /// <param name="description">description text</param>
        /// ´<param name="buttonName">buttonName</param>
        /// <param name="width">width</param>
        /// <returns></returns>
        private static GameObject CreateKeybindElement(Transform parent, string labelname, string description, string buttonName, float width)
        {
            // Create label and keybind button
            var result = GUIManager.Instance.CreateKeyBindField(labelname, parent, width, 4f);

            // Create description text
            var idx = 0;
            var lastPosition = new Vector2(0, -result.GetComponent<RectTransform>().rect.height - 3f);
            GameObject desc = null;
            foreach (var part in description.Split(Environment.NewLine[0]))
            {
                var p2 = part.Trim();
                desc = GUIManager.Instance.CreateText(p2, result.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0),
                    GUIManager.Instance.AveriaSerifBold, 12, Color.white, true, Color.black, width - 150f, 0, false);
                desc.name = $"Description{idx}";
                desc.SetUpperLeft().SetToTextHeight();

                desc.GetComponent<RectTransform>().anchoredPosition = lastPosition;
                lastPosition = new Vector2(0, lastPosition.y - desc.GetTextHeight() - 3);

                idx++;
            }

            // set height and add the layout element
            result.SetHeight(-desc.GetComponent<RectTransform>().anchoredPosition.y + desc.GetComponent<Text>().preferredHeight + 15f);
            result.AddComponent<LayoutElement>().preferredHeight = result.GetComponent<RectTransform>().rect.height;

            return result;
        }

        /// <summary>
        ///     Create a KeyboardShortcut binding element
        /// </summary>
        /// <param name="parent">parent transform</param>
        /// <param name="labelname">label text</param>
        /// <param name="description">description text</param>
        /// ´<param name="buttonName">buttonName</param>
        /// <param name="width">width</param>
        /// <returns></returns>
        private static GameObject CreateShortcutbindElement(Transform parent, string labelname, string description, string buttonName, float width)
        {
            // Create label and keybind button
            var result = GUIManager.Instance.CreateKeyBindField(labelname, parent, width, 24f);

            // Create description text
            var idx = 0;
            var lastPosition = new Vector2(0, -result.GetComponent<RectTransform>().rect.height - 3f);
            GameObject desc = null;
            foreach (var part in description.Split(Environment.NewLine[0]))
            {
                var p2 = part.Trim();
                desc = GUIManager.Instance.CreateText(p2, result.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0),
                    GUIManager.Instance.AveriaSerifBold, 12, Color.white, true, Color.black, width - 150f, 0, false);
                desc.name = $"Description{idx}";
                desc.SetUpperLeft().SetToTextHeight();

                desc.GetComponent<RectTransform>().anchoredPosition = lastPosition;
                lastPosition = new Vector2(0, lastPosition.y - desc.GetTextHeight() - 3);

                idx++;
            }

            // set height and add the layout element
            result.SetHeight(-desc.GetComponent<RectTransform>().anchoredPosition.y + desc.GetComponent<Text>().preferredHeight + 15f);
            result.AddComponent<LayoutElement>().preferredHeight = result.GetComponent<RectTransform>().rect.height;

            return result;
        }


        // Helper classes 

        /// <summary>
        ///     Generic abstract version of the config binding class
        /// </summary>
        /// <typeparam name="T"></typeparam>
        internal abstract class ConfigBound<T> : MonoBehaviour
        {
            public ModSettingConfig Config { get; set; }

            public string ModGUID { get; set; }
            public string Section { get; set; }
            public string Key { get; set; }

            public ConfigEntry<T> Entry { get; set; }

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
                Entry.Value = Value;
            }

            public void SetData(string modGuid, string section, string key)
            {
                Config = gameObject.GetComponent<ModSettingConfig>();

                ModGUID = modGuid;
                Section = section;
                Key = key;

                var pluginConfig = BepInExUtils.GetDependentPlugins(true)
                    .First(x => x.Key == ModGUID).Value.Config;
                Entry = pluginConfig[Section, Key] as ConfigEntry<T>;

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
                    SettingsRoot.GetComponent<ModSettings>().OpenBindDialogue(buttonName);
                    On.ZInput.EndBindKey += ZInput_EndBindKey;
                });
            }

            private bool ZInput_EndBindKey(On.ZInput.orig_EndBindKey orig, ZInput self)
            {
                foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
                {
                    if (Input.GetKeyDown(key))
                    {
                        SetValue(key);
                        ZInput.m_binding.m_key = key;
                        On.ZInput.EndBindKey -= ZInput_EndBindKey;
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
                    SettingsRoot.GetComponent<ModSettings>().OpenBindDialogue(buttonName);
                    On.ZInput.EndBindKey += ZInput_EndBindKey;
                });
            }

            private bool ZInput_EndBindKey(On.ZInput.orig_EndBindKey orig, ZInput self)
            {
                foreach (var key in KeysToCheck)
                {
                    if (Input.GetKeyUp(key))
                    {
                        SetValue(new KeyboardShortcut(key, KeysToCheck.Where(Input.GetKey).ToArray()));
                        ZInput.m_binding.m_key = key;
                        On.ZInput.EndBindKey -= ZInput_EndBindKey;
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
                    GetValue(), Key, SetValue, (c) => Config.ColorButton.targetGraphic.color = c, true);
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
    }
}
