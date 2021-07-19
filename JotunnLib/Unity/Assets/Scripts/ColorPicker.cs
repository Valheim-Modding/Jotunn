using System;
using UnityEngine;
using UnityEngine.UI;

namespace Jotunn.GUI
{
    /// <summary>
    ///     Custom MonoBehaviour for the ColorPicker
    /// </summary>
    public class ColorPicker : MonoBehaviour
    {
        /// <summary>
        /// Event that gets called by the ColorPicker
        /// </summary>
        /// <param name="c">received Color</param>
        public delegate void ColorEvent(Color c);

        /// <summary>
        ///     Singeleton instance
        /// </summary>
        private static ColorPicker instance;

        /// <returns>
        ///     True when the ColorPicker is closed
        /// </returns>
        public static bool done = true;

        /// <summary>
        ///     OnColorChanged event
        /// </summary>
        private static ColorEvent onCC;
        /// <summary>
        ///     OnColorSelected event
        /// </summary>
        private static ColorEvent onCS;

        /// <summary>
        ///     Color before editing
        /// </summary>
        private static Color32 originalColor;
        /// <summary>
        ///     Current Color
        /// </summary>
        private static Color32 modifiedColor;
        private static HSV modifiedHsv;

        /// <summary>
        ///     UseAlpha bool
        /// </summary>
        private static bool useA;
        /// <summary>
        ///     Interact bool
        /// </summary>
        private bool interact;

        /// <summary>
        ///     Component ref
        /// </summary>
        public RectTransform positionIndicator;
        /// <summary>
        ///     Component ref
        /// </summary>
        public Slider mainComponent;
        /// <summary>
        ///     Component ref
        /// </summary>
        public Slider rComponent;
        /// <summary>
        ///     Component ref
        /// </summary>
        public Slider gComponent;
        /// <summary>
        ///     Component ref
        /// </summary>
        public Slider bComponent;
        /// <summary>
        ///     Component ref
        /// </summary>
        public Slider aComponent;
        /// <summary>
        ///     Component ref
        /// </summary>
        public InputField hexaComponent;
        /// <summary>
        ///     Component ref
        /// </summary>
        public RawImage colorComponent;

        private void Awake()
        {
            instance = this;
            gameObject.SetActive(false);

        }

        /// <summary>
        ///     Creates a new Colorpicker
        /// </summary>
        /// <param name="original">Color before editing</param>
        /// <param name="message">Display message</param>
        /// <param name="onColorChanged">Event that gets called when the color gets modified</param>
        /// <param name="onColorSelected">Event that gets called when one of the buttons done or cancel get pressed</param>
        /// <param name="useAlpha">When set to false the colors used don't have an alpha channel</param>
        /// <returns>
        ///     False if the instance is already running
        /// </returns>
        public static bool Create(Color original, string message, ColorEvent onColorChanged, ColorEvent onColorSelected, bool useAlpha = false)
        {
            if (instance is null)
            {
                Debug.LogError("No Colorpicker prefab active on 'Start' in scene");
                return false;
            }
            if (done)
            {
                done = false;
                originalColor = original;
                modifiedColor = original;
                onCC = onColorChanged;
                onCS = onColorSelected;
                useA = useAlpha;
                instance.gameObject.SetActive(true);
                instance.transform.GetChild(0).GetComponent<Text>().text = message;
                instance.aComponent.gameObject.SetActive(useAlpha);
                instance.RecalculateMenu(true);
                instance.hexaComponent.placeholder.GetComponent<Text>().text = "RRGGBB" + (useAlpha ? "AA" : "");
                return true;
            }
            else
            {
                Done();
                return false;
            }
        }

        /// <summary>
        ///     Called when color is modified, to update other UI components
        /// </summary>
        /// <param name="recalculateHSV"></param>
        private void RecalculateMenu(bool recalculateHSV)
        {
            interact = false;
            if (recalculateHSV)
            {
                modifiedHsv = new HSV(modifiedColor);
            }
            else
            {
                modifiedColor = modifiedHsv.ToColor();
            }
            rComponent.value = modifiedColor.r;
            rComponent.transform.GetChild(3).GetComponent<InputField>().text = modifiedColor.r.ToString();
            gComponent.value = modifiedColor.g;
            gComponent.transform.GetChild(3).GetComponent<InputField>().text = modifiedColor.g.ToString();
            bComponent.value = modifiedColor.b;
            bComponent.transform.GetChild(3).GetComponent<InputField>().text = modifiedColor.b.ToString();
            if (useA)
            {
                aComponent.value = modifiedColor.a;
                aComponent.transform.GetChild(3).GetComponent<InputField>().text = modifiedColor.a.ToString();
            }
            mainComponent.value = (float)modifiedHsv.H;
            rComponent.transform.GetChild(0).GetComponent<RawImage>().color = new Color32(255, modifiedColor.g, modifiedColor.b, 255);
            rComponent.transform.GetChild(0).GetChild(0).GetComponent<RawImage>().color = new Color32(0, modifiedColor.g, modifiedColor.b, 255);
            gComponent.transform.GetChild(0).GetComponent<RawImage>().color = new Color32(modifiedColor.r, 255, modifiedColor.b, 255);
            gComponent.transform.GetChild(0).GetChild(0).GetComponent<RawImage>().color = new Color32(modifiedColor.r, 0, modifiedColor.b, 255);
            bComponent.transform.GetChild(0).GetComponent<RawImage>().color = new Color32(modifiedColor.r, modifiedColor.g, 255, 255);
            bComponent.transform.GetChild(0).GetChild(0).GetComponent<RawImage>().color = new Color32(modifiedColor.r, modifiedColor.g, 0, 255);
            if (useA) aComponent.transform.GetChild(0).GetChild(0).GetComponent<RawImage>().color = new Color32(modifiedColor.r, modifiedColor.g, modifiedColor.b, 255);
            positionIndicator.parent.GetChild(0).GetComponent<RawImage>().color = new HSV(modifiedHsv.H, 1d, 1d).ToColor();
            positionIndicator.anchorMin = new Vector2((float)modifiedHsv.S, (float)modifiedHsv.V);
            positionIndicator.anchorMax = positionIndicator.anchorMin;
            hexaComponent.text = useA ? ColorUtility.ToHtmlStringRGBA(modifiedColor) : ColorUtility.ToHtmlStringRGB(modifiedColor);
            colorComponent.color = modifiedColor;
            onCC?.Invoke(modifiedColor);
            interact = true;
        }

        /// <summary>
        ///     Used by EventTrigger to calculate the chosen value in color box
        /// </summary>
        public void SetChooser()
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(positionIndicator.parent as RectTransform, Input.mousePosition, GetComponentInParent<Canvas>().worldCamera, out Vector2 localpoint);
            localpoint = Rect.PointToNormalized((positionIndicator.parent as RectTransform).rect, localpoint);
            if (positionIndicator.anchorMin != localpoint)
            {
                positionIndicator.anchorMin = localpoint;
                positionIndicator.anchorMax = localpoint;
                modifiedHsv.S = localpoint.x;
                modifiedHsv.V = localpoint.y;
                RecalculateMenu(false);
            }
        }

        /// <summary>
        ///     Gets main Slider value
        /// </summary>
        /// <param name="value"></param>
        public void SetMain(float value)
        {
            if (interact)
            {
                modifiedHsv.H = value;
                RecalculateMenu(false);
            }
        }

        /// <summary>
        ///     Gets r Slider value
        /// </summary>
        /// <param name="value"></param>
        public void SetR(float value)
        {
            if (interact)
            {
                modifiedColor.r = (byte)value;
                RecalculateMenu(true);
            }
        }
        /// <summary>
        ///     Gets r InputField value
        /// </summary>
        /// <param name="value"></param>
        public void SetR(string value)
        {
            if (interact)
            {
                modifiedColor.r = (byte)Mathf.Clamp(int.Parse(value), 0, 255);
                RecalculateMenu(true);
            }
        }
        /// <summary>
        ///     Gets g Slider value
        /// </summary>
        /// <param name="value"></param>
        public void SetG(float value)
        {
            if (interact)
            {
                modifiedColor.g = (byte)value;
                RecalculateMenu(true);
            }
        }
        /// <summary>
        ///     Gets g InputField value
        /// </summary>
        /// <param name="value"></param>
        public void SetG(string value)
        {
            if (interact)
            {
                modifiedColor.g = (byte)Mathf.Clamp(int.Parse(value), 0, 255);
                RecalculateMenu(true);
            }
        }
        /// <summary>
        ///     Gets b Slider value
        /// </summary>
        /// <param name="value"></param>
        public void SetB(float value)
        {
            if (interact)
            {
                modifiedColor.b = (byte)value;
                RecalculateMenu(true);
            }
        }
        /// <summary>
        ///     Gets b InputField value
        /// </summary>
        /// <param name="value"></param>
        public void SetB(string value)
        {
            if (interact)
            {
                modifiedColor.b = (byte)Mathf.Clamp(int.Parse(value), 0, 255);
                RecalculateMenu(true);
            }
        }
        /// <summary>
        ///     Gets a Slider value
        /// </summary>
        /// <param name="value"></param>
        public void SetA(float value)
        {
            if (interact)
            {
                modifiedHsv.A = (byte)value;
                RecalculateMenu(false);
            }
        }
        /// <summary>
        ///     Gets a InputField value
        /// </summary>
        /// <param name="value"></param>
        public void SetA(string value)
        {
            if (interact)
            {
                modifiedHsv.A = (byte)Mathf.Clamp(int.Parse(value), 0, 255);
                RecalculateMenu(false);
            }
        }
        /// <summary>
        ///     Gets hexa InputField value
        /// </summary>
        /// <param name="value"></param>
        public void SetHexa(string value)
        {
            if (interact)
            {
                if (ColorUtility.TryParseHtmlString("#" + value, out Color c))
                {
                    if (!useA) c.a = 1;
                    modifiedColor = c;
                    RecalculateMenu(true);
                }
                else
                {
                    hexaComponent.text = useA ? ColorUtility.ToHtmlStringRGBA(modifiedColor) : ColorUtility.ToHtmlStringRGB(modifiedColor);
                }
            }
        }
        /// <summary>
        ///     Cancel button call
        /// </summary>
        public void CCancel()
        {
            Cancel();
        }
        /// <summary>
        ///     Manually cancel the ColorPicker and recover the default value
        /// </summary>
        public static void Cancel()
        {
            modifiedColor = originalColor;
            Done();
        }
        /// <summary>
        ///     Done button call
        /// </summary>
        public void CDone()
        {
            Done();
        }
        /// <summary>
        ///     Manually close the ColorPicker and apply the selected color
        /// </summary>
        public static void Done()
        {
            done = true;
            onCC?.Invoke(modifiedColor);
            onCS?.Invoke(modifiedColor);
            instance.transform.gameObject.SetActive(false);
        }
        /// <summary>
        ///     HSV helper class
        /// </summary>
        private sealed class HSV
        {
            public double H = 0, S = 1, V = 1;
            public byte A = 255;
            public HSV() { }
            public HSV(double h, double s, double v)
            {
                H = h;
                S = s;
                V = v;
            }
            public HSV(Color color)
            {
                float max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
                float min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));

                float hue = (float)H;
                if (min != max)
                {
                    if (max == color.r)
                    {
                        hue = (color.g - color.b) / (max - min);

                    }
                    else if (max == color.g)
                    {
                        hue = 2f + (color.b - color.r) / (max - min);

                    }
                    else
                    {
                        hue = 4f + (color.r - color.g) / (max - min);
                    }

                    hue *= 60;
                    if (hue < 0) hue += 360;
                }

                H = hue;
                S = (max == 0) ? 0 : 1d - ((double)min / max);
                V = max;
                A = (byte)(color.a * 255);
            }
            public Color32 ToColor()
            {
                int hi = Convert.ToInt32(Math.Floor(H / 60)) % 6;
                double f = H / 60 - Math.Floor(H / 60);

                double value = V * 255;
                byte v = (byte)Convert.ToInt32(value);
                byte p = (byte)Convert.ToInt32(value * (1 - S));
                byte q = (byte)Convert.ToInt32(value * (1 - f * S));
                byte t = (byte)Convert.ToInt32(value * (1 - (1 - f) * S));

                switch (hi)
                {
                    case 0:
                        return new Color32(v, t, p, A);
                    case 1:
                        return new Color32(q, v, p, A);
                    case 2:
                        return new Color32(p, v, t, A);
                    case 3:
                        return new Color32(p, q, v, A);
                    case 4:
                        return new Color32(t, p, v, A);
                    case 5:
                        return new Color32(v, p, q, A);
                    default:
                        return new Color32();
                }
            }
        }
    }
}
