using System.Collections;
using System.Collections.Generic;
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
        public Button CancelButton;
        public Button OKButton;

        private readonly Dictionary<string, ModSettingPlugin> Plugins = new Dictionary<string, ModSettingPlugin>();

        public ModSettingPlugin AddPlugin(string name, string text)
        {
            if (!Plugins.TryGetValue(name, out var plugin))
            {
                var go = Instantiate(PluginPrefab, ScrollRect.content);
                go.SetActive(true);
                plugin = go.GetComponent<ModSettingPlugin>();
                plugin.Text.text = text;
                Plugins.Add(name, plugin);
            }
            
            return plugin;
        }

        public void AddSection(string name, string text)
        {
            if (Plugins.TryGetValue(name, out var plugin))
            {
                var go = Instantiate(SectionPrefab, plugin.Content.transform);
                go.SetActive(true);
                go.GetComponent<Text>().text = text;
            }
        }

        public void AddConfig(string name, string entry, Color entryColor, string description, Color descriptionColor)
        {
            if (Plugins.TryGetValue(name, out var plugin))
            {
                var go = Instantiate(ConfigPrefab, plugin.Content.transform);
                go.SetActive(true);
                var config = go.GetComponent<ModSettingConfig>();
                config.Header.text = entry;
                config.Header.color = entryColor;
                config.Description.text = description;
                config.Description.color = descriptionColor;
            }
        }
        
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
