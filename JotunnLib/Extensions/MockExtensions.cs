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

        private class ClassMember
        {
            public readonly List<MemberBase> members = new List<MemberBase>();

            private static readonly Dictionary<Type, ClassMember> CachedClassMembers = new Dictionary<Type, ClassMember>();

            private ClassMember(IEnumerable<FieldInfo> fieldInfos, IEnumerable<PropertyInfo> propertyInfos)
            {
                foreach (var fieldInfo in fieldInfos)
                {
                    members.Add(new FieldMember(fieldInfo));
                }

                foreach (var propertyInfo in propertyInfos)
                {
                    members.Add(new PropertyMember(propertyInfo));
                }
            }

            private static T[] GetMembersFromType<T>(Type type, Func<Type, T[]> getMembers)
            {
                var members = getMembers(type);
                var baseType = type.BaseType;

                while (baseType != null)
                {
                    var parentMembers = getMembers(baseType);
                    members = members.Union(parentMembers).ToArray();
                    baseType = baseType.BaseType;
                }

                return members;
            }

            public static ClassMember GetClassMember(Type type)
            {
                if (CachedClassMembers.TryGetValue(type, out ClassMember classMember))
                {
                    return classMember;
                }

                const BindingFlags flags = ReflectionHelper.AllBindingFlags & ~BindingFlags.Static;
                var fields = GetMembersFromType(type, t => t.GetFields(flags));
                var properties = GetMembersFromType(type, t => t.GetProperties(flags));

                classMember = new ClassMember(fields, properties);
                CachedClassMembers[type] = classMember;
                return classMember;
            }
        }

        private abstract class MemberBase
        {
            public abstract object GetValue(object obj);
            public abstract void SetValue(object obj, object value);
            public bool HasGetMethod { get; protected set; }
            public Type MemberType { get; protected set; }
            public Type EnumeratedType { get; protected set; }
            public bool IsUnityObject { get; protected set; }
            public bool IsClass { get; protected set; }
            public bool IsEnumerableOfUnityObjects { get; protected set; }
            public bool IsEnumeratedClass { get; protected set; }
        }

        private class FieldMember : MemberBase
        {
            private readonly FieldInfo fieldInfo;

            public FieldMember(FieldInfo fieldInfo)
            {
                this.fieldInfo = fieldInfo;
                MemberType = fieldInfo.FieldType;
                IsUnityObject = MemberType.IsSameOrSubclass(typeof(Object));
                IsClass = MemberType.IsClass;
                HasGetMethod = true;
                EnumeratedType = MemberType.GetEnumeratedType();
                IsEnumerableOfUnityObjects = EnumeratedType?.IsSameOrSubclass(typeof(Object)) == true;
                IsEnumeratedClass = EnumeratedType?.IsClass == true;
            }

            public override object GetValue(object obj)
            {
                return fieldInfo.GetValue(obj);
            }

            public override void SetValue(object obj, object value)
            {
                fieldInfo.SetValue(obj, value);
            }
        }

        private class PropertyMember : MemberBase
        {
            private readonly PropertyInfo propertyInfo;

            public PropertyMember(PropertyInfo propertyInfo)
            {
                this.propertyInfo = propertyInfo;
                MemberType = propertyInfo.PropertyType;
                IsUnityObject = MemberType.IsSameOrSubclass(typeof(Object));
                IsClass = MemberType.IsClass;
                HasGetMethod = propertyInfo.GetIndexParameters().Length == 0 && propertyInfo.GetMethod != null;
                EnumeratedType = MemberType.GetEnumeratedType();
                IsEnumerableOfUnityObjects = EnumeratedType?.IsSameOrSubclass(typeof(Object)) == true;
                IsEnumeratedClass = EnumeratedType?.IsClass == true;
            }

            public override object GetValue(object obj)
            {
                return propertyInfo.GetValue(obj);
            }

            public override void SetValue(object obj, object value)
            {
                propertyInfo.SetValue(obj, value);
            }
        }

        // Thanks for not using the Resources folder IronGate
        // There is probably some oddities in there
        private static void FixReferences(this object objectToFix, int depth)
        {
            // This is totally arbitrary.
            // I had to add a depth because of call stack exploding otherwise
            if (depth == 3)
                return;

            depth++;

            var type = objectToFix.GetType();
            ClassMember classMember = ClassMember.GetClassMember(type);

            foreach (var member in classMember.members)
            {
                // Special treatment for DropTable, its a List of struct DropData
                // Maybe there comes a time when I am willing to do some generic stuff
                // But mono did not implement FieldInfo.GetValueDirect()
                if (member.MemberType == typeof(DropTable))
                {
                    var drops = ((DropTable)member.GetValue(objectToFix)).m_drops;

                    for (int i = 0; i < drops.Count; i++)
                    {
                        var drop = drops[i];
                        var realPrefab = MockManager.GetRealPrefabFromMock(drop.m_item, typeof(GameObject));
                        if (realPrefab)
                        {
                            drop.m_item = (GameObject)realPrefab;
                        }

                        drops[i] = drop;
                    }

                    continue;
                }

                if (member.IsUnityObject && member.HasGetMethod)
                {
                    var target = (Object)member.GetValue(objectToFix);
                    var realPrefab = MockManager.GetRealPrefabFromMock(target, member.MemberType);
                    if (realPrefab)
                    {
                        member.SetValue(objectToFix, realPrefab);
                    }
                }
                else
                {
                    if (member.IsEnumeratedClass && member.IsEnumerableOfUnityObjects)
                    {
                        var isArray = member.MemberType.IsArray;
                        var isList = member.MemberType.IsGenericType && member.MemberType.GetGenericTypeDefinition() == typeof(List<>);
                        var isHashSet = member.MemberType.IsGenericType && member.MemberType.GetGenericTypeDefinition() == typeof(HashSet<>);

                        if (!(isArray || isList || isHashSet))
                        {
                            Logger.LogWarning($"Not fixing potential mock references for field {member.MemberType.Name} : {member.MemberType} is not supported.");
                            continue;
                        }

                        var currentValues = (IEnumerable<Object>)member.GetValue(objectToFix);
                        if (currentValues != null)
                        {
                            var list = new List<Object>();
                            foreach (var unityObject in currentValues)
                            {
                                var realPrefab = MockManager.GetRealPrefabFromMock(unityObject, member.EnumeratedType);
                                list.Add(realPrefab ? realPrefab : unityObject);
                            }

                            if (list.Count > 0)
                            {
                                MethodInfo cast = ReflectionHelper.Cache.EnumerableCast;
                                MethodInfo castT = cast.MakeGenericMethod(member.EnumeratedType);
                                object correctTypeList = castT.Invoke(null, new object[] { list });

                                if (isArray)
                                {
                                    var toArray = ReflectionHelper.Cache.EnumerableToArray;
                                    var toArrayT = toArray.MakeGenericMethod(member.EnumeratedType);

                                    var array = toArrayT.Invoke(null, new object[] { correctTypeList });
                                    member.SetValue(objectToFix, array);
                                }
                                else if (isList)
                                {
                                    var toList = ReflectionHelper.Cache.EnumerableToList;
                                    var toListT = toList.MakeGenericMethod(member.EnumeratedType);

                                    var newList = toListT.Invoke(null, new object[] { correctTypeList });
                                    member.SetValue(objectToFix, newList);
                                }
                                else if (isHashSet)
                                {
                                    var hash = typeof(HashSet<>).MakeGenericType(member.EnumeratedType);

                                    var newHash = Activator.CreateInstance(hash, correctTypeList);
                                    member.SetValue(objectToFix, newHash);
                                }
                            }
                        }
                    }
                    else if (member.IsEnumeratedClass)
                    {
                        var isDict = member.MemberType.IsGenericType && member.MemberType.GetGenericTypeDefinition() == typeof(Dictionary<,>);
                        if (isDict)
                        {
                            Logger.LogWarning($"Not fixing potential mock references for field {member.MemberType.Name} : Dictionary is not supported.");
                            continue;
                        }

                        var currentValues = (IEnumerable<object>)member.GetValue(objectToFix);
                        if (currentValues == null)
                        {
                            continue;
                        }

                        foreach (var value in currentValues)
                        {
                            value?.FixReferences(depth);
                        }
                    }
                    else if (member.IsClass && member.HasGetMethod)
                    {
                        member.GetValue(objectToFix)?.FixReferences(depth);
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
