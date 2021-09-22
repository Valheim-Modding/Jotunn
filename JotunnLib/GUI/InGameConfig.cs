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
    internal class InGameConfig
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
        private static readonly List<Transform> Configs = new List<Transform>();
        
        /// <summary>
        ///     Cache keybinds
        /// </summary>
        internal static Dictionary<string, List<Tuple<string, ConfigDefinition, ConfigEntryBase>>> ConfigurationKeybindings =
            new Dictionary<string, List<Tuple<string, ConfigDefinition, ConfigEntryBase>>>();

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

            // Reset keybinding cache
            ConfigurationKeybindings.Clear();
            foreach (var mod in BepInExUtils.GetDependentPlugins(true))
            {
                foreach (var kv in GetConfigurationEntries(mod.Value).Where(x => x.Value.IsVisible() && x.Value.IsButtonBound()))
                {
                    var buttonName = kv.Value.GetBoundButtonName();
                    if (!string.IsNullOrEmpty(buttonName))
                    {
                        if (!ConfigurationKeybindings.ContainsKey(buttonName))
                        {
                            ConfigurationKeybindings.Add(buttonName, new List<Tuple<string, ConfigDefinition, ConfigEntryBase>>());
                        }
                        ConfigurationKeybindings[buttonName].Add(new Tuple<string, ConfigDefinition, ConfigEntryBase>(mod.Key, kv.Key, kv.Value));
                    }
                }
            }

            // Iterate over all dependent plugins (including Jotunn itself)
            foreach (var mod in BepInExUtils.GetDependentPlugins(true).OrderBy(x => x.Value.Info.Metadata.Name))
            {
                yield return CreatePlugin(mod, viewport);
            }

            // Scroll back to top
            scrollView.GetComponentInChildren<ScrollRect>().normalizedPosition = new Vector2(0, 1);

            // Finally show the window
            SettingsRoot.SetActive(true);
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
                button.name = "button";
                button.GetComponent<Image>().sprite = GUIManager.Instance.GetSprite("panel_interior_bkg_128");
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

                yield return CreateContent(mod, contentViewport);

                Configs.Add(contentViewport);
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
                            entryAttributes.DescriptionColor, mod.Value.Info.Metadata.GUID, entry.Key.Section, entry.Key.Key,
                            innerWidth);
                        SetProperties(go.GetComponent<ConfigBoundBoolean>(), entry);
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
                        go.AddComponent<ConfigBoundInt>()
                            .SetData(mod.Value.Info.Metadata.GUID, entry.Key.Section, entry.Key.Key);
                        go.transform.Find("Input").GetComponent<InputField>().characterValidation =
                            InputField.CharacterValidation.Integer;
                        SetProperties(go.GetComponent<ConfigBoundInt>(), entry);
                        go.transform.Find("Input").GetComponent<InputField>().onValueChanged.AddListener(x =>
                        {
                            go.transform.Find("Input").GetComponent<InputField>().textComponent.color =
                                go.GetComponent<ConfigBoundInt>().IsValid() ? Color.white : Color.red;
                        });
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
                        go.AddComponent<ConfigBoundFloat>()
                            .SetData(mod.Value.Info.Metadata.GUID, entry.Key.Section, entry.Key.Key);
                        go.transform.Find("Input").GetComponent<InputField>().characterValidation =
                            InputField.CharacterValidation.Decimal;
                        SetProperties(go.GetComponent<ConfigBoundFloat>(), entry);
                        go.transform.Find("Input").GetComponent<InputField>().onValueChanged.AddListener(x =>
                        {
                            go.transform.Find("Input").GetComponent<InputField>().textComponent.color =
                                go.GetComponent<ConfigBoundFloat>().IsValid() ? Color.white : Color.red;
                        });
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
                        go.AddComponent<ConfigBoundDouble>()
                            .SetData(mod.Value.Info.Metadata.GUID, entry.Key.Section, entry.Key.Key);
                        go.transform.Find("Input").GetComponent<InputField>().characterValidation =
                            InputField.CharacterValidation.Decimal;
                        SetProperties(go.GetComponent<ConfigBoundDouble>(), entry);
                        go.transform.Find("Input").GetComponent<InputField>().onValueChanged.AddListener(x =>
                        {
                            go.transform.Find("Input").GetComponent<InputField>().textComponent.color =
                                go.GetComponent<ConfigBoundDouble>().IsValid() ? Color.white : Color.red;
                        });
                        preferredHeight += go.GetHeight();
                    }
                    else if (entry.Value.SettingType == typeof(KeyCode) &&
                             ZInput.instance.m_buttons.ContainsKey(entry.Value.GetBoundButtonName()))
                    {
                        // Create key binder
                        var buttonName = entry.Value.GetBoundButtonName();
                        var buttonText =
                            $"{entry.Value.Description.Description}{Environment.NewLine}This key is bound to button '{buttonName.Split('!')[0]}'.";
                        if (!string.IsNullOrEmpty(buttonName) && ConfigurationKeybindings.ContainsKey(buttonName))
                        {
                            var duplicateKeybindingText = "";
                            if (ConfigurationKeybindings[buttonName].Count > 1)
                            {
                                duplicateKeybindingText +=
                                    $"{Environment.NewLine}Other mods using this button:{Environment.NewLine}";
                                foreach (var buttons in ConfigurationKeybindings[buttonName])
                                {
                                    // If it is the same config entry, just skip it
                                    if (buttons.Item2 == entry.Key && buttons.Item1 == mod.Key)
                                    {
                                        continue;
                                    }

                                    // Add modguid as text
                                    duplicateKeybindingText += $"{buttons.Item1}, ";
                                }

                                // add to buttonText, but without last ', '
                                buttonText += duplicateKeybindingText.Trim(' ').TrimEnd(',');
                            }
                        }

                        var go = CreateKeybindElement(contentViewport,
                            entry.Key.Key + ":", buttonText,
                            mod.Value.Info.Metadata.GUID, entry.Key.Section, entry.Key.Key, buttonName, innerWidth);
                        go.GetComponent<ConfigBoundKeyCode>()
                            .SetData(mod.Value.Info.Metadata.GUID, entry.Key.Section, entry.Key.Key);
                        SetProperties(go.GetComponent<ConfigBoundKeyCode>(), entry);
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
                        go.AddComponent<ConfigBoundString>()
                            .SetData(mod.Value.Info.Metadata.GUID, entry.Key.Section, entry.Key.Key);
                        go.transform.Find("Input").GetComponent<InputField>().characterValidation =
                            InputField.CharacterValidation.None;
                        SetProperties(go.GetComponent<ConfigBoundString>(), entry);
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
                        conf.Register();
                        conf.SetData(mod.Value.Info.Metadata.GUID, entry.Key.Section, entry.Key.Key);
                        conf.Input.characterValidation = InputField.CharacterValidation.None;
                        conf.Input.contentType = InputField.ContentType.Alphanumeric;
                        SetProperties(conf, entry);
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
            foreach (Transform config in Configs)
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

                    var childKeyCode = values.gameObject.GetComponent<ConfigBoundKeyCode>();
                    if (childKeyCode != null)
                    {
                        childKeyCode.WriteBack();
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
        ///     Set the properties of the <see cref="ConfigBound{T}" />
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="binding"></param>
        /// <param name="entry"></param>
        private static void SetProperties<T>(ConfigBound<T> binding, KeyValuePair<ConfigDefinition, ConfigEntryBase> entry)
        {
            var configurationManagerAttribute =
                (ConfigurationManagerAttributes)entry.Value.Description.Tags.FirstOrDefault(x => x is ConfigurationManagerAttributes);

            // Only act, if we have a valid ConfigurationManagerAttributes tag
            if (configurationManagerAttribute != null)
            {
                binding.SetReadOnly(configurationManagerAttribute.ReadOnly == true);

                // Disable the input field if it is a synchronizable and not unlocked
                if (configurationManagerAttribute.IsAdminOnly && !configurationManagerAttribute.IsUnlocked)
                {
                    binding.SetEnabled(false);
                }
                else
                {
                    binding.SetEnabled(true);
                }

                // and set it's default value
                binding.Default = (T)entry.Value.DefaultValue;
            }

            // Set clamp
            binding.Clamp = entry.Value.Description.AcceptableValues;

            // set the value from the configuration
            binding.Value = binding.GetValueFromConfig();
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
        /// <param name="modguid">module GUID</param>
        /// <param name="section">section</param>
        /// <param name="key">key</param>
        /// <param name="width">width</param>
        /// <returns></returns>
        private static GameObject CreateToggleElement(Transform parent, string labelname, Color labelColor, string description, Color descriptionColor,
            string modguid, string section, string key, float width)
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

            // Bind to config entry
            result.AddComponent<ConfigBoundBoolean>().SetData(modguid, section, key);

            return result;
        }

        /// <summary>
        ///     Create a keybinding element
        /// </summary>
        /// <param name="parent">parent transform</param>
        /// <param name="labelname">label text</param>
        /// <param name="description">description text</param>
        /// <param name="modguid">module GUID</param>
        /// <param name="section">section</param>
        /// <param name="key">key</param>
        /// ´<param name="buttonName">buttonName</param>
        /// <param name="width">width</param>
        /// <returns></returns>
        private static GameObject CreateKeybindElement(Transform parent, string labelname, string description, string modguid, string section, string key,
            string buttonName, float width)
        {
            // Create label and keybind button
            var result = GUIManager.Instance.CreateKeyBindField(labelname, parent, width, 0);

            // Add this keybinding to the list in Settings to utilize valheim's keybind dialog
            Settings.instance.m_keys.Add(new Settings.KeySetting
            {
                //m_keyName = $"{buttonName}!{modguid}", m_keyTransform = result.GetComponent<RectTransform>()
                m_keyName = buttonName,
                m_keyTransform = result.GetComponent<RectTransform>()
            });

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

            // and add the config binding
            result.AddComponent<ConfigBoundKeyCode>().SetData(modguid, section, key);

            return result;
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

            public AcceptableValueBase Clamp { get; set; }

            public T Default { get; set; }

            public T Value
            {
                get => GetValue();
                set => SetValue(value);
            }

            internal abstract T GetValueFromConfig();

            public abstract void SetValueInConfig(T value);

            public abstract T GetValue();
            internal abstract void SetValue(T value);


            public void WriteBack()
            {
                SetValueInConfig(GetValue());
            }

            public void SetData(string modGuid, string section, string key)
            {
                ModGUID = modGuid;
                Section = section;
                Key = key;
                var value = GetValueFromConfig();

                SetValue(value);
            }

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
                    var value = GetValue();
                    return Clamp.IsValid(value);
                }

                return true;
            }
        }

        /// <summary>
        ///     Boolean Binding
        /// </summary>
        internal class ConfigBoundBoolean : ConfigBound<bool>
        {
            internal override bool GetValueFromConfig()
            {
                var pluginConfig = BepInExUtils.GetDependentPlugins(true).First(x => x.Key == ModGUID).Value.Config;
                var entry = pluginConfig[Section, Key];
                return (bool)entry.BoxedValue;
            }

            public override void SetValueInConfig(bool value)
            {
                var pluginConfig = BepInExUtils.GetDependentPlugins(true).First(x => x.Key == ModGUID).Value.Config;
                var entry = pluginConfig[Section, Key] as ConfigEntry<bool>;
                entry.Value = value;
            }

            public override bool GetValue()
            {
                return gameObject.transform.Find("Toggle").GetComponent<Toggle>().isOn;
            }

            internal override void SetValue(bool value)
            {
                gameObject.transform.Find("Toggle").GetComponent<Toggle>().isOn = value;
            }

            public override void SetEnabled(bool enabled)
            {
                gameObject.transform.Find("Toggle").GetComponent<Toggle>().enabled = enabled;
            }

            public override void SetReadOnly(bool readOnly)
            {
                gameObject.transform.Find("Toggle").GetComponent<Toggle>().enabled = !readOnly;
            }
        }

        /// <summary>
        ///     Integer binding
        /// </summary>
        internal class ConfigBoundInt : ConfigBound<int>
        {
            internal override int GetValueFromConfig()
            {
                var pluginConfig = BepInExUtils.GetDependentPlugins(true).First(x => x.Key == ModGUID).Value.Config;
                var entry = pluginConfig[Section, Key];
                return (int)entry.BoxedValue;
            }

            public override void SetValueInConfig(int value)
            {
                var pluginConfig = BepInExUtils.GetDependentPlugins(true).First(x => x.Key == ModGUID).Value.Config;
                var entry = pluginConfig[Section, Key] as ConfigEntry<int>;
                entry.Value = value;
            }

            public override int GetValue()
            {
                int temp;
                var text = gameObject.transform.Find("Input").GetComponent<InputField>();
                if (!int.TryParse(text.text, out temp))
                {
                    temp = Default;
                }

                return temp;
            }

            internal override void SetValue(int value)
            {
                gameObject.transform.Find("Input").GetComponent<InputField>().text = value.ToString();
            }

            public override void SetEnabled(bool enabled)
            {
                gameObject.transform.Find("Input").GetComponent<InputField>().enabled = enabled;
            }

            public override void SetReadOnly(bool readOnly)
            {
                gameObject.transform.Find("Input").GetComponent<InputField>().readOnly = readOnly;
                gameObject.transform.Find("Input").GetComponent<InputField>().textComponent.color = readOnly ? Color.grey : Color.white;
            }
        }

        /// <summary>
        ///     Float binding
        /// </summary>
        internal class ConfigBoundFloat : ConfigBound<float>
        {
            internal override float GetValueFromConfig()
            {
                var pluginConfig = BepInExUtils.GetDependentPlugins(true).First(x => x.Key == ModGUID).Value.Config;
                var entry = pluginConfig[Section, Key];
                return (float)entry.BoxedValue;
            }

            public override void SetValueInConfig(float value)
            {
                var pluginConfig = BepInExUtils.GetDependentPlugins(true).First(x => x.Key == ModGUID).Value.Config;
                var entry = pluginConfig[Section, Key] as ConfigEntry<float>;
                entry.Value = value;
            }

            public override float GetValue()
            {
                float temp;
                var text = gameObject.transform.Find("Input").GetComponent<InputField>();
                if (!float.TryParse(text.text, NumberStyles.Number, CultureInfo.CurrentCulture.NumberFormat, out temp))
                {
                    temp = Default;
                }

                return temp;
            }

            internal override void SetValue(float value)
            {
                gameObject.transform.Find("Input").GetComponent<InputField>().text = value.ToString("F3");
            }

            public override void SetEnabled(bool enabled)
            {
                gameObject.transform.Find("Input").GetComponent<InputField>().enabled = enabled;
            }

            public override void SetReadOnly(bool readOnly)
            {
                gameObject.transform.Find("Input").GetComponent<InputField>().readOnly = readOnly;
                gameObject.transform.Find("Input").GetComponent<InputField>().textComponent.color = readOnly ? Color.grey : Color.white;
            }
        }

        /// <summary>
        ///     Double binding
        /// </summary>
        internal class ConfigBoundDouble : ConfigBound<double>
        {
            internal override double GetValueFromConfig()
            {
                var pluginConfig = BepInExUtils.GetDependentPlugins(true).First(x => x.Key == ModGUID).Value.Config;
                var entry = pluginConfig[Section, Key];
                return (double)entry.BoxedValue;
            }

            public override void SetValueInConfig(double value)
            {
                var pluginConfig = BepInExUtils.GetDependentPlugins(true).First(x => x.Key == ModGUID).Value.Config;
                var entry = pluginConfig[Section, Key] as ConfigEntry<double>;
                entry.Value = value;
            }

            public override double GetValue()
            {
                double temp;
                var text = gameObject.transform.Find("Input").GetComponent<InputField>();
                if (!double.TryParse(text.text, NumberStyles.Number, CultureInfo.CurrentCulture.NumberFormat, out temp))
                {
                    temp = Default;
                }

                return temp;
            }

            internal override void SetValue(double value)
            {
                gameObject.transform.Find("Input").GetComponent<InputField>().text = value.ToString("F3");
            }

            public override void SetEnabled(bool enabled)
            {
                gameObject.transform.Find("Input").GetComponent<InputField>().enabled = enabled;
            }

            public override void SetReadOnly(bool readOnly)
            {
                gameObject.transform.Find("Input").GetComponent<InputField>().readOnly = readOnly;
                gameObject.transform.Find("Input").GetComponent<InputField>().textComponent.color = readOnly ? Color.grey : Color.white;
            }
        }

        /// <summary>
        ///     KeyCode binding
        /// </summary>
        internal class ConfigBoundKeyCode : ConfigBound<KeyCode>
        {
            internal override KeyCode GetValueFromConfig()
            {
                var pluginConfig = BepInExUtils.GetDependentPlugins(true).First(x => x.Key == ModGUID).Value.Config;
                var entry = pluginConfig[Section, Key];
                return (KeyCode)entry.BoxedValue;
            }

            public override void SetValueInConfig(KeyCode value)
            {
                var pluginConfig = BepInExUtils.GetDependentPlugins(true).First(x => x.Key == ModGUID).Value.Config;
                var entry = pluginConfig[Section, Key] as ConfigEntry<KeyCode>;
                entry.Value = value;
            }

            public override KeyCode GetValue()
            {
                // TODO: Get and parse value from input field
                var pluginConfig = BepInExUtils.GetDependentPlugins(true).First(x => x.Key == ModGUID).Value.Config;
                var entry = pluginConfig[Section, Key];
                var temp = KeyCode.None;
                if (Enum.TryParse(gameObject.transform.Find("Button/Text").GetComponent<Text>().text, out temp))
                {
                    return temp;
                }

                Logger.LogError($"Error parsing Keycode {gameObject.transform.Find("Button/Text").GetComponent<Text>().text}");
                return temp;
            }

            internal override void SetValue(KeyCode value)
            {
                var pluginConfig = BepInExUtils.GetDependentPlugins(true).First(x => x.Key == ModGUID).Value.Config;
                var entry = pluginConfig[Section, Key];
                var buttonName = entry.GetBoundButtonName();
                gameObject.transform.Find("Button/Text").GetComponent<Text>().text = value.ToString();
            }

            public void Awake()
            {
                var pluginConfig = BepInExUtils.GetDependentPlugins(true).First(x => x.Key == ModGUID).Value.Config;
                var entry = pluginConfig[Section, Key];
                var buttonName = entry.GetBoundButtonName();
                gameObject.transform.Find("Button").GetComponent<Button>().onClick.AddListener(() =>
                {
                    Settings.instance.OpenBindDialog(buttonName);
                });
            }

            public override void SetEnabled(bool enabled)
            {
                gameObject.transform.Find("Button").GetComponent<Button>().enabled = enabled;
            }

            public override void SetReadOnly(bool readOnly)
            {
                gameObject.transform.Find("Button").GetComponent<Button>().enabled &= readOnly;
                gameObject.transform.Find("Button/Text").GetComponent<Text>().color = readOnly ? Color.grey : Color.white;
            }
        }

        /// <summary>
        ///     String binding
        /// </summary>
        internal class ConfigBoundString : ConfigBound<string>
        {
            internal override string GetValueFromConfig()
            {
                var pluginConfig = BepInExUtils.GetDependentPlugins(true).First(x => x.Key == ModGUID).Value.Config;
                var entry = pluginConfig[Section, Key];
                return (string)entry.BoxedValue;
            }

            public override void SetValueInConfig(string value)
            {
                var pluginConfig = BepInExUtils.GetDependentPlugins(true).First(x => x.Key == ModGUID).Value.Config;
                var entry = pluginConfig[Section, Key] as ConfigEntry<string>;
                entry.Value = value;
            }

            public override string GetValue()
            {
                return gameObject.transform.Find("Input").GetComponent<InputField>().text;
            }

            internal override void SetValue(string value)
            {
                gameObject.transform.Find("Input").GetComponent<InputField>().text = value;
            }

            public override void SetEnabled(bool enabled)
            {
                gameObject.transform.Find("Input").GetComponent<InputField>().enabled = enabled;
            }

            public override void SetReadOnly(bool readOnly)
            {
                gameObject.transform.Find("Input").GetComponent<InputField>().readOnly = readOnly;
                gameObject.transform.Find("Input").GetComponent<InputField>().textComponent.color = readOnly ? Color.grey : Color.white;
            }
        }

        internal class ConfigBoundColor : ConfigBound<Color>
        {
            internal InputField Input;
            internal Button Button;
            internal Image Image;

            internal void Register()
            {
                Input = gameObject.transform.Find("Layout/Input").GetComponent<InputField>();
                Input.onEndEdit.AddListener(SetButtonColor);
                Button = gameObject.transform.Find("Layout/Button").GetComponent<Button>();
                Button.onClick.AddListener(ShowColorPicker);
                Image = gameObject.transform.Find("Layout/Button").GetComponent<Image>();
            }

            internal override Color GetValueFromConfig()
            {
                var pluginConfig = BepInExUtils.GetDependentPlugins(true).First(x => x.Key == ModGUID).Value.Config;
                var entry = pluginConfig[Section, Key] as ConfigEntry<Color>;
                return (Color)entry.BoxedValue;
            }

            public override void SetValueInConfig(Color value)
            {
                var pluginConfig = BepInExUtils.GetDependentPlugins(true).First(x => x.Key == ModGUID).Value.Config;
                var entry = pluginConfig[Section, Key] as ConfigEntry<Color>;
                entry.Value = value;
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
                    var pluginConfig = BepInExUtils.GetDependentPlugins(true).First(x => x.Key == ModGUID).Value.Config;
                    var entry = pluginConfig[Section, Key] as ConfigEntry<Color>;
                    Logger.LogWarning($"Using default value ({(Color)entry.DefaultValue}) instead.");
                    return (Color)entry.DefaultValue;
                }
            }

            internal override void SetValue(Color value)
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
