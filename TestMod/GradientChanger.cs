using Jotunn.Managers;
using UnityEngine;

namespace TestMod
{
    internal class GradientChanger : MonoBehaviour
    {
        private Renderer r;
        private Gradient g;

        void Start()
        {
            r = GetComponentInChildren<Renderer>();
            r.sharedMaterial = r.material;
            g = new Gradient();
            GUIManager.Instance.CreateGradientPicker(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0),
                new Gradient(), "Gradiwut?", SetGradient, GradientFinished);

            GUIManager.BlockInput(true);
        }
        private void Update()
        {
            r.sharedMaterial.color = g.Evaluate(0.5f + Mathf.Sin(Time.time * 2f) * 0.5f);
        }

        private void SetGradient(Gradient currentGradient)
        {
            g = currentGradient;
        }

        public void GradientFinished(Gradient finishedGradient)
        {
            GUIManager.BlockInput(false);
        }
    }
}
