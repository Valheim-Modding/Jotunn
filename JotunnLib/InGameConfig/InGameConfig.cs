// JotunnLib
// a Valheim mod
// 
// File:    InGameConfig.cs
// Project: JotunnLib

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Jotunn.InGameConfig
{
    /// <summary>
    ///     An ingame GUI for BepInEx config files
    /// </summary>
    public class InGameConfig
    {
        /// <summary>
        ///     Cached root gameobject of the settings panel
        /// </summary>
        private static GameObject settingsRoot;

        /// <summary>
        ///     Cached root of our additional tab
        /// </summary>
        private static GameObject configTab;

        internal static ConfigBoundKeyCode keyInBinding;

        /// <summary>
        ///    Cache keybinds 
        /// </summary>
        internal static Dictionary<string, List<Tuple<string, ConfigDefinition, ConfigEntryBase>>> configurationKeybindings = new Dictionary<string, List<Tuple<string, ConfigDefinition, ConfigEntryBase>>>();

        /// <summary>
        ///     Hook into settings setup
        /// </summary>
        [PatchInit(0)]
        public static void HookOnSettings()
        {
            On.FejdStartup.OnButtonSettings += FejdStartup_OnButtonSettings;
            On.Menu.OnSettings += Menu_OnSettings;
            On.ZInput.EndBindKey += ZInput_EndBindKey;
        }

        private static bool ZInput_EndBindKey(On.ZInput.orig_EndBindKey orig, ZInput self)
        {
            bool result = orig(self);
            if (result && ZInput.m_binding != null)
            {
                keyInBinding.SetOtherButtons(ZInput.m_binding.m_key);
            }

            return result;
        }

        // Create our tab and cache configuration values for later synchronization
        private static void Menu_OnSettings(On.Menu.orig_OnSettings orig, Menu self)
        {
            orig(self);
            settingsRoot = self.m_settingsInstance;

            SynchronizationManager.Instance.CacheConfigurationValues();
            CreateModConfigTab();
        }

        // Create our tab
        private static void FejdStartup_OnButtonSettings(On.FejdStartup.orig_OnButtonSettings orig, FejdStartup self)
        {
            // we don't need to call orig here, only thing it did, was to instantiate the settings prefab (but without saving the reference)
            settingsRoot = Object.Instantiate(self.m_settingsPrefab, self.transform);

            CreateModConfigTab();
        }

        /// <summary>
        ///     Create custom configuration tab
        /// </summary>
        private static void CreateModConfigTab()
        {
            var anyConfig = BepInExUtils.GetDependentPlugins(true).Any(x => GetConfigurationEntries(x.Value).GroupBy(x => x.Key.Section).Any());

            if (!anyConfig)
            {
                return;
            }

            // Hook SaveSettings to be notified when OK was pressed
            On.Settings.SaveSettings += Settings_SaveSettings;

            // Copy the Misc tab button
            var tabButtonCopy = Object.Instantiate(settingsRoot.transform.Find("panel/TabButtons/Misc").gameObject,
                settingsRoot.transform.Find("panel/TabButtons"));

            var tabHandler = settingsRoot.transform.Find("panel/TabButtons").GetComponent<TabHandler>();

            // and set it's new property values
            tabButtonCopy.name = "ModConfig";
            tabButtonCopy.transform.Find("Text").GetComponent<Text>().text = "ModConfig";
            tabButtonCopy.transform.Find("Selected/Text (1)").GetComponent<Text>().text = "ModConfig";

            // Rearrange/center settings tab buttons (Valheim had an offset to the right.....)
            var numChildren = settingsRoot.transform.Find("panel/TabButtons").childCount;
            var width = settingsRoot.transform.Find("panel/TabButtons").GetComponent<RectTransform>().rect.width;
            for (var i = 0; i < numChildren; i++)
            {
                settingsRoot.transform.Find("panel/TabButtons").GetChild(i).GetComponent<RectTransform>().anchoredPosition = new Vector2(
                    ((width - (numChildren * 100f)) / 2f) + (i * 100f) + 50f,
                    settingsRoot.transform.Find("panel/TabButtons").GetChild(i).GetComponent<RectTransform>().anchoredPosition.y);
            }


            // Add the tab 

            var tab = settingsRoot.transform.Find("panel/Tabs").gameObject;

            // Create the content scroll view
            configTab = GUIManager.Instance.CreateScrollView(tab.transform, false, true, 8f, 10f, GUIManager.Instance.ValheimScrollbarHandleColorBlock,
                    new Color(0, 0, 0, 1), tab.GetComponent<RectTransform>().rect.width - 50f, tab.GetComponent<RectTransform>().rect.height - 50f)
                .SetMiddleCenter();

            configTab.name = "ModConfig";

            // configure the ui group handler
            var groupHandler = configTab.AddComponent<UIGroupHandler>();
            groupHandler.m_groupPriority = 10;
            groupHandler.m_canvasGroup = configTab.GetComponent<CanvasGroup>();
            groupHandler.m_canvasGroup.ignoreParentGroups = true;
            groupHandler.m_canvasGroup.blocksRaycasts = true;
            groupHandler.Update();

            // create ok and back button (just copy them from Controls tab)
            var ok = Object.Instantiate(settingsRoot.transform.Find("panel/Tabs/Controls/Ok").gameObject, configTab.transform);
            ok.GetComponent<RectTransform>().anchoredPosition = ok.GetComponent<RectTransform>().anchoredPosition - new Vector2(0, 25f);
            ok.GetComponent<Button>().onClick.AddListener(() =>
            {
                Settings.instance.OnOk();

                // After applying ingame values, lets synchronize any changed (and unlocked) values
                SynchronizationManager.Instance.SynchronizeToServer();

                // remove reference to gameobject
                settingsRoot = null;
                configTab = null;
            });

            var back = Object.Instantiate(settingsRoot.transform.Find("panel/Tabs/Controls/Back").gameObject, configTab.transform);
            back.GetComponent<RectTransform>().anchoredPosition = back.GetComponent<RectTransform>().anchoredPosition - new Vector2(0, 25f);
            back.GetComponent<Button>().onClick.AddListener(() =>
            {
                Settings.instance.OnBack();

                // remove reference to gameobject
                settingsRoot = null;
                configTab = null;
            });

            // initially hide the configTab
            configTab.SetActive(false);

            // Add a new Tab to the TabHandler
            var newTab = new TabHandler.Tab();
            newTab.m_default = false;
            newTab.m_button = tabButtonCopy.GetComponent<Button>();
            newTab.m_page = configTab.GetComponent<RectTransform>();
            newTab.m_onClick = new UnityEvent();
            newTab.m_onClick.AddListener(() =>
            {
                configTab.GetComponent<UIGroupHandler>().SetActive(true);
                configTab.SetActive(true);
                configTab.transform.Find("Scroll View").GetComponent<ScrollRect>().normalizedPosition = new Vector2(0, 1);
            });

            // Add the onClick of the tabhandler to the tab button
            tabButtonCopy.GetComponent<Button>().onClick.AddListener(() => tabHandler.OnClick(newTab.m_button));

            // and add the new Tab to the tabs list
            tabHandler.m_tabs.Add(newTab);

            var innerWidth = configTab.GetComponent<RectTransform>().rect.width - 25f;

            // Reset keybinding cache
            configurationKeybindings.Clear();

            foreach (var mod in BepInExUtils.GetDependentPlugins(true))
            {
                foreach (var kv in GetConfigurationEntries(mod.Value).Where(x => x.Value.IsVisible() && x.Value.IsButtonBound()))
                {
                    var buttonName = kv.Value.GetBoundButtonName();
                    if (!string.IsNullOrEmpty(buttonName))
                    {
                        if (!configurationKeybindings.ContainsKey(buttonName))
                        {
                            configurationKeybindings.Add(buttonName, new List<Tuple<string, ConfigDefinition, ConfigEntryBase>>());
                        }

                        configurationKeybindings[buttonName].Add(new Tuple<string, ConfigDefinition, ConfigEntryBase>(mod.Key, kv.Key, kv.Value));
                    }
                }
            }

            // Iterate over all dependent plugins (including Jotunn itself)
            foreach (var mod in BepInExUtils.GetDependentPlugins(true))
            {
                // Create a header if there are any relevant configuration entries
                if (GetConfigurationEntries(mod.Value).Where(x => x.Value.IsVisible()).GroupBy(x => x.Key.Section).Any())
                {
                    // Create module header Text element
                    var text = GUIManager.Instance.CreateText(mod.Key, configTab.transform.Find("Scroll View/Viewport/Content"), new Vector2(0.5f, 0.5f),
                        new Vector2(0.5f, 0.5f), new Vector2(0, 0), GUIManager.Instance.AveriaSerifBold, 20, Color.white, true, Color.black,
                        configTab.GetComponent<RectTransform>().rect.width, 50, false);
                    text.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
                    text.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
                    text.AddComponent<LayoutElement>().preferredHeight = 40f;
                }

                // Iterate over all configuration entries (grouped by their sections)
                foreach (var kv in GetConfigurationEntries(mod.Value).Where(x => x.Value.IsVisible()).GroupBy(x => x.Key.Section))
                {
                    // Create section header Text element
                    var sectiontext = GUIManager.Instance.CreateText("Section " + kv.Key, configTab.transform.Find("Scroll View/Viewport/Content"),
                        new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), GUIManager.Instance.AveriaSerifBold, 16,
                        GUIManager.Instance.ValheimOrange, true, Color.black, configTab.GetComponent<RectTransform>().rect.width, 30, false);
                    sectiontext.SetMiddleCenter();
                    sectiontext.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
                    sectiontext.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
                    sectiontext.AddComponent<LayoutElement>().preferredHeight = 30f;

                    // Iterate over all entries of this section
                    foreach (var entry in kv.OrderByDescending(x =>
                    {
                        if (x.Value.Description.Tags.FirstOrDefault(y => y is ConfigurationManagerAttributes) is ConfigurationManagerAttributes cma)
                        {
                            return cma.Order ?? 0;
                        }

                        return 0;
                    }).ThenBy(x => x.Key.Key))
                    {
                        // Create config entry
                        // switch by type
                        var entryAttributes =
                            entry.Value.Description.Tags.FirstOrDefault(x => x is ConfigurationManagerAttributes) as ConfigurationManagerAttributes;
                        if (entryAttributes == null)
                        {
                            entryAttributes = new ConfigurationManagerAttributes();
                        }

                        if (entry.Value.SettingType == typeof(bool))
                        {
                            // Create toggle element
                            var go = CreateToggleElement(configTab.transform.Find("Scroll View/Viewport/Content"), entry.Key.Key + ":",
                                entryAttributes.EntryColor, entry.Value.Description.Description + (entryAttributes.IsAdminOnly ? "\n(Server side setting)" : ""), entryAttributes.DescriptionColor, mod.Value.Info.Metadata.GUID,
                                entry.Key.Section, entry.Key.Key, innerWidth);
                            SetProperties(go.GetComponent<ConfigBoundBoolean>(), entry);
                        }
                        else if (entry.Value.SettingType == typeof(int))
                        {
                            var description = entry.Value.Description.Description;
                            if (entry.Value.Description.AcceptableValues != null)
                            {
                                description += Environment.NewLine + "(" +
                                               entry.Value.Description.AcceptableValues.ToDescriptionString().TrimStart('#').Trim() + ")";
                            }

                            // Create input field int
                            var go = CreateTextInputField(configTab.transform.Find("Scroll View/Viewport/Content"), entry.Key.Key + ":",
                                entryAttributes.EntryColor, description + (entryAttributes.IsAdminOnly ? "\n(Server side setting)" : ""), entryAttributes.DescriptionColor, mod.Value.Info.Metadata.GUID, entry.Key.Section,
                                entry.Key.Key, innerWidth);
                            go.AddComponent<ConfigBoundInt>().SetData(mod.Value.Info.Metadata.GUID, entry.Key.Section, entry.Key.Key);
                            go.transform.Find("Input").GetComponent<InputField>().characterValidation = InputField.CharacterValidation.Integer;
                            SetProperties(go.GetComponent<ConfigBoundInt>(), entry);
                            go.transform.Find("Input").GetComponent<InputField>().onValueChanged.AddListener(x =>
                            {
                                go.transform.Find("Input").GetComponent<InputField>().textComponent.color =
                                    go.GetComponent<ConfigBoundInt>().IsValid() ? Color.white : Color.red;
                            });
                        }
                        else if (entry.Value.SettingType == typeof(float))
                        {
                            var description = entry.Value.Description.Description;
                            if (entry.Value.Description.AcceptableValues != null)
                            {
                                description += Environment.NewLine + "(" +
                                               entry.Value.Description.AcceptableValues.ToDescriptionString().TrimStart('#').Trim() + ")";
                            }

                            // Create input field float
                            var go = CreateTextInputField(configTab.transform.Find("Scroll View/Viewport/Content"), entry.Key.Key + ":",
                                entryAttributes.EntryColor, description + (entryAttributes.IsAdminOnly ? "\n(Server side setting)" : ""), entryAttributes.DescriptionColor, mod.Value.Info.Metadata.GUID, entry.Key.Section,
                                entry.Key.Key, innerWidth);
                            go.AddComponent<ConfigBoundFloat>().SetData(mod.Value.Info.Metadata.GUID, entry.Key.Section, entry.Key.Key);
                            go.transform.Find("Input").GetComponent<InputField>().characterValidation = InputField.CharacterValidation.Decimal;
                            SetProperties(go.GetComponent<ConfigBoundFloat>(), entry);
                            go.transform.Find("Input").GetComponent<InputField>().onValueChanged.AddListener(x =>
                            {
                                go.transform.Find("Input").GetComponent<InputField>().textComponent.color =
                                    go.GetComponent<ConfigBoundFloat>().IsValid() ? Color.white : Color.red;
                            });
                        }
                        else if (entry.Value.SettingType == typeof(KeyCode))
                        {
                            // Create key binder
                            string buttonName = entry.Value.GetBoundButtonName();
                            string buttonText = $"{entry.Value.Description.Description}{Environment.NewLine}This key is bound to button '{buttonName}'.";
                            if (!string.IsNullOrEmpty(buttonName))
                            {
                                string duplicateKeybindingText = "";
                                if (configurationKeybindings[entry.Value.GetBoundButtonName()].Count > 1)
                                {
                                    duplicateKeybindingText += $"{Environment.NewLine}Other mods using this button: {Environment.NewLine}";
                                    foreach (var buttons in configurationKeybindings[buttonName])
                                    {
                                        // If it is the same config entry, just skip it
                                        if ((buttons.Item2 == entry.Key) && (buttons.Item1 == mod.Key))
                                        {
                                            continue;
                                        }

                                        // Set others to the first's value
                                        buttons.Item3.BoxedValue = entry.Value.BoxedValue;
                                        // Add modguid as text
                                        duplicateKeybindingText += $"{buttons.Item1}, ";
                                    }

                                    // add to buttonText, but without last ', '
                                    buttonText += duplicateKeybindingText.Trim(' ').TrimEnd(',');
                                }
                            }

                            var go = CreateKeybindElement(configTab.transform.Find("Scroll View/Viewport/Content"), entry.Key.Key + ":",
                                buttonText, mod.Value.Info.Metadata.GUID, entry.Key.Section, entry.Key.Key, buttonName, innerWidth);
                            go.GetComponent<ConfigBoundKeyCode>().SetData(mod.Value.Info.Metadata.GUID, entry.Key.Section, entry.Key.Key);
                            SetProperties(go.GetComponent<ConfigBoundKeyCode>(), entry);
                        }
                        else if (entry.Value.SettingType == typeof(string))
                        {
                            // Create input field string
                            var go = CreateTextInputField(configTab.transform.Find("Scroll View/Viewport/Content"), entry.Key.Key + ":",
                                entryAttributes.EntryColor, entry.Value.Description.Description + (entryAttributes.IsAdminOnly ? "\n(Server side setting)" : ""), entryAttributes.DescriptionColor, mod.Value.Info.Metadata.GUID,
                                entry.Key.Section, entry.Key.Key, innerWidth);
                            go.AddComponent<ConfigBoundString>().SetData(mod.Value.Info.Metadata.GUID, entry.Key.Section, entry.Key.Key);
                            go.transform.Find("Input").GetComponent<InputField>().characterValidation = InputField.CharacterValidation.None;
                            SetProperties(go.GetComponent<ConfigBoundString>(), entry);
                        }
                        else if (entry.Value.SettingType == typeof(Color))
                        {
                            // Create input field string
                            var go = CreateTextInputField(configTab.transform.Find("Scroll View/Viewport/Content"), entry.Key.Key + ":",
                                entryAttributes.EntryColor, entry.Value.Description.Description + (entryAttributes.IsAdminOnly ? "\n(Server side setting)" : ""), entryAttributes.DescriptionColor, mod.Value.Info.Metadata.GUID,
                                entry.Key.Section, entry.Key.Key, innerWidth);
                            go.AddComponent<ConfigBoundColor>().SetData(mod.Value.Info.Metadata.GUID, entry.Key.Section, entry.Key.Key);
                            go.transform.Find("Input").GetComponent<InputField>().characterValidation = InputField.CharacterValidation.None;
                            go.transform.Find("Input").GetComponent<InputField>().contentType = InputField.ContentType.Alphanumeric;
                            SetProperties(go.GetComponent<ConfigBoundColor>(), entry);
                        }
                    }
                }
            }
        }


        /// <summary>
        ///     SaveSettings Hook
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private static void Settings_SaveSettings(On.Settings.orig_SaveSettings orig, Settings self)
        {
            orig(self);

            // Save our config values
            var numChildren = configTab.transform.Find("Scroll View/Viewport/Content").childCount;

            // Just iterate over the children in the scroll view and act if we find a ConfigBound<T> component
            for (var i = 0; i < numChildren; i++)
            {
                var childBoolean = configTab.transform.Find("Scroll View/Viewport/Content").GetChild(i).gameObject.GetComponent<ConfigBoundBoolean>();
                if (childBoolean != null)
                {
                    childBoolean.WriteBack();
                    continue;
                }

                var childInt = configTab.transform.Find("Scroll View/Viewport/Content").GetChild(i).gameObject.GetComponent<ConfigBoundInt>();
                if (childInt != null)
                {
                    childInt.WriteBack();
                    continue;
                }

                var childFloat = configTab.transform.Find("Scroll View/Viewport/Content").GetChild(i).gameObject.GetComponent<ConfigBoundFloat>();
                if (childFloat != null)
                {
                    childFloat.WriteBack();
                    continue;
                }

                var childKeyCode = configTab.transform.Find("Scroll View/Viewport/Content").GetChild(i).gameObject.GetComponent<ConfigBoundKeyCode>();
                if (childKeyCode != null)
                {
                    childKeyCode.WriteBack();
                    continue;
                }

                var childString = configTab.transform.Find("Scroll View/Viewport/Content").GetChild(i).gameObject.GetComponent<ConfigBoundString>();
                if (childString != null)
                {
                    childString.WriteBack();
                }

                var childColor = configTab.transform.Find("Scroll View/Viewport/Content").GetChild(i).gameObject.GetComponent<ConfigBoundColor>();
                if (childColor != null)
                {
                    childColor.WriteBack();
                }
            }

            // Remove hook again until the next time
            On.Settings.SaveSettings -= Settings_SaveSettings;
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
                if (configurationManagerAttribute.IsAdminOnly && !configurationManagerAttribute.UnlockSetting)
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
        /// <param name="description">Description text</param>
        /// <param name="guid">module GUID</param>
        /// <param name="section">Section</param>
        /// <param name="key">Key</param>
        /// <param name="width">Width</param>
        /// <returns></returns>
        private static GameObject CreateTextInputField(Transform parent, string labelname, Color labelColor, string description, Color descriptionColor,
            string guid, string section, string key, float width)
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
            desc.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -(label.GetHeight() + 3f));
            desc.SetToTextHeight();

            // calculate combined height
            result.SetHeight(label.GetHeight() + 3f + desc.GetHeight() + 15f);

            // Add the input field element
            var field = new GameObject("Input", typeof(RectTransform), typeof(Image), typeof(InputField)).SetUpperRight().SetSize(140f, label.GetHeight() + 6f);
            field.GetComponent<Image>().sprite = GUIManager.Instance.GetSprite("text_field");
            field.GetComponent<Image>().type = Image.Type.Sliced;
            field.transform.SetParent(result.transform, false);

            var inputField = field.GetComponent<InputField>();

            var text = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Outline)).SetMiddleLeft().SetHeight(label.GetHeight() + 6f)
                .SetWidth(130f);
            inputField.textComponent = text.GetComponent<Text>();
            text.transform.SetParent(field.transform, false);
            text.GetComponent<RectTransform>().anchoredPosition = new Vector2(5, 0);
            text.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
            text.GetComponent<Text>().font = GUIManager.Instance.AveriaSerifBold;

            // create the placeholder element
            var placeholder = new GameObject("Placeholder", typeof(RectTransform), typeof(Text)).SetMiddleLeft().SetHeight(label.GetHeight() + 6f)
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
        ///     Get all config entries of a module
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        private static IEnumerable<KeyValuePair<ConfigDefinition, ConfigEntryBase>> GetConfigurationEntries(BaseUnityPlugin module)
        {
            var enumerator = module.Config.GetEnumerator();
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }

        /// <summary>
        ///     Create a toggle element
        /// </summary>
        /// <param name="parent">parent transform</param>
        /// <param name="labelname">label text</param>
        /// <param name="description">description text</param>
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
            GUIManager.Instance.CreateToggle(result.transform, new Vector2(0, 0), 28f, 28f).SetUpperRight();

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
            desc.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -(result.transform.Find("Text").gameObject.GetHeight() + 3f));
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
        /// <param name="width">width</param>
        /// <returns></returns>
        private static GameObject CreateKeybindElement(Transform parent, string labelname, string description, string modguid, string section, string key, string buttonName,
            float width)
        {
            // Create label and keybind button
            var result = GUIManager.Instance.CreateKeyBindField(labelname, parent, width, 0);

            // Add this keybinding to the list in Settings to utilize valheim's keybind dialog
            Settings.instance.m_keys.Add(new Settings.KeySetting { m_keyName = $"{buttonName}!{modguid}", m_keyTransform = result.GetComponent<RectTransform>() });

            // Create description text
            var idx = 0;
            Vector2 lastPosition = new Vector2(0, -result.GetComponent<RectTransform>().rect.height - 3f);
            GameObject desc = null;
            foreach (var part in description.Split(Environment.NewLine[0]))
            {
                string p2 = part.Trim();
                desc = GUIManager.Instance.CreateText(p2, result.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0),
                    GUIManager.Instance.AveriaSerifBold, 12, Color.white, true, Color.black, width - 150f, 0, false);
                desc.name = $"Description{idx}";
                desc.SetUpperLeft().SetToTextHeight();

                desc.GetComponent<RectTransform>().anchoredPosition = lastPosition;
                lastPosition = new Vector2(0, lastPosition.y - desc.GetHeight() - 3);

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
                string buttonName = entry.GetBoundButtonName();
                gameObject.transform.Find("Button/Text").GetComponent<Text>().text = value.ToString();
            }

            public void Awake()
            {
                var pluginConfig = BepInExUtils.GetDependentPlugins(true).First(x => x.Key == ModGUID).Value.Config;
                var entry = pluginConfig[Section, Key];
                string buttonName = entry.GetBoundButtonName();
                gameObject.transform.Find("Button").GetComponent<Button>().onClick.AddListener(() =>
                {
                    InGameConfig.keyInBinding = this;
                    Settings.instance.OpenBindDialog(buttonName+"!"+ModGUID);
                });
            }

            internal void SetOtherButtons(KeyCode newKeyCode)
            {
                var pluginConfig = BepInExUtils.GetDependentPlugins(true).First(x => x.Key == ModGUID).Value.Config;
                var entry = pluginConfig[Section, Key];
                string buttonName = entry.GetBoundButtonName();

                if (configurationKeybindings.ContainsKey(buttonName))
                {
                    foreach (var button in configurationKeybindings[buttonName])
                    {
                        var cbkc = gameObject.transform.parent.gameObject.GetComponentsInChildren<ConfigBoundKeyCode>().FirstOrDefault(x =>
                            x.ModGUID == button.Item1 && x.Section == button.Item2.Section && x.Key == button.Item2.Key);
                        if (cbkc != null)
                        {
                            ZInput.instance.m_buttons[$"{buttonName}!{cbkc.ModGUID}"].m_key = newKeyCode;
                        }
                    }

                    Settings.instance.UpdateBindings();
                }
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
                var col = gameObject.transform.Find("Input").GetComponent<InputField>().text;
                try
                {
                    return ColorFromString(col);
                }
                catch (Exception e)
                {
                    Logger.LogWarning(e.Message);
                    var pluginConfig = BepInExUtils.GetDependentPlugins(true).First(x => x.Key == ModGUID).Value.Config;
                    var entry = pluginConfig[Section, Key] as ConfigEntry<Color>;
                    Logger.LogWarning($"Using default value ({(Color)entry.DefaultValue}) instead.");
                    return (Color)entry.DefaultValue;
                }
            }

            internal override void SetValue(Color value)
            {
                gameObject.transform.Find("Input").GetComponent<InputField>().text = StringFromColor(value);
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
                long fromHex = 0;
                if (long.TryParse(str.Trim().ToLower(), NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out fromHex))
                {
                    var r = (int)(fromHex >> 24);
                    var g = (int)((fromHex >> 16) & 0xff);
                    var b = (int)((fromHex >> 8) & 0xff);
                    var a = (int)(fromHex & 0xff);
                    var result = new Color(r / 255f, g / 255f, b / 255f, a / 255f);
                    return result;
                }

                throw new ArgumentException($"'{str}' is no valid color value");
            }
        }
    }

    public static class ConfigEntryBaseExtension
    {
        public static bool IsVisible(this ConfigEntryBase ceb)
        {
            var cma = ceb.Description.Tags.FirstOrDefault(x => x is ConfigurationManagerAttributes) as ConfigurationManagerAttributes;
            if (cma != null)
            {
                // if configuration manager attribute is set, check if browsable is not false
                return cma.Browsable != false;
            }

            // no configuration manager attribute?
            return true;
        }
        
        public static bool IsButtonBound(this ConfigEntryBase ceb)
        {
            if (ceb.SettingType != typeof(KeyCode))
            {
                return false;
            }

            var cma = ceb.Description.Tags.FirstOrDefault(x => x is ButtonConfig) as ButtonConfig;
            if (cma != null)
            {
                return !string.IsNullOrEmpty(cma.Name);
            }

            return false;
        }

        public static string GetBoundButtonName(this ConfigEntryBase ceb)
        {
            if (ceb.SettingType != typeof(KeyCode))
            {
                return null;
            }

            var buttonConfig = ceb.Description.Tags.FirstOrDefault(x => x is ButtonConfig) as ButtonConfig;
            if (buttonConfig == null || string.IsNullOrEmpty(buttonConfig.Name))
            {
                throw new Exception(
                    $"Input bound configuration key ({ceb.Definition.Section}.{ceb.Definition.Key}) needs to have a ButtonConfig as Tag.");
            }
            return buttonConfig?.Name.Split('!')[0];
        }

        public static ButtonConfig GetButtonConfig(this ConfigEntryBase ceb)
        {
            if (ceb.SettingType != typeof(KeyCode))
            {
                return null;
            }

            return ceb.Description.Tags.FirstOrDefault(x => x is ButtonConfig) as ButtonConfig;
        }
    }
}
