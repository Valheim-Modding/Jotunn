using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Linq;
using UnityEngine.SceneManagement;

namespace JotunnLib.Managers
{
    /// <summary>
    ///     Handles all logic to do with managing custom prefabs added into the game.
    /// </summary>
    public class PrefabManager : Manager
    {
        /// <summary>
        ///     The singleton instance of this manager.
        /// </summary>
        public static PrefabManager Instance { get; private set; }

        public event EventHandler PrefabsLoaded;
        internal GameObject PrefabContainer;
        internal Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();

        private bool loaded = false;

        private void Awake()
        {
            if (Instance != null)
            {
                Logger.LogError($"Cannot have multiple instances of singleton: {GetType()}");
                return;
            }

            Instance = this;
        }

        internal override void Init()
        {
            On.ZNetScene.Awake += registerAllToZNetScene;

            PrefabContainer = new GameObject("Prefabs");
            PrefabContainer.transform.parent = Main.RootObject.transform;
            PrefabContainer.SetActive(false);

            SceneManager.sceneUnloaded += (Scene current) => Cache.clearCache();
        }

        /*private string createUID()
        {
            const char separator = '_';

            var methodBase = new System.Diagnostics.StackFrame(2).GetMethod();
            var id = methodBase.DeclaringType.Assembly.GetName().Name + separator + methodBase.DeclaringType.Name + separator + methodBase.Name;

            return separator + id;
        }*/

        /// <summary>
        ///     Adds a prefab to the manager. Added prefabs get registered to the <see cref="ZNetScene"/> on Awake().
        /// </summary>
        /// <param name="name"></param>
        /// <param name="prefab"></param>
        public void AddPrefab(GameObject prefab)
        {
            if (Prefabs.ContainsKey(prefab.name))
            {
                Logger.LogWarning($"Prefab '{prefab.name}' already exists");
                return;
            }

            prefab.transform.SetParent(PrefabContainer.transform, false);
            //prefab.SetActive(true);
            Prefabs.Add(prefab.name, prefab);
        }

        /// <summary>
        ///     Creates a new prefab that's an empty primitive.
        /// </summary>
        /// <param name="name">The name of the new GameObject</param>
        /// <param name="addZNetView" >
        ///     When true a ZNetView component is added to the new GameObject for ZDO generation and networking. Default: true
        /// </param>
        /// <returns>The newly created empty prefab</returns>
        public GameObject CreateEmptyPrefab(string name, bool addZNetView = true)
        {
            if (string.IsNullOrEmpty(name))
            {
                Logger.LogError($"Failed to create prefab with invalid name: {name}");
                return null;
            }

            if (GetPrefab(name))
            {
                Logger.LogError($"Failed to create prefab, name already exists: {name}");
                return null;
            }

            GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //prefab.name = name + createUID();
            prefab.name = name;
            prefab.transform.parent = PrefabContainer.transform;

            if (addZNetView)
            {
                // Add ZNetView and make prefabs persistent by default
                ZNetView newView = prefab.AddComponent<ZNetView>();
                newView.m_persistent = true;
            }

            return prefab;
        }

        /// <summary>
        ///     Allows you to clone a given prefab without modifying the original.
        /// </summary>
        /// <param name="name">New prefab name</param>
        /// <param name="baseName">Base prefab name</param>
        /// <returns>Newly created prefab object</returns>
        public GameObject CreateClonedPrefab(string name, string baseName)
        {
            if (string.IsNullOrEmpty(baseName))
            {
                Logger.LogError($"Failed to clone prefab with invalid baseName: {baseName}");
                return null;
            }

            // Try to get the prefab in local Dictionary or ZNetScene (if available)
            GameObject prefab = GetPrefab(baseName);

            // Try to get the prefab from the PrefabCache
            if (!prefab)
            {
                prefab = Cache.GetPrefab<GameObject>(baseName);
            }
            if (!prefab)
            {
                Logger.LogError($"Failed to clone prefab, can not find base prefab with name: {baseName}");
                return null;
            }

            return CreateClonedPrefab(name, prefab);
        }

        /// <summary>
        ///     Allows you to clone a given prefab without modifying the original.
        /// </summary>
        /// <param name="name">New prefab name</param>
        /// <param name="prefab">Base prefab</param>
        /// <returns>Newly created cloned prefab object</returns>
        public GameObject CreateClonedPrefab(string name, GameObject prefab)
        {
            if (string.IsNullOrEmpty(name))
            {
                Logger.LogError($"Failed to clone prefab with invalid name: {name}");
                return null;
            }
            if (!prefab)
            {
                Logger.LogError($"Failed to clone prefab, base prefab is not valid");
                return null;
            }
            if (GetPrefab(name))
            {
                Logger.LogWarning($"Failed to clone prefab, name already exists: {name}");
                return null;
            }

            var newPrefab = Instantiate(prefab, PrefabContainer.transform);
            //newPrefab.name = name + createUID();
            newPrefab.name = name;

            return newPrefab;
        }

        /// <summary>
        ///     Returns an existing prefab with given name, or null if none exist.
        /// </summary>
        /// <param name="name">Name of the prefab to search for</param>
        /// <returns>The existing prefab, or null if none exists with given name</returns>
        public GameObject GetPrefab(string name)
        {
            if (Prefabs.ContainsKey(name))
            {
                return Prefabs[name];
            }

            if (ZNetScene.instance)
            {
                foreach (GameObject obj in ZNetScene.instance.m_prefabs)
                {
                    if (obj.name == name)
                    {
                        return obj;
                    }
                }
            }

            return null;
        }

        /// <summary>
        ///     Destroy a known custom prefab. Removes it from the manager and if found also on the <see cref="ZNetScene"/>.
        /// </summary>
        /// <param name="name">The name of the prefab to destroy</param>
        public void DestroyPrefab(string name)
        {
            if (Prefabs.ContainsKey(name))
            {
                Prefabs.Remove(name);
            }

            if (ZNetScene.instance)
            {
                GameObject del = null;
                foreach (GameObject obj in ZNetScene.instance.m_prefabs)
                {
                    if (obj.name == name)
                    {
                        break;
                    }
                }

                if (del)
                {
                    ZNetScene.instance.m_prefabs.Remove(del);
                    ZNetScene.instance.Destroy(del);
                }
            }
        }

        /// <summary>
        ///     Add all registered prefabs to the namedPrefabs in <see cref="ZNetScene" />.
        /// </summary>
        /// <param name="instance"></param>
        private void registerAllToZNetScene(On.ZNetScene.orig_Awake orig, ZNetScene self)
        {
            orig(self);

            Logger.LogInfo($"---- Adding custom prefabs to {self} ----");

            if (self && Instance.Prefabs.Count > 0)
            {
                foreach (var prefab in Instance.Prefabs)
                {
                    var name = prefab.Key;

                    RegisterToZNetScene(name, prefab.Value);
                }
            }

            // Send event that all prefabs are loaded
            if (!loaded)
            {
                PrefabsLoaded?.Invoke(null, EventArgs.Empty);
            }

            loaded = true;
        }

        /// <summary>
        ///     Add a single prefab to the <see cref="ZNetScene"/>.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="gameObject"></param>
        public void RegisterToZNetScene(string name, GameObject gameObject)
        {
            var znet = ZNetScene.instance;

            if (znet)
            {
                if (znet.m_namedPrefabs.ContainsKey(name.GetStableHashCode()))
                {
                    Logger.LogWarning($"Prefab {name} already in ZNetScene");
                }
                else
                { 
                    znet.m_prefabs.Add(gameObject);
                    znet.m_namedPrefabs.Add(name.GetStableHashCode(), gameObject);
                    Logger.LogInfo($"Added prefab {name}");
                }
            }
        }

        /// <summary>
        ///     The global cache of prefabs per scene.
        /// </summary>
        public static class Cache
        {
            private static readonly Dictionary<Type, Dictionary<string, Object>> dictionaryCache =
                new Dictionary<Type, Dictionary<string, Object>>();

            /// <summary>
            ///     Get an instance of an Unity Object from the current scene with the given name.
            /// </summary>
            /// <param name="type"></param>
            /// <param name="name"></param>
            /// <returns></returns>
            public static Object GetPrefab(Type type, string name)
            {
                if (dictionaryCache.TryGetValue(type, out var map))
                {
                    if (map.TryGetValue(name, out var unityObject))
                    {
                        return unityObject;
                    }
                }
                else
                {
                    initCache(type);
                    return GetPrefab(type, name);
                }

                return null;
            }

            /// <summary>
            ///     Get an instance of an Unity Object from the current scene with the given name.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="name"></param>
            /// <returns></returns>
            public static T GetPrefab<T>(string name) where T : Object
            {
                return (T)GetPrefab(typeof(T), name);
            }

            /// <summary>
            ///     Get the instances of UnityObjects from the current scene with the given type.
            /// </summary>
            /// <param name="type"></param>
            /// <returns></returns>
            public static Dictionary<string, Object> GetPrefabs(Type type)
            {
                if (dictionaryCache.TryGetValue(type, out var map))
                {
                    return map;
                }
                else
                {
                    initCache(type);
                    return GetPrefabs(type);
                }
            }

            private static void initCache(Type type, Dictionary<string, Object> map = null)
            {
                map ??= new Dictionary<string, Object>();
                foreach (var unityObject in Resources.FindObjectsOfTypeAll(type))
                {
                    map[unityObject.name] = unityObject;
                }

                dictionaryCache[type] = map;
            }

            internal static void clearCache()
            {
                dictionaryCache.Clear();
            }
        }
    }
}