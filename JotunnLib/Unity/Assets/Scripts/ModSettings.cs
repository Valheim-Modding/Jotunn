using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Jotunn.GUI
{
    internal class ModSettings : MonoBehaviour
    {
        public GameObject PluginPrefab;
        public GameObject SectionPrefab;
        public GameObject ConfigPrefab;

        public Image Panel;
        public Text Header;
        public ScrollRect ScrollRect;
        public Button CurrentPluginButton;
        public Button CancelButton;
        public Button OKButton;

        public GameObject BindDialog;
        public KeyBindCheck CurrentKeyBindCheck;

        public readonly Dictionary<string, ModSettingPlugin> Plugins = new Dictionary<string, ModSettingPlugin>();
        public readonly List<ModSettingSection> Sections = new List<ModSettingSection>();
        public readonly List<ModSettingConfig> Configs = new List<ModSettingConfig>();

        public void AddPlugin(string pluginName, string text)
        {
            if (Plugins.ContainsKey(pluginName))
            {
                return;
            }

            var go = Instantiate(PluginPrefab, ScrollRect.content);
            go.name = pluginName;
            go.SetActive(true);
            var plugin = go.GetComponent<ModSettingPlugin>();
            plugin.Text.text = text;
            Plugins.Add(pluginName, plugin);
        }

        public void AddSection(string pluginName, string sectionName)
        {
            if (!Plugins.TryGetValue(pluginName, out var plugin))
            {
                return;
            }

            var go = Instantiate(SectionPrefab, plugin.Content.transform);
            go.name = sectionName;
            go.SetActive(true);
            var section = go.GetComponent<ModSettingSection>();
            section.GUID = pluginName;
            section.Text.text = sectionName;
            Sections.Add(section);
        }

        public GameObject AddConfig(string pluginName, string entryName, Color entryColor, string description, Color descriptionColor)
        {
            if (!Plugins.TryGetValue(pluginName, out var plugin))
            {
                return null;
            }

            var go = Instantiate(ConfigPrefab, plugin.Content.transform);
            go.SetActive(true);
            var config = go.GetComponent<ModSettingConfig>();
            config.Header.text = entryName;
            config.Header.color = entryColor;
            config.Description.text = description;
            config.Description.color = descriptionColor;
            Configs.Add(config);

            return go;
        }

        public void OnScrollRectChanged(Vector2 position)
        {
            var overlaps = Plugins.Values.Select(x => x.Button.GetComponent<RectTransform>())
                .Any(x => Overlaps(x, CurrentPluginButton.GetComponent<RectTransform>()));

            CurrentPluginButton.gameObject.SetActive(!overlaps);

            var currentPlugin = Plugins.Values.Select(x => x.Button)
                .LastOrDefault(x =>
                    WorldRect(x.GetComponent<RectTransform>()).y >
                    WorldRect(CurrentPluginButton.GetComponent<RectTransform>()).y);

            if (currentPlugin)
            {
                CurrentPluginButton.GetComponentInChildren<Text>().text = currentPlugin.GetComponentInChildren<Text>().text;
                CurrentPluginButton.onClick = currentPlugin.onClick;
            }
        }

        private Rect WorldRect(RectTransform rectTransform)
        {
            Vector2 sizeDelta = rectTransform.sizeDelta;
            float rectTransformWidth = sizeDelta.x * rectTransform.lossyScale.x;
            float rectTransformHeight = sizeDelta.y * rectTransform.lossyScale.y;

            Vector3 position = rectTransform.position;
            return new Rect(position.x - rectTransformWidth / 2f, position.y - rectTransformHeight / 2f, rectTransformWidth, rectTransformHeight);
        }

        private bool Overlaps(RectTransform a, RectTransform b)
        {
            var recta = WorldRect(a);
            var rectb = WorldRect(b);

            return (int)recta.y == (int)rectb.y || (recta.y + recta.height > rectb.y && recta.y < rectb.y);
        }

        public delegate bool KeyBindCheck();

        public void OpenBindDialog(string keyName, KeyBindCheck keyBindCheck)
        {
            ZInput.instance.StartBindKey(keyName ?? string.Empty);
            CurrentKeyBindCheck = keyBindCheck;
            BindDialog.SetActive(true);
        }

        private void Update()
        {
            if (BindDialog.activeSelf && CurrentKeyBindCheck.Invoke())
            {
                BindDialog.SetActive(false);
            }
        }

        public void CreateTestConfig()
        {
            string modName = $"Test{Plugins.Count}";
            ModSettingConfig config;
            AddPlugin(modName, $"Test Mod {Plugins.Count}");
            AddSection(modName, "First section");
            config = AddConfig(modName, "Bool Test", Color.black, "Testing booleans", Color.black)
                .GetComponent<ModSettingConfig>();
            config.Toggle.gameObject.SetActive(true);
            config = AddConfig(modName, "String Test", Color.black, "Testing strings", Color.black)
                .GetComponent<ModSettingConfig>();
            config.InputField.gameObject.SetActive(true);
            AddSection(modName, "Second section");
            config = AddConfig(modName, "KeyCode Test", Color.black, "Testing KeyCodes", Color.black)
                .GetComponent<ModSettingConfig>();
            config.InputField.gameObject.SetActive(true);
            config.Dropdown.gameObject.SetActive(true);
        }
    }
}
