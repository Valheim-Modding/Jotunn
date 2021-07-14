﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Jotunn.Utils
{
    class ColorWheel
    {
        private static GameObject ColorMenu;
        private static GameObject menu;
        private static Color m_color { get; set; }
        //the next thing to do in here is to only run LoadColorWheelAsset if this class is used in any way... not sure how to do that
        internal static void LoadColorWheelAsset()
        {
            var stream = typeof(ColorWheel).Assembly.GetManifestResourceStream("Jotunn.Assets.jotunncolorwheel");
            if (stream == null)
            {
#if DEBUG
                Debug.LogError($"Colorwheel not loaded from stream {stream}");
#endif
            }
            else
            {
               AssetBundle asset =  AssetBundle.LoadFromStream(stream);
               ColorMenu = asset.LoadAsset<GameObject>("JotunnColorWheel");
            }
        }

        //Add some framework for loading the Colormanager on screen in any scenario, Leave it up to end user how they dictate restrictions on this item showing
        /// <summary>
        /// Pass your Color to this structure in order to allow the "Choose Color" button to manipulate your UnityEngine Color 
        /// </summary>
        /// <param name="color"></param>
         private static void DisplayColorButtons(Color color)
        {
            try
            {
                if (ZNetScene.instance != null)
                {
                    if (menu is null)
                    {
                        menu = Main.Instantiate(ColorMenu);
                        menu.name = "JotunnColorMenu";
                        menu.transform.SetSiblingIndex(menu.transform.GetSiblingIndex() - 4);
                        var ColorButton = menu.transform.Find("ChooseColor").GetComponent<Button>();
                        ColorButton.onClick.AddListener((() =>
                        {
                            ColorPicker.Create(color, $"Setting the Color for {color}", ColorWheel.SetColor,
                                ColorWheel.ColorFinished, true);
                        }));
                    }
                    else
                    {
                        menu.SetActive(!menu.activeSelf);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Issue displaying ColorInterface: {ex}");
            }
        }

        private static void SetColor(Color currentColor)
        {
            m_color = currentColor;
        }

        private static void ColorFinished(Color finishedColor)
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
