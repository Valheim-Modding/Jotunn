using Jotunn.Managers;
using UnityEngine;

namespace TestMod
{
    internal class ColorChanger : MonoBehaviour
    {
        private Renderer renderer;

        void Start()
        {
            renderer = GetComponent<Renderer>();
            renderer.sharedMaterial = renderer.material;
            GUIManager.Instance.CreateColorPicker(
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                renderer.sharedMaterial.color, "Choose your poison", SetColor, ColorChosen, true);
            GUIManager.BlockInput(true);
        }

        private void SetColor(Color currentColor)
        {
            renderer.sharedMaterial.color = currentColor;
        }

        private void ColorChosen(Color finalColor)
        {
            GUIManager.BlockInput(false);
            Destroy(this);
        }
    }
}
