using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Jotunn.Utils
{
    class ColorWheel
    {

        private static Color m_color;
        /// <summary>
        /// <code name="ChooseColorButtonClick()">Invoke this void and pass it Color to alter the color using the wheel</code>
        /// <param name="color"> this is the paramter to pass while invoking ChooseColorButtonClick it is a Unity Color type</param>
        /// </summary>
        public static void ChooseColorButtonClick(Color color)
        {

            ColorPicker.Create(color, $"Choose the color!", SetColor, ColorFinished, true);
            m_color = color;
        }
        public static void SetColor(Color currentColor)
        {
            m_color = currentColor;
        }

        public static void ColorFinished(Color finishedColor)
        {
#if DEBUG
            Debug.Log("You chose the color " + ColorUtility.ToHtmlStringRGBA(finishedColor));
#endif

        }

        /// <summary>
        /// Gradient picker
        /// </summary>
        //private void Update()
        //{
        //    r.sharedMaterial.color = myGradient.Evaluate(0.5f + Mathf.Sin(Time.time * 2f) * 0.5f);
        //}
        //public void ChooseGradientButtonClick()
        //{
        //    GradientPicker.Create(myGradient, "Choose the sphere's color!", SetGradient, GradientFinished);
        //}
        //private void SetGradient(Gradient currentGradient)
        //{
        //    myGradient = currentGradient;
        //}

        //public static void GradientFinished(Gradient finishedGradient)
        //{
        //    Debug.Log("You chose a Gradient with " + finishedGradient.colorKeys.Length + " Color keys");
        //}
    }
}
