using System;
using UnityEngine;
using UnityEngine.UI;

namespace JotunnLib.Utils
{
    /// <summary>
    /// Use only, if you know what you do.
    /// There are no checks if a component exists
    /// </summary>
    internal static class GameObjectExtensions
    {
        internal static GameObject SetToTextHeight(this GameObject go)
        {
            go.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, go.GetComponent<Text>().preferredHeight);
            return go;
        }

        internal static GameObject SetUpperLeft(this GameObject go)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMax = new Vector2(0, 1);
            rect.anchorMin = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(0, 0);
            return go;
        }

        internal static GameObject SetMiddleLeft(this GameObject go)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMax = new Vector2(0, 0.5f);
            rect.anchorMin = new Vector2(0, 0.5f);
            rect.pivot = new Vector2(0, 0.5f);
            rect.anchoredPosition = new Vector2(0, 0f);
            return go;
        }

        internal static GameObject SetUpperRight(this GameObject go)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMax = new Vector2(1, 1);
            rect.anchorMin = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(0, 0);
            return go;
        }

        internal static GameObject SetBottomRight(this GameObject go)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMax = new Vector2(1, 0);
            rect.anchorMin = new Vector2(1, 0);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(0, 0);
            return go;
        }

        internal static GameObject SetBottomCenter(this GameObject go)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMax = new Vector2(0.5f, 0);
            rect.anchorMin = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0, 0);
            return go;
        }

        internal static GameObject SetMiddleCenter(this GameObject go)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0, 0);
            return go;
        }

        internal static GameObject SetSize(this GameObject go, float width, float height)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            return go;
        }

        internal static GameObject SetWidth(this GameObject go, float width)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            return go;
        }

        internal static GameObject SetHeight(this GameObject go, float height)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            return go;
        }

        internal static float GetHeight(this GameObject go)
        {
            return go.GetComponent<Text>().preferredHeight;
        }

        internal static GameObject SetText(this GameObject go, string text)
        {
            go.GetComponent<Text>().text = text;
            return go;
        }
    }

    public static class ExposedGameObjectExtensions
    {
        /// <summary>
        ///     Facilitates use of null propagation operator for unity GameObjects by respecting op_equality.
        /// </summary>
        /// <param name="this"> this </param>
        /// <returns>Returns null when GameObject.op_equality returns false.</returns>
        public static GameObject OrNull(this GameObject @this)
        {
            return @this ? @this : null;
        }

        /// <summary>
        /// Facilitates use of null propagation operator for unity MonBehaviours by respecting op_equality.
        /// </summary>
        /// <typeparam name="T">Any type that inherits MonoBehaviour</typeparam>
        /// <param name="this">this</param>
        /// <returns>Returns null when MonoBehaviours.op_equality returns false.</returns>
        public static T OrNull<T>(this T @this) where T : UnityEngine.Object
        {
            return (T)(@this ? @this : null);
        }
    }
}