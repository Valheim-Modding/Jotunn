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
        private const string MenuName = "$jotunn_modsettings";

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
        ///     Our own mod config tabs
        /// </summary>
        private static readonly Dictionary<string, RectTransform> Configs = new Dictionary<string, RectTransform>();

        /// <summary>
        ///     Navigation with mode set to None
        /// </summary>
        private static readonly Navigation NavigationNone = new Navigation() { mode = Navigation.Mode.None };
        
        /// <summary>
        ///     Hook into settings setup
        /// </summary>
        [PatchInit(0)]
        public static void HookOnSettings()
        {
            On.FejdStartup.SetupGui += FejdStartup_SetupGui;
            On.Menu.Start += Menu_Start;
        }

        /// <summary>
        ///     After SetupGui
        /// </summary>
        private static void FejdStartup_SetupGui(On.FejdStartup.orig_SetupGui orig, FejdStartup self)
        {
            // Fallback english translation
            LocalizationManager.Instance.JotunnLocalization.AddTranslation(MenuName, "Mod Settings");

            orig(self);

            try
            {
                Instantiate(self.m_mainMenu.transform.Find("MenuList"), self.m_settingsPrefab);
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
                Instantiate(self.m_menuDialog, self.m_settingsPrefab);
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
            SettingsPrefab = settingsPrefab;

            bool settingsFound = false;
            for (int i = 0; i < menuList.childCount; i++)
            {
                if (menuList.GetChild(i).name == "Settings")
                {
                    Transform modSettings = Object.Instantiate(menuList.GetChild(i), menuList);
                    modSettings.GetComponentInChildren<Text>().text = LocalizationManager.Instance.TryTranslate(MenuName);
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
            // Reset
            Configs.Clear();

            // Create settings window
            SettingsRoot = Object.Instantiate(SettingsPrefab, MenuList.parent);
            SettingsRoot.SetActive(false);
            SettingsRoot.name = "ModSettings";
            SettingsRoot.transform.GetComponentInChildren<Text>().gameObject.SetWidth(500f);
            SettingsRoot.transform.GetComponentInChildren<Text>().text = LocalizationManager.Instance.TryTranslate(MenuName);
            if (Menu.instance != null)
            {
                Menu.instance.m_settingsInstance = SettingsRoot;
            }

            RectTransform panel = SettingsRoot.transform.Find("panel") as RectTransform;

            // Deactivate all
            Transform tabButtons = panel.Find("TabButtons");
            foreach (Transform t in tabButtons)
            {
                t.gameObject.SetActive(false);
            }
            tabButtons.gameObject.SetActive(false);

            RectTransform tabs = panel.Find("Tabs") as RectTransform;
            foreach (Transform t in tabs)
            {
                t.gameObject.SetActive(false);
            }
            tabs.gameObject.SetActive(false);

            // Create main scroll view
            GameObject scrollView = GUIManager.Instance.CreateScrollView(
                panel, false, true, 8f, 10f, GUIManager.Instance.ValheimScrollbarHandleColorBlock,
                new Color(0, 0, 0, 1), tabs.rect.width, tabs.rect.height);

            var group = scrollView.AddComponent<UIGroupHandler>();
            group.m_canvasGroup = scrollView.GetComponent<CanvasGroup>();
            group.m_groupPriority = 20;
            group.m_defaultElement = scrollView;
            
            RectTransform viewport =
                scrollView.transform.Find("Scroll View/Viewport/Content") as RectTransform;

            VerticalLayoutGroup scrollLayout = viewport.GetComponent<VerticalLayoutGroup>();
            scrollLayout.childControlWidth = true;
            scrollLayout.childControlHeight = true;
            scrollLayout.childForceExpandWidth = false;
            scrollLayout.childForceExpandHeight = false;
            scrollLayout.childAlignment = TextAnchor.UpperCenter;
            scrollLayout.spacing = 5f;

            // Create OK and Back button, react on Escape
            var ok = Object.Instantiate(global::Utils.FindChild(SettingsRoot.transform, "Ok").gameObject, scrollView.transform);
            ok.GetComponent<Button>().onClick.AddListener(() =>
            {
                try { ColorPicker.Done(); } catch (Exception) { }
                SaveConfiguration();
                Object.Destroy(SettingsRoot);

            });

            var back = Object.Instantiate(global::Utils.FindChild(SettingsRoot.transform, "Back").gameObject, scrollView.transform);
            back.GetComponent<Button>().onClick.AddListener(() =>
            {
                try { ColorPicker.Cancel(); } catch (Exception) { }
                Object.Destroy(SettingsRoot);
            });

            SettingsRoot.AddComponent<EscBehaviour>();
            
            // Iterate over all dependent plugins (including Jotunn itself)
            foreach (var mod in BepInExUtils.GetDependentPlugins(true).OrderBy(x => x.Value.Info.Metadata.Name))
            {
                yield return CreatePlugin(mod, viewport);
            }
            
            // Scroll back to top
            scrollView.GetComponentInChildren<ScrollRect>().normalizedPosition = new Vector2(0, 1);

            // Show the window and fake that we are finished loading (whole thing needs a rework...)
            SettingsRoot.SetActive(true);

            // Iterate over all plugins again, creating the actual config values
            foreach (var mod in BepInExUtils.GetDependentPlugins(true).OrderBy(x => x.Value.Info.Metadata.Name))
            {
                if (Configs.ContainsKey(mod.Key))
                {
                    yield return CreateContent(mod, Configs[mod.Key]);
                }
            }

            // Connect OK and Back's Navigation
            var okButton = ok.GetComponent<Button>();
            var backButton = back.GetComponent<Button>();

            var okNavigation = okButton.navigation;
            var backNavigation = backButton.navigation;

            okNavigation.selectOnLeft = backButton;
            backNavigation.selectOnRight = okButton;

            // These don't seem to work, leaving in to show intention
            okNavigation.selectOnUp = Configs.Values.Last().gameObject.GetComponent<Selectable>();
            backNavigation.selectOnUp = Configs.Values.Last().gameObject.GetComponent<Selectable>();

            okButton.navigation = okNavigation;
            backButton.navigation = backNavigation;
        }
        
        private class EscBehaviour : MonoBehaviour
        {
            private void OnDestroy()
            {
                try { ColorPicker.Cancel(); } catch (Exception) { }
            }
        }

        private static IEnumerator CreatePlugin(KeyValuePair<string, BaseUnityPlugin> mod, RectTransform pluginViewport)
        {
            // Create a header if there are any relevant configuration entries
            if (GetConfigurationEntries(mod.Value).Where(x => x.Value.IsVisible() && x.Value.IsWritable()).GroupBy(x => x.Key.Section).Any())
            {
                // Create plugin
                GameObject plugin = new GameObject(mod.Key, typeof(RectTransform), typeof(LayoutElement));
                plugin.SetWidth(pluginViewport.rect.width);
                plugin.GetComponent<LayoutElement>().preferredHeight = 40f;
                plugin.transform.SetParent(pluginViewport, false);

                var pluginLayout = plugin.AddComponent<VerticalLayoutGroup>();
                pluginLayout.childControlWidth = true;
                pluginLayout.childControlHeight = true;
                pluginLayout.childForceExpandWidth = true;
                pluginLayout.childForceExpandHeight = true;
                pluginLayout.childAlignment = TextAnchor.MiddleCenter;
                pluginLayout.spacing = 5f;

                // Create button element
                GameObject button = GUIManager.Instance.CreateButton(
                    $"{mod.Value.Info.Metadata.Name} {mod.Value.Info.Metadata.Version}", plugin.transform,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), plugin.GetWidth(), 40f);
                button.name = mod.Key;
                button.GetComponent<Button>().colors = new ColorBlock
                {
                    normalColor = new Color(0.824f, 0.824f, 0.824f, 0.5f),
                    highlightedColor = new Color(0.824f, 0.824f, 0.824f, 0.8f),
                    pressedColor = new Color(0.537f, 0.556f, 0.556f, 0.8f),
                    selectedColor = new Color(0.824f, 0.824f, 0.824f, 0.8f),
                    disabledColor = new Color(0.566f, 0.566f, 0.566f, 0.502f),
                    colorMultiplier = 1f,
                    fadeDuration = 0.1f
                };
                button.GetComponent<Image>().sprite = GUIManager.Instance.GetSprite("panel_bkg_128_transparent");
                button.GetComponentInChildren<Text>().fontSize = 20;
                button.AddComponent<LayoutElement>().preferredHeight = 40f;
                button.SetActive(true);

                // Create content element
                GameObject content = new GameObject("content", typeof(RectTransform), typeof(LayoutElement));
                content.SetWidth(plugin.GetWidth());
                
                RectTransform contentViewport = content.GetComponent<RectTransform>();
                contentViewport.SetParent(plugin.transform, false);

                var contentLayout = content.AddComponent<VerticalLayoutGroup>();
                contentLayout.childControlWidth = false;
                contentLayout.childControlHeight = true;
                contentLayout.childForceExpandWidth = false;
                contentLayout.childForceExpandHeight = false;
                contentLayout.childAlignment = TextAnchor.UpperCenter;
                contentLayout.spacing = 5f;

                content.SetActive(false);

                button.GetComponent<Button>().onClick.AddListener(() =>
                {
                    content.SetActive(!content.activeSelf);
                    if (content.activeSelf)
                    {
                        plugin.GetComponent<LayoutElement>().preferredHeight =
                            button.GetComponent<LayoutElement>().preferredHeight +
                            content.GetComponent<LayoutElement>().preferredHeight;

                    }
                    else
                    {
                        plugin.GetComponent<LayoutElement>().preferredHeight =
                            button.GetComponent<LayoutElement>().preferredHeight;
                    }
                });

                Configs.Add(mod.Key, contentViewport);

                yield return null;
            }
        }

        private static IEnumerator CreateContent(KeyValuePair<string, BaseUnityPlugin> mod, RectTransform contentViewport)
        {
            float innerWidth = contentViewport.rect.width - 25f;
            float preferredHeight = 0f;

            // Iterate over all configuration entries (grouped by their sections)
            foreach (var kv in GetConfigurationEntries(mod.Value).Where(x => x.Value.IsVisible() && x.Value.IsWritable()).GroupBy(x => x.Key.Section))
            {
                // Create section header Text element
                var sectiontext = GUIManager.Instance.CreateText(
                    "Section " + kv.Key, contentViewport, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0, 0), GUIManager.Instance.AveriaSerifBold, 16, GUIManager.Instance.ValheimOrange,
                    true, Color.black, contentViewport.rect.width, 30, false);
                sectiontext.SetMiddleCenter();
                sectiontext.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
                sectiontext.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
                sectiontext.AddComponent<LayoutElement>().preferredHeight = 30f;
                preferredHeight += sectiontext.GetHeight();

                // Iterate over all entries of this section
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
                    // Create config entry
                    // switch by type
                    var entryAttributes =
                        entry.Value.Description.Tags.FirstOrDefault(x => x is ConfigurationManagerAttributes) as
                            ConfigurationManagerAttributes;
                    if (entryAttributes == null)
                    {
                        entryAttributes = new ConfigurationManagerAttributes();
                    }

                    if (entry.Value.SettingType == typeof(bool))
                    {
                        // Create toggle element
                        var go = CreateToggleElement(contentViewport,
                            entry.Key.Key + ":",
                            entryAttributes.EntryColor,
                            entry.Value.Description.Description + (entryAttributes.IsAdminOnly
                                ? $"{Environment.NewLine}(Server side setting)"
                                : ""),
                            entryAttributes.DescriptionColor, innerWidth);
                        var conf = go.AddComponent<ConfigBoundBoolean>();
                        conf.SetData(mod.Value.Info.Metadata.GUID, entry.Key.Section, entry.Key.Key);
                        preferredHeight += go.GetHeight();
                    }
                    else if (entry.Value.SettingType == typeof(int))
                    {
                        var description = entry.Value.Description.Description;
                        if (entry.Value.Description.AcceptableValues != null)
                        {
                            description += Environment.NewLine + "(" +
                                           entry.Value.Description.AcceptableValues.ToDescriptionString().TrimStart('#')
                                               .Trim() + ")";
                        }

                        // Create input field int
                        var go = CreateTextInputField(contentViewport,
                            entry.Key.Key + ":",
                            entryAttributes.EntryColor,
                            description + (entryAttributes.IsAdminOnly ? $"{Environment.NewLine}(Server side setting)" : ""),
                            entryAttributes.DescriptionColor, innerWidth);
                        var conf = go.AddComponent<ConfigBoundInt>();
                        conf.SetData(mod.Value.Info.Metadata.GUID, entry.Key.Section, entry.Key.Key);
                        preferredHeight += go.GetHeight();
                    }
                    else if (entry.Value.SettingType == typeof(float))
                    {
                        var description = entry.Value.Description.Description;
                        if (entry.Value.Description.AcceptableValues != null)
                        {
                            description += Environment.NewLine + "(" +
                                           entry.Value.Description.AcceptableValues.ToDescriptionString().TrimStart('#')
                                               .Trim() + ")";
                        }

                        // Create input field float
                        var go = CreateTextInputField(contentViewport,
                            entry.Key.Key + ":",
                            entryAttributes.EntryColor,
                            description + (entryAttributes.IsAdminOnly ? $"{Environment.NewLine}(Server side setting)" : ""),
                            entryAttributes.DescriptionColor, innerWidth);
                        var conf = go.AddComponent<ConfigBoundFloat>();
                        conf.SetData(mod.Value.Info.Metadata.GUID, entry.Key.Section, entry.Key.Key);
                        preferredHeight += go.GetHeight();
                    }
                    else if (entry.Value.SettingType == typeof(double))
                    {
                        var description = entry.Value.Description.Description;
                        if (entry.Value.Description.AcceptableValues != null)
                        {
                            description += Environment.NewLine + "(" +
                                           entry.Value.Description.AcceptableValues.ToDescriptionString().TrimStart('#')
                                               .Trim() + ")";
                        }

                        // Create input field double
                        var go = CreateTextInputField(contentViewport,
                            entry.Key.Key + ":",
                            entryAttributes.EntryColor,
                            description + (entryAttributes.IsAdminOnly ? $"{Environment.NewLine}(Server side setting)" : ""),
                            entryAttributes.DescriptionColor, innerWidth);
                        var conf = go.AddComponent<ConfigBoundDouble>();
                        conf.SetData(mod.Value.Info.Metadata.GUID, entry.Key.Section, entry.Key.Key);
                        preferredHeight += go.GetHeight();
                    }
                    else if (entry.Value.SettingType == typeof(KeyCode) &&
                             ZInput.instance.m_buttons.ContainsKey(entry.Value.GetBoundButtonName()))
                    {
                        // Create key binder
                        var buttonName = entry.Value.GetBoundButtonName();
                        var buttonText = $"{entry.Value.Description.Description}";
                        buttonText += $"{Environment.NewLine}This key is bound to button '{buttonName.Split('!')[0]}'.";
                        buttonText += entryAttributes.IsAdminOnly
                            ? $"{Environment.NewLine}(Server side setting)"
                            : "";
                        
                        var go = CreateKeybindElement(contentViewport,
                            entry.Key.Key + ":", buttonText,
                            buttonName, innerWidth);
                        var conf = go.AddComponent<ConfigBoundKeyCode>();
                        conf.SetData(mod.Value.Info.Metadata.GUID, entry.Key.Section, entry.Key.Key);
                        preferredHeight += go.GetHeight();

                        if (entry.Value.GetButtonConfig().GamepadConfig != null)
                        {
                            // Create dropdown
                            var dropdown = GUIManager.Instance.CreateDropDown(
                                    go.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0), 14)
                                .SetMiddleRight().SetSize(140f, 22f);
                            var rect = dropdown.GetComponent<RectTransform>();
                            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, rect.anchoredPosition.y - 3f);
                            var conf2 = dropdown.AddComponent<ConfigBoundGamepadButton>();
                            conf2.SetData(mod.Value.Info.Metadata.GUID,
                                entry.Value.GetButtonConfig().GamepadConfig.Definition.Section,
                                entry.Value.GetButtonConfig().GamepadConfig.Definition.Key);
                            preferredHeight += dropdown.GetHeight();
                        }
                    }
                    else if (entry.Value.SettingType == typeof(KeyboardShortcut) &&
                             ZInput.instance.m_buttons.ContainsKey(entry.Value.GetBoundButtonName()))
                    {
                        var description = entry.Value.Description.Description;

                        // Create shortcut binder
                        var buttonName = entry.Value.GetBoundButtonName();
                        var buttonText = $"{entry.Value.Description.Description}";
                        buttonText += $"{Environment.NewLine}This shortcut is bound to button '{buttonName.Split('!')[0]}'.";
                        buttonText += entryAttributes.IsAdminOnly
                            ? $"{Environment.NewLine}(Server side setting)"
                            : "";

                        var go = CreateShortcutbindElement(contentViewport,
                            entry.Key.Key + ":", buttonText,
                            buttonName, innerWidth);
                        var conf = go.AddComponent<ConfigBoundKeyboardShortcut>();
                        conf.SetData(mod.Value.Info.Metadata.GUID, entry.Key.Section, entry.Key.Key);
                        preferredHeight += go.GetHeight();
                    }
                    else if (entry.Value.SettingType == typeof(string))
                    {
                        // Create input field string
                        var go = CreateTextInputField(contentViewport,
                            entry.Key.Key + ":",
                            entryAttributes.EntryColor,
                            entry.Value.Description.Description + (entryAttributes.IsAdminOnly
                                ? $"{Environment.NewLine}(Server side setting)"
                                : ""),
                            entryAttributes.DescriptionColor, innerWidth);
                        var conf = go.AddComponent<ConfigBoundString>(); 
                        conf.SetData(mod.Value.Info.Metadata.GUID, entry.Key.Section, entry.Key.Key);
                        preferredHeight += go.GetHeight();
                    }
                    else if (entry.Value.SettingType == typeof(Color))
                    {
                        // Create input field string with color picker
                        var go = CreateColorInputField(contentViewport,
                            entry.Key.Key + ":",
                            entryAttributes.EntryColor,
                            entry.Value.Description.Description + (entryAttributes.IsAdminOnly
                                ? $"{Environment.NewLine}(Server side setting)"
                                : ""),
                            entryAttributes.DescriptionColor, innerWidth);
                        var conf = go.AddComponent<ConfigBoundColor>();
                        conf.SetData(mod.Value.Info.Metadata.GUID, entry.Key.Section, entry.Key.Key);
                        preferredHeight += go.GetHeight();
                    }
                }
            }

            contentViewport.GetComponent<LayoutElement>().preferredHeight = preferredHeight;

            yield return null;
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
            // Iterate over all configs
            foreach (Transform config in Configs.Values)
            {
                // Just iterate over the children in the scroll view and act if we find a ConfigBound<T> component
                foreach (Transform values in config)
                {
                    var childBoolean = values.gameObject.GetComponent<ConfigBoundBoolean>();
                    if (childBoolean != null)
                    {
                        childBoolean.WriteBack();
                        continue;
                    }

                    var childInt = values.gameObject.GetComponent<ConfigBoundInt>();
                    if (childInt != null)
                    {
                        childInt.WriteBack();
                        continue;
                    }

                    var childFloat = values.gameObject.GetComponent<ConfigBoundFloat>();
                    if (childFloat != null)
                    {
                        childFloat.WriteBack();
                        continue;
                    }

                    var childDouble = values.gameObject.GetComponent<ConfigBoundDouble>();
                    if (childDouble != null)
                    {
                        childDouble.WriteBack();
                        continue;
                    }

                    var childKeyCode = values.gameObject.GetComponent<ConfigBoundKeyCode>();
                    if (childKeyCode != null)
                    {
                        childKeyCode.WriteBack();
                        var childGamepadButton = values.gameObject.GetComponentInChildren<ConfigBoundGamepadButton>();
                        if (childGamepadButton != null)
                        {
                            childGamepadButton.WriteBack();
                        }
                        continue;
                    }

                    var childShortcut = values.gameObject.GetComponent<ConfigBoundKeyboardShortcut>();
                    if (childShortcut != null)
                    {
                        childShortcut.WriteBack();
                        continue;
                    }

                    var childString = values.gameObject.GetComponent<ConfigBoundString>();
                    if (childString != null)
                    {
                        childString.WriteBack();
                        continue;
                    }

                    var childColor = values.gameObject.GetComponent<ConfigBoundColor>();
                    if (childColor != null)
                    {
                        childColor.WriteBack();
                        continue;
                    }
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
            SetNavigationNone(field);
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

            // Add SelectableConfig and pass field as target
            var selectable = result.AddComponent<SelectableConfig>();
            selectable.InteractionTarget = slider;

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
            SetNavigationNone(field);

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

            // Add SelectableConfig and pass ColorPicker button as target

            result.AddComponent<SelectableConfig>().InteractionTarget = button;

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
            var toggle = GUIManager.Instance.CreateToggle(result.transform, 20f, 20f).SetUpperRight();

            // Add SelectableConfig and pass toggle as target
            result.AddComponent<SelectableConfig>().InteractionTarget = toggle;

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

            // Setup keybind button for controller navigation
            var button = result.transform.Find("Button").gameObject;
            result.AddComponent<SelectableConfig>().InteractionTarget = button;

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

            // Setup keybind button for controller navigation
            var button = result.transform.Find("Button").gameObject;
            result.AddComponent<SelectableConfig>().InteractionTarget = button;

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
        /// Sets Selectable Component navigation mode to None
        /// </summary>
        /// <param name="go">Game Object with a Selectable Component to be set to navigation mode None</param>
        private static void SetNavigationNone(GameObject go)
        {
            var selectable = go.GetComponent<Selectable>();
            if (selectable != null)
            {
                selectable.navigation = NavigationNone;
            }
        }
        // Helper classes 

        /// <summary>
        ///     Generic abstract version of the config binding class
        /// </summary>
        /// <typeparam name="T"></typeparam>
        internal abstract class ConfigBound<T> : MonoBehaviour
        {
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
            private Toggle Toggle;

            public override void Register()
            {
                Toggle = gameObject.transform.Find("Toggle").GetComponent<Toggle>();
            }
            
            public override bool GetValue()
            {
                return Toggle.isOn;
            }

            public override void SetValue(bool value)
            {
                Toggle.isOn = value;
            }

            public override void SetEnabled(bool enabled)
            {
                Toggle.enabled = enabled;
            }

            public override void SetReadOnly(bool readOnly)
            {
                Toggle.enabled = !readOnly;
            }
        }

        /// <summary>
        ///     Integer binding
        /// </summary>
        internal class ConfigBoundInt : ConfigBound<int>
        {
            private InputField Input;

            public override void Register()
            {
                Input = gameObject.transform.Find("Input").GetComponent<InputField>();
                Input.characterValidation = InputField.CharacterValidation.Integer;

                if (Entry.Description.AcceptableValues is AcceptableValueRange<int> acceptableValueRange)
                {
                    var slider = Input.transform.Find("Slider").GetComponent<Slider>();
                    slider.gameObject.SetActive(true);
                    slider.minValue = acceptableValueRange.MinValue;
                    slider.maxValue = acceptableValueRange.MaxValue;
                    slider.onValueChanged.AddListener(value => 
                        Input.SetTextWithoutNotify(((int)value)
                            .ToString(CultureInfo.CurrentCulture)));
                    Input.onValueChanged.AddListener(text =>
                    {
                        if (int.TryParse(text, out var value))
                        {
                            slider.SetValueWithoutNotify(value);
                        }
                    });
                }
                Input.onValueChanged.AddListener(x =>
                {
                    Input.textComponent.color = IsValid() ? Color.white : Color.red;
                });
            }
            
            public override int GetValue()
            {
                int temp;
                if (!int.TryParse(Input.text, out temp))
                {
                    temp = Default;
                }

                return temp;
            }

            public override void SetValue(int value)
            {
                Input.text = value.ToString();
            }

            public override void SetEnabled(bool enabled)
            {
                Input.enabled = enabled;
            }

            public override void SetReadOnly(bool readOnly)
            {
                Input.readOnly = readOnly;
                Input.textComponent.color = readOnly ? Color.grey : Color.white;
            }
        }

        /// <summary>
        ///     Float binding
        /// </summary>
        internal class ConfigBoundFloat : ConfigBound<float>
        {
            private InputField Input;

            public override void Register()
            {
                Input = gameObject.transform.Find("Input").GetComponent<InputField>();
                Input.characterValidation = InputField.CharacterValidation.Decimal;

                if (Entry.Description.AcceptableValues is AcceptableValueRange<float> acceptableValueRange)
                {
                    var slider = gameObject.GetComponentInChildren<Slider>(true);
                    slider.gameObject.SetActive(true);
                    slider.minValue = acceptableValueRange.MinValue;
                    slider.maxValue = acceptableValueRange.MaxValue;
                    var step = Mathf.Clamp(slider.minValue / slider.maxValue, 0.1f, 1f);
                    slider.onValueChanged.AddListener(value => 
                        Input.SetTextWithoutNotify((Mathf.Round(value/step)*step)
                            .ToString("F3", CultureInfo.CurrentCulture)));
                    Input.onValueChanged.AddListener(text =>
                    {
                        if (float.TryParse(text, out var value))
                        {
                            slider.SetValueWithoutNotify(value);
                        }
                    });
                }
                Input.onValueChanged.AddListener(x =>
                {
                    Input.textComponent.color = IsValid() ? Color.white : Color.red;
                });
            }

            public override float GetValue()
            {
                float temp;

                if (!float.TryParse(Input.text, NumberStyles.Number, CultureInfo.CurrentCulture.NumberFormat, out temp))
                {
                    temp = Default;
                }

                return temp;
            }

            public override void SetValue(float value)
            {
                Input.text = value.ToString("F3");
            }

            public override void SetEnabled(bool enabled)
            {
                Input.enabled = enabled;
            }

            public override void SetReadOnly(bool readOnly)
            {
                Input.readOnly = readOnly;
                Input.textComponent.color = readOnly ? Color.grey : Color.white;
            }
        }

        /// <summary>
        ///     Double binding
        /// </summary>
        internal class ConfigBoundDouble : ConfigBound<double>
        {
            private InputField Input;

            public override void Register()
            {
                Input = gameObject.transform.Find("Input").GetComponent<InputField>();
                Input.characterValidation = InputField.CharacterValidation.Decimal;

                if (Entry.Description.AcceptableValues is AcceptableValueRange<double> acceptableValueRange)
                {
                    var slider = GetComponentInChildren<Slider>(true);
                    slider.gameObject.SetActive(true);
                    slider.minValue = (float) acceptableValueRange.MinValue;
                    slider.maxValue = (float) acceptableValueRange.MaxValue;
                    var step = Mathf.Clamp(slider.minValue / slider.maxValue, 0.1f, 1f);
                    slider.onValueChanged.AddListener(value => 
                        Input.SetTextWithoutNotify((Mathf.Round(value / step) * step)
                            .ToString("F3", CultureInfo.CurrentCulture)));
                    Input.onValueChanged.AddListener(text =>
                    {
                        if (double.TryParse(text, out var value))
                        {
                            slider.SetValueWithoutNotify((float)value);
                        }
                    });
                }
                Input.onValueChanged.AddListener(x =>
                {
                    Input.textComponent.color = IsValid() ? Color.white : Color.red;
                });
            }

            public override double GetValue()
            {
                double temp;

                if (!double.TryParse(Input.text, NumberStyles.Number, CultureInfo.CurrentCulture.NumberFormat, out temp))
                {
                    temp = Default;
                }

                return temp;
            }

            public override void SetValue(double value)
            {
                Input.text = value.ToString("F3");
            }

            public override void SetEnabled(bool enabled)
            {
                Input.enabled = enabled;
            }

            public override void SetReadOnly(bool readOnly)
            {
                Input.readOnly = readOnly;
                Input.textComponent.color = readOnly ? Color.grey : Color.white;
            }
        }

        /// <summary>
        ///     KeyCode binding
        /// </summary>
        internal class ConfigBoundKeyCode : ConfigBound<KeyCode>
        {
            private Text Text;
            private Button Button;

            public override void Register()
            {
                Text = gameObject.transform.Find("Button/Text").GetComponent<Text>();
                Button = gameObject.transform.Find("Button").GetComponent<Button>();
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
                Button.onClick.AddListener(() =>
                {
                    StartCoroutine(nameof(CheckForKeysDown));
                });
            }

            private IEnumerator CheckForKeysDown()
            {
                var anyKeyDown = false;
                do
                {
                    anyKeyDown = false;
                    foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
                    {
                        if (Input.GetKeyDown(key))
                        {
                            anyKeyDown = true;
                        }
                    }
                    if (anyKeyDown)
                        yield return null;

                } while (anyKeyDown);

                var buttonName = Entry.GetBoundButtonName();
                Settings.instance.OpenBindDialog(buttonName);
                On.ZInput.EndBindKey += ZInput_EndBindKey;
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
                Button.enabled = enabled;
            }

            public override void SetReadOnly(bool readOnly)
            {
                Button.enabled &= readOnly;
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
            private Button Button;

            public override void Register()
            {
                Text = gameObject.transform.Find("Button/Text").GetComponent<Text>();
                Button = gameObject.transform.Find("Button").GetComponent<Button>();
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
                Button.onClick.AddListener(() =>
                {
                    StartCoroutine(nameof(CheckForKeysDownAndUp));
                });
            }

            private IEnumerator CheckForKeysDownAndUp()
            {
                var anyKeyDown = false;
                do
                {
                    anyKeyDown = false;
                    foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
                    {
                        if (Input.GetKeyDown(key) || Input.GetKey(key))
                        {
                            anyKeyDown = true;
                        }
                    }
                    if (anyKeyDown)
                        yield return null;

                } while (anyKeyDown);

                var buttonName = Entry.GetBoundButtonName();
                Settings.instance.OpenBindDialog(buttonName);
                On.ZInput.EndBindKey += ZInput_EndBindKey;
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
                Button.enabled = enabled;
            }

            public override void SetReadOnly(bool readOnly)
            {
                Button.enabled &= readOnly;
                Text.color = readOnly ? Color.grey : Color.white;
            }
        }
        
        /// <summary>
        ///     GamepadButton binding
        /// </summary>
        internal class ConfigBoundGamepadButton : ConfigBound<InputManager.GamepadButton>
        {
            private Dropdown Dropdown;
            
            public override void Register()
            {
                Dropdown = gameObject.GetComponentInChildren<Dropdown>();
                Dropdown.AddOptions(Enum.GetNames(typeof(InputManager.GamepadButton)).ToList());
            }
            
            public override InputManager.GamepadButton GetValue()
            {
                if (Enum.TryParse<InputManager.GamepadButton>(Dropdown.options[Dropdown.value].text, out var ret))
                {
                    return ret;
                }

                return InputManager.GamepadButton.None;
            }

            public override void SetValue(InputManager.GamepadButton value)
            {
                Dropdown.value = Dropdown.options.IndexOf(Dropdown.options.FirstOrDefault(x =>
                    x.text.Equals(Enum.GetName(typeof(InputManager.GamepadButton), value))));
                Dropdown.RefreshShownValue();
            }
            
            public void Start()
            {
                var buttonName = $"Joy!{Entry.GetBoundButtonName()}";
                Dropdown.onValueChanged.AddListener(index =>
                {
                    if (Enum.TryParse<InputManager.GamepadButton>(Dropdown.options[index].text, out var btn) &&
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
                Dropdown.enabled = enabled;
            }

            public override void SetReadOnly(bool readOnly)
            {
                Dropdown.enabled = !readOnly;
                Dropdown.itemText.color = readOnly ? Color.grey : Color.white;
            }
        }

        /// <summary>
        ///     String binding
        /// </summary>
        internal class ConfigBoundString : ConfigBound<string>
        {
            private InputField Input;
            
            public override void Register()
            {
                Input = gameObject.transform.Find("Input").GetComponent<InputField>();
                Input.characterValidation = InputField.CharacterValidation.None;
                Input.contentType = InputField.ContentType.Standard;
            }
            
            public override string GetValue()
            {
                return Input.text;
            }

            public override void SetValue(string value)
            {
                Input.text = value;
            }

            public override void SetEnabled(bool enabled)
            {
                Input.enabled = enabled;
            }

            public override void SetReadOnly(bool readOnly)
            {
                Input.readOnly = readOnly;
                Input.textComponent.color = readOnly ? Color.grey : Color.white;
            }
        }

        internal class ConfigBoundColor : ConfigBound<Color>
        {
            private InputField Input;
            private Button Button;
            private Image Image;

            public override void Register()
            {
                Input = gameObject.transform.Find("Layout/Input").GetComponent<InputField>();
                Input.onEndEdit.AddListener(SetButtonColor);
                Input.characterValidation = InputField.CharacterValidation.None;
                Input.contentType = InputField.ContentType.Alphanumeric;

                Button = gameObject.transform.Find("Layout/Button").GetComponent<Button>();
                Button.onClick.AddListener(ShowColorPicker);

                Image = gameObject.transform.Find("Layout/Button").GetComponent<Image>();
            }
            
            public override Color GetValue()
            {
                var col = Input.text;
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
                Input.text = StringFromColor(value);
                Image.color = value;
            }

            public override void SetEnabled(bool enabled)
            {
                Input.enabled = enabled;
                Button.enabled = enabled;
                if (enabled)
                {
                    Input.onEndEdit.AddListener(SetButtonColor);
                    Button.onClick.AddListener(ShowColorPicker);
                }
                else
                {
                    Input.onEndEdit.RemoveAllListeners();
                    Button.onClick.RemoveAllListeners();
                }
            }

            public override void SetReadOnly(bool readOnly)
            {
                Input.readOnly = readOnly;
                Input.textComponent.color = readOnly ? Color.grey : Color.white;
                Button.enabled = !readOnly;
            }

            private void SetButtonColor(string value)
            {
                if (string.IsNullOrEmpty(value))
                {
                    return;
                }
                Image.color = ColorFromString(value);
            }

            private void ShowColorPicker()
            {
                if (!ColorPicker.done)
                {
                    ColorPicker.Cancel();
                }
                GUIManager.Instance.CreateColorPicker(
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    GetValue(), Key, SetValue, (c) => Image.color = c, true);
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
