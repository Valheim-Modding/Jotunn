using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using JotunnLib.Utils;
using UnityObject = UnityEngine.Object;

namespace JotunnLib.Managers
{
    

    public static class PrefabManagerVL
    {
        /// <summary>
        /// Name of the Root GameObject that have as childs every Modded GameObject done through InstantiateClone.
        /// </summary>
        public const string ModdedPrefabsParentName = "ModdedPrefabs";

        /// <summary>
        /// Prefix used by the Mock System to recognize Mock gameObject that must be replaced at some point.
        /// </summary>
        public const string MockPrefix = "VLmock_";

        internal static List<WeakReference> NetworkedModdedPrefabs = new List<WeakReference>();

        private static GameObject _parent;
        /// <summary>
        /// Parent is the Root GameObject that have as childs every Modded GameObject done through InstantiateClone.
        /// Feel free to add your modded prefabs here too
        /// </summary>
        public static GameObject Parent
        {
            get
            {
                if (!_parent)
                {
                    _parent = new GameObject(ModdedPrefabsParentName);
                    UnityObject.DontDestroyOnLoad(_parent);
                    _parent.SetActive(false);
                }

                return _parent;
            }
        }

        internal static void Init()
        {
            On.ZNetScene.Awake += AddCustomPrefabsToZNetSceneDictionary;
        }

        private static void AddPrefab(this ZNetScene self, GameObject prefab)
        {
            self.m_namedPrefabs.Add(prefab.name.GetStableHashCode(), prefab);
        }

        private static void AddCustomPrefabsToZNetSceneDictionary(On.ZNetScene.orig_Awake orig, ZNetScene self)
        {
            orig(self);

            if (self)
            {
                foreach (var weakReference in NetworkedModdedPrefabs)
                {
                    if (weakReference.IsAlive)
                    {
                        var prefab = (GameObject)weakReference.Target;
                        if (prefab)
                        {
                            self.AddPrefab(prefab);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Allow you to register to the ZNetScene list so that its correctly networked by the game.
        /// </summary>
        /// <param name="prefab">Prefab to register to the ZNetScene list</param>
        public static void NetworkRegister(this GameObject prefab)
        {
            NetworkedModdedPrefabs.Add(new WeakReference(prefab));

            var zNetScene = ZNetScene.instance;
            if (zNetScene)
            {
                zNetScene.AddPrefab(prefab);
            }
        }

        /// <summary>
        /// Allow you to clone a given prefab without modifying the original. Also handle the networking and unique naming.
        /// </summary>
        /// <param name="gameObject">prefab that you want to clone</param>
        /// <param name="nameToSet">name for the new clone</param>
        /// <param name="zNetRegister">Must be true if you want to have the prefab correctly networked and handled by the ZDO system. True by default</param>
        /// <returns></returns>
        public static GameObject InstantiateClone(this GameObject gameObject, string nameToSet, bool zNetRegister = true)
        {
            const char separator = '_';

            var methodBase = new System.Diagnostics.StackTrace().GetFrame(1).GetMethod();
            var id = methodBase.DeclaringType.Assembly.GetName().Name + separator + methodBase.DeclaringType.Name + separator + methodBase.Name;

            var prefab = UnityObject.Instantiate(gameObject, Parent.transform);
            prefab.name = nameToSet + separator + id;

            if (zNetRegister)
            {
                prefab.NetworkRegister();
            }

            return prefab;
        }

        /// <summary>
        /// Will try to find the real vanilla prefab from the given mock
        /// </summary>
        /// <param name="unityObject"></param>
        /// <param name="mockObjectType"></param>
        /// <returns>the real prefab</returns>
        public static UnityObject GetRealPrefabFromMock(UnityObject unityObject, Type mockObjectType)
        {
            if (unityObject)
            {
                var unityObjectName = unityObject.name;
                var isMock = unityObjectName.StartsWith(MockPrefix);
                if (isMock)
                {
                    unityObjectName = unityObjectName.Substring(MockPrefix.Length);

                    // Cut off the suffix in the name to correctly query the original material
                    if (unityObject is Material)
                    {
                        const string materialInstance = " (Instance)";
                        if (unityObjectName.EndsWith(materialInstance))
                        {
                            unityObjectName = unityObjectName.Substring(0, unityObjectName.Length - materialInstance.Length);
                            JotunnLib.Logger.LogError(unityObjectName);
                        }
                    }

                    return Cache.GetPrefab(mockObjectType, unityObjectName);
                }
            }

            return null;
        }

        /// <summary>
        /// Will try to find the real vanilla prefab from the given mock
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="unityObject"></param>
        /// <returns>the real prefab</returns>
        public static T GetRealPrefabFromMock<T>(UnityObject unityObject) where T : UnityObject
        {
            return (T)GetRealPrefabFromMock(unityObject, typeof(T));
        }


        /// <summary>
        /// Will attempt to fix every field that are mocks gameObjects / Components from the given object.
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

                var isUnityObject = fieldType.IsSameOrSubclass(typeof(UnityObject));
                if (isUnityObject)
                {
                    var mock = (UnityObject)field.GetValue(objectToFix);
                    var realPrefab = GetRealPrefabFromMock(mock, fieldType);
                    if (realPrefab)
                    {
                        field.SetValue(objectToFix, realPrefab);
                    }
                }
                else
                {
                    var enumeratedType = fieldType.GetEnumeratedType();
                    var isEnumerableOfUnityObjects = enumeratedType?.IsSameOrSubclass(typeof(UnityObject)) == true;
                    if (isEnumerableOfUnityObjects)
                    {
                        var currentValues = (IEnumerable<UnityObject>)field.GetValue(objectToFix);
                        if (currentValues != null)
                        {
                            var isArray = fieldType.IsArray;
                            var newI = isArray ? (IEnumerable<UnityObject>)Array.CreateInstance(enumeratedType, currentValues.Count()) : (IEnumerable<UnityObject>)Activator.CreateInstance(fieldType);
                            var list = new List<UnityObject>();
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

                var isUnityObject = propertyType.IsSameOrSubclass(typeof(UnityObject));
                if (isUnityObject)
                {
                    var mock = (UnityObject)property.GetValue(objectToFix, null);
                    var realPrefab = GetRealPrefabFromMock(mock, propertyType);
                    if (realPrefab)
                    {
                        property.SetValue(objectToFix, realPrefab, null);
                    }
                }
                else
                {
                    var enumeratedType = propertyType.GetEnumeratedType();
                    var isEnumerableOfUnityObjects = enumeratedType?.IsSameOrSubclass(typeof(UnityObject)) == true;
                    if (isEnumerableOfUnityObjects)
                    {
                        var currentValues = (IEnumerable<UnityObject>)property.GetValue(objectToFix, null);
                        if (currentValues != null)
                        {
                            var isArray = propertyType.IsArray;
                            var newI = isArray ? (IEnumerable<UnityObject>)Array.CreateInstance(enumeratedType, currentValues.Count()) : (IEnumerable<UnityObject>)Activator.CreateInstance(propertyType);
                            var list = new List<UnityObject>();
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
        /// Fix the components fields of a given gameObject
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
        /// Will clone all fields from gameObject to objectToClone
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

        /// <summary>
        /// Helper class for caching gameobjects in the current scene.
        /// </summary>
        public static class Cache
        {
            private static readonly Dictionary<Type, Dictionary<string, UnityObject>> DictionaryCache =
                new Dictionary<Type, Dictionary<string, UnityObject>>();

            internal static ConditionalWeakTable<Inventory, Container> InventoryToContainer = new ConditionalWeakTable<Inventory, Container>();

            private static void InitCache(Type type, Dictionary<string, UnityObject> map = null)
            {
                map ??= new Dictionary<string, UnityObject>();
                foreach (var unityObject in Resources.FindObjectsOfTypeAll(type))
                {
                    map[unityObject.name] = unityObject;
                }

                DictionaryCache[type] = map;
            }

            /// <summary>
            /// Get an instance of an UnityObject from the current scene with the given name
            /// </summary>
            /// <param name="type"></param>
            /// <param name="name"></param>
            /// <returns></returns>
            public static UnityObject GetPrefab(Type type, string name)
            {
                if (DictionaryCache.TryGetValue(type, out var map))
                {
                    if (map.Count == 0 || !map.Values.First())
                    {
                        InitCache(type, map);
                    }

                    if (map.TryGetValue(name, out var unityObject))
                    {
                        return unityObject;
                    }
                }
                else
                {
                    InitCache(type);
                    return GetPrefab(type, name);
                }

                return null;
            }

            /// <summary>
            /// Get an instance of an UnityObject from the current scene with the given name
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="name"></param>
            /// <returns></returns>
            public static T GetPrefab<T>(string name) where T : UnityObject
            {
                return (T)GetPrefab(typeof(T), name);
            }

            /// <summary>
            /// Get the instances of UnityObjects from the current scene with the given type
            /// </summary>
            /// <param name="type"></param>
            /// <returns></returns>
            public static Dictionary<string, UnityObject> GetPrefabs(Type type)
            {
                if (DictionaryCache.TryGetValue(type, out var map))
                {
                    if (map.Count == 0 || !map.Values.First())
                    {
                        InitCache(type, map);
                    }

                    return map;
                }
                else
                {
                    InitCache(type);
                    return GetPrefabs(type);
                }
            }
        }
    }
}