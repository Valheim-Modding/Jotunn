using Jotunn.Managers;
using UnityEngine;

namespace TestMod
{
    internal class ColorChanger : MonoBehaviour
    {
        private Renderer r;

        void Start()
        {
            r = GetComponentInChildren<Renderer>();
            r.sharedMaterial = r.material;
            GUIManager.Instance.CreateColorPicker(
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                r.sharedMaterial.color, "Choose your poison", SetColor, ColorChosen, true);
            GUIManager.BlockInput(true);
        }

        private void SetColor(Color currentColor)
        {
            r.sharedMaterial.color = currentColor;
        }

        private void ColorChosen(Color finalColor)
        {
            GUIManager.BlockInput(false);
            Destroy(this);
        }
    }
}
