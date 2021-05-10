﻿using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Jotunn
{
    internal static class ItemDropMockFix
    {
        private static bool _enabled;

        internal static void Switch(bool enable)
        {
            if (enable)
            {
                if (!_enabled)
                {
                    On.ItemDrop.Awake += SilenceErrors;
                    _enabled = enable;
                }
            }
            else
            {
                On.ItemDrop.Awake -= SilenceErrors;
                _enabled = enable;
            }
        }

        private static void SilenceErrors(On.ItemDrop.orig_Awake orig, ItemDrop self)
        {
            try
            {
                orig(self);
            }
            catch (Exception)
            {

            }
        }

        internal static bool IsValid(this ObjectDB self)
        {
            return self.m_items.Count > 0;
        }
    }

    /// <summary>
    ///     Extends prefab GameObjects with functionality related to the mocking system.
    /// </summary>
    public static class PrefabExtension
    {
        /// <summary>
        ///     Legacy ValheimLib prefix used by the Mock System to recognize Mock gameObject that must be replaced at some point.
        /// </summary>
        [Obsolete("Legacy ValheimLib mock prefix. Use JVLMockPrefix \"JVLmock_\" instead.")]
        public const string MockPrefix = "VLmock_";

        /// <summary>
        ///     Prefix used by the Mock System to recognize Mock gameObject that must be replaced at some point.
        /// </summary>
        public const string JVLMockPrefix = "JVLmock_";

        /// <summary>
        ///     Will try to find the real vanilla prefab from the given mock
        /// </summary>
        /// <param name="unityObject"></param>
        /// <param name="mockObjectType"></param>
        /// <returns>the real prefab</returns>
        public static Object GetRealPrefabFromMock(Object unityObject, Type mockObjectType)
        {
            if (unityObject)
            {
                var unityObjectName = unityObject.name;
#pragma warning disable CS0618
                var isVLMock = unityObjectName.StartsWith(MockPrefix);
#pragma warning restore CS0618
                var isJVLMock = unityObjectName.StartsWith(JVLMockPrefix);
                if (isVLMock || isJVLMock)
                {
#pragma warning disable CS0618
                    if (isVLMock) unityObjectName = unityObjectName.Substring(MockPrefix.Length);
#pragma warning restore CS0618
                    if (isJVLMock) unityObjectName = unityObjectName.Substring(JVLMockPrefix.Length);

                    // Cut off the suffix in the name to correctly query the original material
                    if (unityObject is Material)
                    {
                        const string materialInstance = " (Instance)";
                        if (unityObjectName.EndsWith(materialInstance))
                        {
                            unityObjectName = unityObjectName.Substring(0, unityObjectName.Length - materialInstance.Length);
                        }
                    }

                    return PrefabManager.Cache.GetPrefab(mockObjectType, unityObjectName);
                }
            }

            return null;
        }

        /// <summary>
        ///     Will try to find the real vanilla prefab from the given mock
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="unityObject"></param>
        /// <returns>the real prefab</returns>
        public static T GetRealPrefabFromMock<T>(Object unityObject) where T : Object
        {
            return (T)GetRealPrefabFromMock(unityObject, typeof(T));
        }

        /// <summary>
        ///     Will attempt to fix every field that are mocks gameObjects / Components from the given object.
        /// </summary>
        /// <param name="objectToFix"></param>
        public static void FixReferences(this object objectToFix)
        {
            objectToFix.FixReferences(0);
        }

        // Thanks for not using the Resources folder IronGate
        // There is probably some oddities in there
        private static void FixReferences(this object objectToFix, int depth = 0)
        {
            // This is totally arbitrary.
            // I had to add a depth because of call stack exploding otherwise
            if (depth == 3)
                return;

            depth++;

            var type = objectToFix.GetType();

            const BindingFlags flags = ReflectionHelper.AllBindingFlags & ~BindingFlags.Static;

            var fields = type.GetFields(flags);
            var baseType = type.BaseType;
            while (baseType != null)
            {
                var parentFields = baseType.GetFields(flags);
                fields = fields.Union(parentFields).ToArray();
                baseType = baseType.BaseType;
            }
            foreach (var field in fields)
            {
                var fieldType = field.FieldType;

                var isUnityObject = fieldType.IsSameOrSubclass(typeof(Object));
                if (isUnityObject)
                {
                    var mock = (Object)field.GetValue(objectToFix);
                    var realPrefab = GetRealPrefabFromMock(mock, fieldType);
                    if (realPrefab)
                    {
                        field.SetValue(objectToFix, realPrefab);
                    }
                }
                else
                {
                    var enumeratedType = fieldType.GetEnumeratedType();
                    var isEnumerableOfUnityObjects = enumeratedType?.IsSameOrSubclass(typeof(Object)) == true;
                    if (isEnumerableOfUnityObjects)
                    {
                        var currentValues = (IEnumerable<Object>)field.GetValue(objectToFix);
                        if (currentValues != null)
                        {
                            var isArray = fieldType.IsArray;
                            var newI = isArray ? (IEnumerable<Object>)Array.CreateInstance(enumeratedType, currentValues.Count()) : (IEnumerable<Object>)Activator.CreateInstance(fieldType);
                            var list = new List<Object>();
                            foreach (var unityObject in currentValues)
                            {
                                var realPrefab = GetRealPrefabFromMock(unityObject, enumeratedType);
                                if (realPrefab)
                                {
                                    list.Add(realPrefab);
                                }
                            }

                            if (list.Count > 0)
                            {
                                if (isArray)
                                {
                                    var toArray = ReflectionHelper.Cache.EnumerableToArray;
                                    var toArrayT = toArray.MakeGenericMethod(enumeratedType);

                                    // mono...
                                    var cast = ReflectionHelper.Cache.EnumerableCast;
                                    var castT = cast.MakeGenericMethod(enumeratedType);
                                    var correctTypeList = castT.Invoke(null, new object[] { list });

                                    var array = toArrayT.Invoke(null, new object[] { correctTypeList });
                                    field.SetValue(objectToFix, array);
                                }
                                else
                                {
                                    field.SetValue(objectToFix, newI.Concat(list));
                                }
                            }
                        }
                    }
                    else if (enumeratedType?.IsClass == true)
                    {
                        var currentValues = (IEnumerable<object>)field.GetValue(objectToFix);
                        foreach (var value in currentValues)
                        {
                            value.FixReferences(depth);
                        }
                    }
                    else if (fieldType.IsClass)
                    {
                        field.GetValue(objectToFix)?.FixReferences(depth);
                    }
                }
            }

            var properties = type.GetProperties(flags).ToList();
            baseType = type.BaseType;
            if (baseType != null)
            {
                var parentProperties = baseType.GetProperties(flags).ToList();
                foreach (var a in parentProperties)
                    properties.Add(a);
            }
            foreach (var property in properties)
            {
                var propertyType = property.PropertyType;

                var isUnityObject = propertyType.IsSameOrSubclass(typeof(Object));
                if (isUnityObject)
                {
                    var mock = (Object)property.GetValue(objectToFix, null);
                    var realPrefab = GetRealPrefabFromMock(mock, propertyType);
                    if (realPrefab)
                    {
                        property.SetValue(objectToFix, realPrefab, null);
                    }
                }
                else
                {
                    var enumeratedType = propertyType.GetEnumeratedType();
                    var isEnumerableOfUnityObjects = enumeratedType?.IsSameOrSubclass(typeof(Object)) == true;
                    if (isEnumerableOfUnityObjects)
                    {
                        var currentValues = (IEnumerable<Object>)property.GetValue(objectToFix, null);
                        if (currentValues != null)
                        {
                            var isArray = propertyType.IsArray;
                            var newI = isArray ? (IEnumerable<Object>)Array.CreateInstance(enumeratedType, currentValues.Count()) : (IEnumerable<Object>)Activator.CreateInstance(propertyType);
                            var list = new List<Object>();
                            foreach (var unityObject in currentValues)
                            {
                                var realPrefab = GetRealPrefabFromMock(unityObject, enumeratedType);
                                if (realPrefab)
                                {
                                    list.Add(realPrefab);
                                }
                            }

                            if (list.Count > 0)
                            {
                                if (isArray)
                                {
                                    var toArray = ReflectionHelper.Cache.EnumerableToArray;
                                    var toArrayT = toArray.MakeGenericMethod(enumeratedType);

                                    // mono...
                                    var cast = ReflectionHelper.Cache.EnumerableCast;
                                    var castT = cast.MakeGenericMethod(enumeratedType);
                                    var correctTypeList = castT.Invoke(null, new object[] { list });

                                    var array = toArrayT.Invoke(null, new object[] { correctTypeList });
                                    property.SetValue(objectToFix, array, null);
                                }
                                else
                                {
                                    property.SetValue(objectToFix, newI.Concat(list), null);
                                }
                            }
                        }
                    }
                    else if (enumeratedType?.IsClass == true)
                    {
                        var currentValues = (IEnumerable<object>)property.GetValue(objectToFix, null);
                        foreach (var value in currentValues)
                        {
                            value.FixReferences(depth);
                        }
                    }
                    else if (propertyType.IsClass)
                    {
                        if (property.GetIndexParameters().Length == 0)
                        {
                            property.GetValue(objectToFix, null)?.FixReferences(depth);
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Resolves all references for mocks in this GameObject recursively.
        /// </summary>
        /// <param name="gameObject"></param>
        public static void FixReferences(this GameObject gameObject)
        {
            foreach (var component in gameObject.GetComponents<Component>())
            {
                component.FixReferences();
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
