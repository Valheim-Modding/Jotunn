using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

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
            objectToFix.FixReferences(0);
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
                    component.FixReferences(0);
                }
            }

            if (!recursive)
            {
                return;
            }

            var childCount = gameObject.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform tf = gameObject.transform.GetChild(i);
                 
                var realPrefab = MockManager.GetRealPrefabFromMock<GameObject>(tf.gameObject);
                if (realPrefab)
                {
                    var realInstance = Object.Instantiate(realPrefab, gameObject.transform);
                    realInstance.transform.localPosition = tf.localPosition;
                    realInstance.transform.localRotation = tf.localRotation;
                    realInstance.transform.localScale = tf.localScale;
                    Object.Destroy(tf.gameObject);
                } else
                {
                    tf.gameObject.FixReferences(true);
                }
            }
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
                    var realPrefab = MockManager.GetRealPrefabFromMock(mock, fieldType);
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
                        var isArray = fieldType.IsArray;
                        var isList = fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>);
                        var isHashSet = fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(HashSet<>);

                        if (!(isArray || isList || isHashSet))
                        {
                            Logger.LogWarning($"Not fixing potential mock references for field {field.Name} : {fieldType} is not supported.");
                            continue;
                        }

                        var currentValues = (IEnumerable<Object>)field.GetValue(objectToFix);
                        if (currentValues != null)
                        {
                            var list = new List<Object>();
                            foreach (var unityObject in currentValues)
                            {
                                var realPrefab = MockManager.GetRealPrefabFromMock(unityObject, enumeratedType);
                                list.Add(realPrefab ? realPrefab : unityObject);
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
                                else if (isList)
                                {
                                    var toList = ReflectionHelper.Cache.EnumerableToList;
                                    var toListT = toList.MakeGenericMethod(enumeratedType);

                                    // mono...
                                    var cast = ReflectionHelper.Cache.EnumerableCast;
                                    var castT = cast.MakeGenericMethod(enumeratedType);
                                    var correctTypeList = castT.Invoke(null, new object[] { list });

                                    var newList = toListT.Invoke(null, new object[] { correctTypeList });
                                    field.SetValue(objectToFix, newList);
                                }
                                else if (isHashSet)
                                {
                                    var hash = typeof(HashSet<>).MakeGenericType(enumeratedType);
                                    
                                    // mono...
                                    var cast = ReflectionHelper.Cache.EnumerableCast;
                                    var castT = cast.MakeGenericMethod(enumeratedType);
                                    var correctTypeList = castT.Invoke(null, new object[] { list });
                                    
                                    var newHash = Activator.CreateInstance(hash, correctTypeList);
                                    field.SetValue(objectToFix, newHash);
                                }
                            }
                        }
                    }
                    else if (enumeratedType?.IsClass == true)
                    {
                        var isDict = fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>);
                        if (isDict)
                        {
                            Logger.LogWarning($"Not fixing potential mock references for field {field.Name} : Dictionary is not supported.");
                            continue;
                        }

                        var currentValues = (IEnumerable<object>)field.GetValue(objectToFix);
                        if (currentValues == null)
                        {
                            continue;
                        }
                        foreach (var value in currentValues)
                        {
                            value?.FixReferences(depth);
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
                    var realPrefab = MockManager.GetRealPrefabFromMock(mock, propertyType);
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
                        var isArray = propertyType.IsArray;
                        var isList = propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>);
                        var isHashSet = propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(HashSet<>);

                        if (!(isArray || isList || isHashSet))
                        {
                            Logger.LogWarning($"Not fixing potential mock references for property {property.Name} : {propertyType} is not supported.");
                            continue;
                        }

                        var currentValues = (IEnumerable<Object>)property.GetValue(objectToFix, null);
                        if (currentValues != null)
                        {
                            var list = new List<Object>();
                            foreach (var unityObject in currentValues)
                            {
                                var realPrefab = MockManager.GetRealPrefabFromMock(unityObject, enumeratedType);
                                list.Add(realPrefab ? realPrefab : unityObject);
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
                                else if (isList)
                                {
                                    var toList = ReflectionHelper.Cache.EnumerableToList;
                                    var toListT = toList.MakeGenericMethod(enumeratedType);

                                    // mono...
                                    var cast = ReflectionHelper.Cache.EnumerableCast;
                                    var castT = cast.MakeGenericMethod(enumeratedType);
                                    var correctTypeList = castT.Invoke(null, new object[] { list });

                                    var newList = toListT.Invoke(null, new object[] { correctTypeList });
                                    property.SetValue(objectToFix, newList, null);
                                }
                                else if (isHashSet)
                                {
                                    var hash = typeof(HashSet<>).MakeGenericType(enumeratedType);
                                    
                                    // mono...
                                    var cast = ReflectionHelper.Cache.EnumerableCast;
                                    var castT = cast.MakeGenericMethod(enumeratedType);
                                    var correctTypeList = castT.Invoke(null, new object[] { list });
                                    
                                    var newHash = Activator.CreateInstance(hash, correctTypeList);
                                    property.SetValue(objectToFix, newHash, null);
                                }
                            }
                        }
                    }
                    else if (enumeratedType?.IsClass == true)
                    {
                        var isDict = propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>);
                        if (isDict)
                        {
                            Logger.LogWarning($"Not fixing potential mock references for field {property.Name} : Dictionary is not supported.");
                            continue;
                        }

                        var currentValues = (IEnumerable<object>)property.GetValue(objectToFix, null);
                        if (currentValues == null)
                        {
                            continue;
                        }
                        foreach (var value in currentValues)
                        {
                            value?.FixReferences(depth);
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
