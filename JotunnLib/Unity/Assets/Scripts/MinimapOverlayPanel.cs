using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Jotunn.GUI
{
    public class MinimapOverlayPanel : MonoBehaviour
    {
        public GameObject OverlayGroup;
        public Button Button;
        public GameObject BaseMod;
        public Text BaseModText;
        public Toggle BaseToggle;

        private readonly Dictionary<string, GameObject> Mods = new Dictionary<string, GameObject>();

        public void ToggleOverlayGroup()
        {
            OverlayGroup.SetActive(!OverlayGroup.activeSelf);
        }

        public Toggle AddOverlayToggle(string modName, string overlayName)
        {
            if (!Mods.TryGetValue(modName, out var mod))
            {
                mod = Instantiate(BaseMod, OverlayGroup.transform);
                mod.SetActive(true);
                mod.name = modName;
                mod.GetComponentInChildren<Text>().text = modName;
                Mods.Add(modName, mod);
            }
            var toggle = Instantiate(BaseToggle, mod.transform);
            toggle.gameObject.SetActive(true);
            toggle.gameObject.name = overlayName;
            toggle.GetComponentInChildren<Text>().text = overlayName;
            return toggle;
        }
    }
}
