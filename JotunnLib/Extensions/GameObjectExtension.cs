using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace Jotunn
{
    /// <summary>
    ///     Extends GameObject with a shortcut for the Unity bool operator override.
    /// </summary>
    public static class ExposedGameObjectExtension
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
        ///     Facilitates use of null propagation operator for unity MonBehaviours by respecting op_equality.
        /// </summary>
        /// <typeparam name="T">Any type that inherits MonoBehaviour</typeparam>
        /// <param name="this">this</param>
        /// <returns>Returns null when MonoBehaviours.op_equality returns false.</returns>
        public static T OrNull<T>(this T @this) where T : Object
        {
            return (T)(@this ? @this : null);
        }

        /// <summary>
        ///     Returns the component of Type type. If one doesn't already exist on the GameObject it will be added.
        /// </summary>
        /// <remarks>Source: https://wiki.unity3d.com/index.php/GetOrAddComponent</remarks>
        /// <typeparam name="T">The type of Component to return.</typeparam>
        /// <param name="gameObject">The GameObject this Component is attached to.</param>
        /// <returns>Component</returns>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            return gameObject.GetComponent<T>() ?? gameObject.AddComponent<T>();
        }

        /// <summary>
        ///     Adds a new copy of the provided component to a gameObject
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="duplicate"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Component AddComponentCopy<T>(this GameObject gameObject, T duplicate) where T : Component
        {
            Component target = gameObject.AddComponent(duplicate.GetType());
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            foreach (PropertyInfo propertyInfo in duplicate.GetType().GetProperties(flags))
            {
                if (propertyInfo.CanWrite)
                {
                    propertyInfo.SetValue(target, propertyInfo.GetValue(duplicate, null), null);
                }
            }

            foreach (FieldInfo fieldInfo in duplicate.GetType().GetFields(flags))
            {
                fieldInfo.SetValue(target, fieldInfo.GetValue(duplicate));
            }

            return target;
        }
    }

    /// <summary>
    ///     Use only, if you know what you do.
    ///     There are no checks if a component exists.
    /// </summary>
    internal static class GameObjectGUIExtension
    {
        internal static GameObject SetToTextHeight(this GameObject go)
        {
            go.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, go.GetComponentInChildren<Text>().preferredHeight + 3f);
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

        internal static GameObject SetBottomLeft(this GameObject go)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMax = new Vector2(0, 0);
            rect.anchorMin = new Vector2(0, 0);
            rect.pivot = new Vector2(0, 0);
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

        internal static GameObject SetMiddleRight(this GameObject go)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMax = new Vector2(1, 0.5f);
            rect.anchorMin = new Vector2(1, 0.5f);
            rect.pivot = new Vector2(1, 0.5f);
            rect.anchoredPosition = new Vector2(0, 0f);
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

        internal static GameObject SetUpperCenter(this GameObject go)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
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

        internal static GameObject SetBottomCenter(this GameObject go)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMax = new Vector2(0.5f, 0);
            rect.anchorMin = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0f);
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

        internal static float GetWidth(this GameObject go)
        {
            var rect = go.GetComponent<RectTransform>();
            return rect.rect.width;
        }

        internal static float GetHeight(this GameObject go)
        {
            var rect = go.GetComponent<RectTransform>();
            return rect.rect.height;
        }

        internal static float GetTextHeight(this GameObject go)
        {
            return go.GetComponent<Text>().preferredHeight;
        }

        internal static GameObject SetText(this GameObject go, string text)
        {
            go.GetComponent<Text>().text = text;
            return go;
        }
    }
}
