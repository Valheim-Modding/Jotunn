using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Jotunn.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Jotunn.Managers
{
    /// <summary>
    ///     Handles all logic to do with managing mocked prefabs added into the game.
    /// </summary>
    internal class MockManager : IManager
    {
        private static MockManager _instance;

        /// <summary>
        ///     The singleton instance of this manager.
        /// </summary>
        public static MockManager Instance => _instance ??= new MockManager();

        /// <summary>
        ///     Hide .ctor
        /// </summary>
        private MockManager() {}

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
        ///     Internal container for mocked prefabs
        /// </summary>
        internal GameObject MockPrefabContainer;

        /// <summary>
        ///     Creates the container and registers all hooks
        /// </summary>
        public void Init()
        {
            MockPrefabContainer = new GameObject("MockPrefabs");
            MockPrefabContainer.transform.parent = Main.RootObject.transform;
            MockPrefabContainer.SetActive(false);
        }

        /// <summary>
        ///     Create an empty GameObject with the mock string prepended
        /// </summary>
        /// <param name="prefabName">Name of the mocked vanilla prefab</param>
        /// <returns>Mocked GameObject reference</returns>
        public GameObject CreateMockedGameObject(string prefabName)
        {
            string name = JVLMockPrefix + prefabName;

            Transform transform = MockPrefabContainer.transform.Find(name);
            if (transform != null)
            {
                return transform.gameObject;
            }

            GameObject g = new GameObject(name);
            g.transform.parent = MockPrefabContainer.transform;
            g.SetActive(false);

            return g;
        }

        /// <summary>
        ///     Create a mocked component on an empty GameObject
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="prefabName"></param>
        /// <returns></returns>
        public T CreateMockedPrefab<T>(string prefabName) where T : Component
        {
            GameObject g = CreateMockedGameObject(prefabName);
            string name = g.name;

            T mock = g.GetOrAddComponent<T>();
            if (mock == null)
            {
                Logger.LogWarning($"Could not create mock for prefab {prefabName} of type {typeof(T)}");
                return null;
            }

            mock.name = name;

            Logger.LogDebug($"Mock {name} created");

            return mock;
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

#pragma warning disable CS0618
        /// <summary>
        ///     Will try to find the real vanilla prefab from the given mock
        /// </summary>
        /// <param name="unityObject"></param>
        /// <param name="mockObjectType"></param>
        /// <returns>the real prefab</returns>
        public static Object GetRealPrefabFromMock(Object unityObject, Type mockObjectType)
        {
            if (!unityObject)
            {
                return null;
            }

            var unityObjectName = unityObject.name;
            var isVLMock = unityObjectName.StartsWith(MockPrefix, StringComparison.Ordinal);
            var isJVLMock = unityObjectName.StartsWith(JVLMockPrefix, StringComparison.Ordinal);
            if (isVLMock || isJVLMock)
            {
                if (isVLMock) unityObjectName = unityObjectName.Substring(MockPrefix.Length);
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

                Object ret = PrefabManager.Cache.GetPrefab(mockObjectType, unityObjectName);

                if (!ret)
                {
                    throw new Exception($"Mock prefab {unityObjectName} could not be resolved");
                }

                return ret;
            }

            return null;
        }
#pragma warning restore CS0618

        // Thanks for not using the Resources folder IronGate
        // There is probably some oddities in there
        internal static void FixReferences(object objectToFix, int depth)
        {
            // This is totally arbitrary.
            // I had to add a depth because of call stack exploding otherwise
            if (depth == 3 || objectToFix == null)
            {
                return;
            }

            depth++;

            var type = objectToFix.GetType();
            ClassMember classMember = ClassMember.GetClassMember(type);

            foreach (var member in classMember.members)
            {
                FixMemberReferences(member, objectToFix, depth);
            }
        }

        private static void FixMemberReferences(MemberBase member, object objectToFix, int depth)
        {
            // Special treatment for DropTable, its a List of struct DropData
            // Maybe there comes a time when I am willing to do some generic stuff
            // But mono did not implement FieldInfo.GetValueDirect()
            if (member.MemberType == typeof(DropTable))
            {
                FixDropTable(member, objectToFix);
                return;
            }

            if (member.IsUnityObject && member.HasGetMethod)
            {
                var target = (Object)member.GetValue(objectToFix);
                var realPrefab = GetRealPrefabFromMock(target, member.MemberType);
                if (realPrefab)
                {
                    member.SetValue(objectToFix, realPrefab);
                }
            }
            else if (member.IsEnumeratedClass && member.IsEnumerableOfUnityObjects)
            {
                var isArray = member.MemberType.IsArray;
                var isList = member.MemberType.IsGenericType && member.MemberType.GetGenericTypeDefinition() == typeof(List<>);
                var isHashSet = member.MemberType.IsGenericType && member.MemberType.GetGenericTypeDefinition() == typeof(HashSet<>);

                if (!(isArray || isList || isHashSet))
                {
                    Logger.LogWarning($"Not fixing potential mock references for field {member.MemberType.Name} : {member.MemberType} is not supported.");
                    return;
                }

                var currentValues = (IEnumerable<Object>)member.GetValue(objectToFix);
                if (currentValues == null)
                {
                    return;
                }

                if (isArray || isList)
                {
                    FixIndexBasedCollection(member, currentValues);
                }
                else if (isHashSet)
                {
                    FixHashSet(member, objectToFix, currentValues);
                }
            }
            else if (member.IsEnumeratedClass)
            {
                var isDict = member.MemberType.IsGenericType && member.MemberType.GetGenericTypeDefinition() == typeof(Dictionary<,>);
                if (isDict)
                {
                    Logger.LogWarning($"Not fixing potential mock references for field {member.MemberType.Name} : Dictionary is not supported.");
                    return;
                }

                var currentValues = (IEnumerable<object>)member.GetValue(objectToFix);
                if (currentValues == null)
                {
                    return;
                }

                foreach (var value in currentValues)
                {
                    FixReferences(value, depth);
                }
            }
            else if (member.IsClass && member.HasGetMethod)
            {
                FixReferences(member.GetValue(objectToFix), depth);
            }
        }

        private static void FixHashSet(MemberBase member, object objectToFix, IEnumerable<Object> currentValues)
        {
            var list = new List<Object>();
            bool hasAnyMockResolved = false;

            foreach (var unityObject in currentValues)
            {
                var realPrefab = GetRealPrefabFromMock(unityObject, member.EnumeratedType);
                list.Add(realPrefab ? realPrefab : unityObject);
                hasAnyMockResolved = hasAnyMockResolved || realPrefab;
            }

            if (list.Count > 0 && hasAnyMockResolved)
            {
                MethodInfo cast = ReflectionHelper.Cache.EnumerableCast;
                MethodInfo castT = cast.MakeGenericMethod(member.EnumeratedType);
                object correctTypeList = castT.Invoke(null, new object[] { list });

                var hash = typeof(HashSet<>).MakeGenericType(member.EnumeratedType);

                var newHash = Activator.CreateInstance(hash, correctTypeList);
                member.SetValue(objectToFix, newHash);
            }
        }

        private static void FixIndexBasedCollection(MemberBase member, IEnumerable<Object> currentValues)
        {
            int count = ((ICollection)currentValues).Count;
            IList values = (IList)currentValues;

            for (int i = 0; i < count; i++)
            {
                var unityObject = values[i];
                var realPrefab = GetRealPrefabFromMock((Object)unityObject, member.EnumeratedType);
                if (realPrefab)
                {
                    values[i] = unityObject;
                }
            }
        }

        private static void FixDropTable(MemberBase member, object objectToFix)
        {
            var drops = ((DropTable)member.GetValue(objectToFix)).m_drops;

            for (int i = 0; i < drops.Count; i++)
            {
                var drop = drops[i];
                var realPrefab = GetRealPrefabFromMock(drop.m_item, typeof(GameObject));
                if (realPrefab)
                {
                    drop.m_item = (GameObject)realPrefab;
                }

                drops[i] = drop;
            }
        }
    }
}
