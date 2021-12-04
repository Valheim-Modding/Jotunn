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
        public Button CurrentPlugin;
        public ScrollRect ScrollRect;
        public Button CancelButton;
        public Button OKButton;

        private readonly Dictionary<string, ModSettingPlugin> Plugins = new Dictionary<string, ModSettingPlugin>();

        public ModSettingPlugin AddPlugin(string name, string text)
        {
            if (!Plugins.TryGetValue(name, out var plugin))
            {
                var go = Instantiate(PluginPrefab, ScrollRect.content);
                go.name = name;
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

        public GameObject AddConfig(string name, string entry, Color entryColor, string description, Color descriptionColor)
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

                return go;
            }

            return null;
        }

        public void OnScrollRectChanged(Vector2 position)
        {
            var overlaps = Plugins.Values.Select(x => WorldRect(x.Button.GetComponent<RectTransform>()))
                .Any(x => (int)x.yMax + (int)x.height/2 == (int)WorldRect(CurrentPlugin.GetComponent<RectTransform>()).yMax);
            
            CurrentPlugin.gameObject.SetActive(!overlaps);
         
            var currentPlugin = Plugins.Values.Select(x => x.Button.GetComponent<RectTransform>())
                .LastOrDefault(x => (int)WorldRect(x).y > (int)CurrentPlugin.GetComponent<RectTransform>().position.y);

            if (currentPlugin)
            {
                CurrentPlugin.GetComponentInChildren<Text>().text = currentPlugin.GetComponentInChildren<Text>().text;
            }
        }

        private Rect WorldRect(RectTransform rectTransform)
        {
            Vector2 sizeDelta = rectTransform.sizeDelta;
            float rectTransformWidth = sizeDelta.x * rectTransform.lossyScale.x;
            float rectTransformHeight = sizeDelta.y * rectTransform.lossyScale.y;

            Vector3 position = rectTransform.position;
            return new Rect(position.x + rectTransformWidth / 2f, position.y + rectTransformHeight / 2f, rectTransformWidth, rectTransformHeight);
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
