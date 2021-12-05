using UnityEngine;
using UnityEngine.UI;

namespace Jotunn.GUI
{
    internal class ModSettingPlugin : MonoBehaviour
    {
        public Button Button;
        public Text Text;
        public Transform Content;

        public void Toggle()
        {
            Content.gameObject.SetActive(!Content.gameObject.activeSelf);
        }
    }
}
