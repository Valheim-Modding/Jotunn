using System.Reflection;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Jotunn.Extensions;

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
            return gameObject.TryGetComponent(out T component) ? component : gameObject.AddComponent<T>();
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
                switch (propertyInfo.Name)
                {
                    // setting rayTracingMode prints a warning, because ray tracing is disabled
                    case "rayTracingMode":
                        continue;
                    // this is Component.name and is shared with the GameObject name. Copying a component should not change the GameObject name
                    case "name":
                        continue;
                    // this is Component.tag and sets the GameObject tag. Copying a component should not change the GameObject tag
                    case "tag":
                        continue;
                    // not allowed to access
                    case "mesh":
                        if (duplicate is MeshFilter)
                            continue;
                        break;
                    // not allowed to access
                    case "material":
                    case "materials":
                        if (duplicate is Renderer)
                            continue;
                        break;
                    // setting the bounds overrides the default bounding box and the renderer bounding volume will no longer be automatically calculated
                    case "bounds":
                        if (duplicate is Renderer)
                            continue;
                        break;
                }

                if (propertyInfo.CanWrite && propertyInfo.GetMethod != null)
                {
                    propertyInfo.SetValue(target, propertyInfo.GetValue(duplicate));
                }
            }

            foreach (FieldInfo fieldInfo in duplicate.GetType().GetFields(flags))
            {
                if (fieldInfo.Name == "rayTracingMode")
                {
                    continue;
                }

                fieldInfo.SetValue(target, fieldInfo.GetValue(duplicate));
            }

            return target;
        }

        /// <summary>
        ///     Extension method to check if GameObject has a component.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        public static bool HasComponent<T>(this GameObject gameObject) where T : Component
        {
            return gameObject.GetComponent<T>() != null;
        }

        /// <summary>
        ///     Extension method to check if GameObject has a component.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        public static bool HasComponent(this GameObject gameObject, string componentName)
        {
            return gameObject.GetComponent(componentName) != null;
        }

        /// <summary>
        ///     Check if GameObject has any of the specified components.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="components"></param>
        /// <returns></returns>
        public static bool HasAnyComponent(this GameObject gameObject, params Type[] components)
        {
            foreach (var compo in components)
            {
                if (gameObject.GetComponent(compo) != null)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///     Check if GameObject has any of the specified components.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="componentNames"></param>
        /// <returns></returns>
        public static bool HasAnyComponent(this GameObject gameObject, params string[] componentNames)
        {
            foreach (var name in componentNames)
            {
                if (gameObject.GetComponent(name) != null)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///     Check if GameObject has all of the specified components.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="componentNames"></param>
        /// <returns></returns>
        public static bool HasAllComponents(this GameObject gameObject, params string[] componentNames)
        {
            foreach (var name in componentNames)
            {
                if (gameObject.GetComponent(name) == null)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        ///     Check if GameObject has all of the specified components.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="components"></param>
        /// <returns></returns>
        public static bool HasAllComponents(this GameObject gameObject, params Type[] components)
        {
            foreach (var compo in components)
            {
                if (gameObject.GetComponent(compo) == null)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        ///     Check if GameObject or any of it's children
        ///     have any of the specified components.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="components"></param>
        /// <returns></returns>
        public static bool HasAnyComponentInChildren(
            this GameObject gameObject,
            bool includeInactive = false,
            params Type[] components
        )
        {
            foreach (var compo in components)
            {
                if (gameObject.GetComponentInChildren(compo, includeInactive) != null)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///     Check if GameObject or any of it's children
        ///     have the specific component.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="includeInactive">Whether to include inactive child objects in the search or not.</param>
        /// <returns></returns>
        public static bool HasComponentInChildren<T>(this GameObject gameObject, bool includeInactive = false) where T : Component
        {
            return gameObject.GetComponentInChildren<T>(includeInactive) != null;
        }

        /// <summary>
        ///     Extension method to get the first component in the GameObject
        ///     or it's children that has the specified name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="gameObject"></param>
        /// <param name="name"></param>
        /// <param name="includeInactive">Whether to include inactive child objects in the search or not.</param>
        /// <returns></returns>
        public static T GetComponentInChildrenByName<T>(
            this GameObject gameObject,
            string name,
            bool includeInactive = false
        ) where T : Component
        {
            foreach (var compo in gameObject.GetComponentsInChildren<T>(includeInactive))
            {
                if (compo.name == name)
                {
                    return compo;
                }
            }
            Logger.LogWarning($"No {nameof(T)} with name {name} found for GameObject: {gameObject.name}");
            return null;
        }

        /// <summary>
        ///     Extension method to find nested children by name using either
        ///     a breadth-first or depth-first search. Default is breadth-first.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="childName">Name of the child object to search for.</param>
        /// <param name="breadthFirst"> Whether to preform a breadth first or depth first search. Default is breadth first.</param>
        /// <returns></returns>
        public static Transform FindDeepChild(this GameObject gameObject, string childName, bool breadthFirst = true)
        {
            return gameObject?.transform.FindDeepChild(childName, breadthFirst);
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
            var txt = go.GetComponent<Text>();
            if (txt != null) txt.text = text;
            var tmp = go.GetComponent<TMP_Text>();
            if (tmp != null) tmp.text = text;
            return go;
        }
    }
}
