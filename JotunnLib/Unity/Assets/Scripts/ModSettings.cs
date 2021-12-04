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
        
        public GameObject BindDialogue;

        private readonly Dictionary<string, ModSettingPlugin> Plugins = new Dictionary<string, ModSettingPlugin>();

        public ModSettingPlugin AddPlugin(string name, string text)
        {
            if (Plugins.TryGetValue(name, out var plugin))
            {
                return plugin;
            }

            var go = Instantiate(PluginPrefab, ScrollRect.content);
            go.name = name;
            go.SetActive(true);
            plugin = go.GetComponent<ModSettingPlugin>();
            plugin.Text.text = text;
            Plugins.Add(name, plugin);

            return plugin;
        }

        public void AddSection(string name, string text)
        {
            if (!Plugins.TryGetValue(name, out var plugin))
            {
                return;
            }

            var go = Instantiate(SectionPrefab, plugin.Content.transform);
            go.SetActive(true);
            go.GetComponent<Text>().text = text;
        }

        public GameObject AddConfig(string name, string entry, Color entryColor, string description, Color descriptionColor)
        {
            if (!Plugins.TryGetValue(name, out var plugin))
            {
                return null;
            }

            var go = Instantiate(ConfigPrefab, plugin.Content.transform);
            go.SetActive(true);
            var config = go.GetComponent<ModSettingConfig>();
            config.Header.text = entry;
            config.Header.color = entryColor;
            config.Description.text = description;
            config.Description.color = descriptionColor;

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
                CurrentPluginButton.onClick.RemoveAllListeners();
                CurrentPluginButton.onClick.AddListener(() => currentPlugin.GetComponentInParent<ModSettingPlugin>().Toggle());
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
        
        public void OpenBindDialogue(string keyName)
        {
            ZInput.instance.StartBindKey(keyName);
            BindDialogue.SetActive(true);
        }

        private void Update()
        {
            if (BindDialogue.activeSelf && ZInput.instance.EndBindKey())
            {
                BindDialogue.SetActive(false);
            }
        }
    }
}
