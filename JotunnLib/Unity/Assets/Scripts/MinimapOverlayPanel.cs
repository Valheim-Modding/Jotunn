using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Jotunn.GUI
{
    public class MinimapOverlayPanel : MonoBehaviour
    {
        /// <summary>
        ///     
        /// </summary>
        public GameObject OverlayGroup;
        /// <summary>
        /// 
        /// </summary>
        public Button Button;
        /// <summary>
        /// 
        /// </summary>
        public GameObject BaseMod;
        /// <summary>
        /// 
        /// </summary>
        public Text BaseModText;
        /// <summary>
        /// 
        /// </summary>
        public Toggle BaseToggle;

        private readonly Dictionary<string, GameObject> Mods = new Dictionary<string, GameObject>();
        
        /// <summary>
        /// 
        /// </summary>
        public void ToggleOverlayGroup()
        {
            OverlayGroup.SetActive(!OverlayGroup.activeSelf);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="modName"></param>
        /// <param name="overlayName"></param>
        /// <returns></returns>
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
