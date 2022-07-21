using System.Collections.Generic;
using System.Reflection;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace Jotunn
{
    /// <summary>
    ///     Extends prefab GameObjects with functionality related to the mocking system.
    /// </summary>
    public static class PrefabExtension
    {
        /// <summary>
        ///     Will attempt to fix every field that are mocks gameObjects / Components from the given object.
        /// </summary>
        /// <param name="objectToFix"></param>
        public static void FixReferences(this object objectToFix)
        {
            MockManager.FixReferences(objectToFix, 0);
        }

        /// <summary>
        ///     Resolves all references for mocks in this GameObject's components recursively
        /// </summary>
        /// <param name="gameObject"></param>
        public static void FixReferences(this GameObject gameObject)
        {
            gameObject.FixReferences(false);
        }

        /// <summary>
        ///     Resolves all references for mocks in this GameObject recursively.
        ///     Can additionally traverse the transforms hierarchy to fix child GameObjects recursively.
        /// </summary>
        /// <param name="gameObject">This GameObject</param>
        /// <param name="recursive">Traverse all child transforms</param>
        public static void FixReferences(this GameObject gameObject, bool recursive)
        {
            foreach (var component in gameObject.GetComponents<Component>())
            {
                if (!(component is Transform))
                {
                    MockManager.FixReferences(component, 0);
                }
            }

            if (!recursive)
            {
                return;
            }

            foreach (Transform tf in gameObject.transform)
            {
                tf.gameObject.FixReferences(true);

                // only works with instantiated prefabs...
                /*var realPrefab = MockManager.GetRealPrefabFromMock<GameObject>(tf.gameObject);
                if (realPrefab)
                {
                    var realInstance = Object.Instantiate(realPrefab, gameObject.transform);
                    realInstance.transform.SetSiblingIndex(tf.GetSiblingIndex()+1);
                    realInstance.name = realPrefab.name;
                    realInstance.transform.localPosition = tf.localPosition;
                    realInstance.transform.localRotation = tf.localRotation;
                    realInstance.transform.localScale = tf.localScale;
                    Object.DestroyImmediate(tf.gameObject);
                }
                else
                {
                    tf.gameObject.FixReferences(true);
                }*/
            }
        }

        /// <summary>
        ///     Clones all fields from this GameObject to objectToClone.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="objectToClone"></param>
        public static void CloneFields(this GameObject gameObject, GameObject objectToClone)
        {
            const BindingFlags flags = ReflectionHelper.AllBindingFlags;

            var fieldValues = new Dictionary<FieldInfo, object>();
            var origComponents = objectToClone.GetComponentsInChildren<Component>();
            foreach (var origComponent in origComponents)
            {
                foreach (var fieldInfo in origComponent.GetType().GetFields(flags))
                {
                    if (!fieldInfo.IsLiteral && !fieldInfo.IsInitOnly)
                        fieldValues.Add(fieldInfo, fieldInfo.GetValue(origComponent));
                }

                if (!gameObject.GetComponent(origComponent.GetType()))
                {
                    gameObject.AddComponent(origComponent.GetType());
                }
            }

            var clonedComponents = gameObject.GetComponentsInChildren<Component>();
            foreach (var clonedComponent in clonedComponents)
            {
                foreach (var fieldInfo in clonedComponent.GetType().GetFields(flags))
                {
                    if (fieldValues.TryGetValue(fieldInfo, out var fieldValue))
                    {
                        fieldInfo.SetValue(clonedComponent, fieldValue);
                    }
                }
            }
        }
    }
}
