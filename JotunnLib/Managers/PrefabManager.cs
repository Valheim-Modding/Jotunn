using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace JotunnLib.Managers
{
    /// <summary>
    ///     Manager for handling custom prefabs added to the game.
    /// </summary>
    public class PrefabManager : Manager
    {
        /// <summary>
        ///     The singleton instance of this manager.
        /// </summary>
        public static PrefabManager Instance { get; private set; }

        /// <summary>
        ///     One time event called after adding the prefabs to <see cref="ZNetScene"/> for the first time.
        /// </summary>
        public event EventHandler PrefabsLoaded;

        /// <summary>
        ///     Container for custom prefabs in the DontDestroyOnLoad scene.
        /// </summary>
        internal GameObject PrefabContainer;

        /// <summary>
        /// Dictionary of all added custom prefabs by name.
        /// </summary>
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
            On.ZNetScene.Awake += RegisterAllToZNetScene;

            PrefabContainer = new GameObject("Prefabs");
            PrefabContainer.transform.parent = Main.RootObject.transform;
            PrefabContainer.SetActive(false);

            SceneManager.sceneUnloaded += (Scene current) => Cache.ClearCache();
        }

        /// <summary>
        ///     Add a custom prefab to the manager.<br />
        ///     Checks if a prefab with the same name is already added.<br />
        ///     Added prefabs get registered to the <see cref="ZNetScene"/> on Awake().
        /// </summary>
        /// <param name="prefab">Prefab to add.</param>
        public void AddPrefab(GameObject prefab)
        {
            if (Prefabs.ContainsKey(prefab.name))
            {
                Logger.LogWarning($"Prefab '{prefab.name}' already exists");
                return;
            }

            prefab.transform.SetParent(PrefabContainer.transform, false);
            Prefabs.Add(prefab.name, prefab);
        }

        /// <summary>
        ///     Create a new prefab that's an empty primitive.
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
        ///     Create a copy of a given prefab without modifying the original.
        /// </summary>
        /// <param name="name">Name of the new prefab.</param>
        /// <param name="baseName">Name of the vanilla prefab to copy from.</param>
        /// <returns>Newly created prefab object.</returns>
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
        ///     Create a copy of a given prefab without modifying the original.
        /// </summary>
        /// <param name="name">Name of the new prefab.</param>
        /// <param name="prefab">Prefab instance to copy.</param>
        /// <returns>Newly created prefab object.</returns>
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
            newPrefab.name = name;

            return newPrefab;
        }

        /// <summary>
        ///     Get a prefab by its name.<br /><br />
        ///     Search hierarchy:
        ///     <list type="number">
        ///         <item>Custom prefab with the exact name</item>
        ///         <item>Vanilla prefab with the exact name if <see cref="ZNetScene"/> is already instantiated</item>
        ///     </list>
        /// </summary>
        /// <param name="name">Name of the prefab to search for.</param>
        /// <returns>The existing prefab, or null if none exists with given name.</returns>
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
        ///     Remove a custom prefab from the manager.
        /// </summary>
        /// <param name="name">Name of the prefab to remove.</param>
        public void RemovePrefab(string name)
        {
            if (Prefabs.ContainsKey(name))
            {
                Prefabs.Remove(name);
            }
        }

        /// <summary>
        ///     Destroy a custom prefab.<br />
        ///     Removes it from the manager and if instantiated also from the <see cref="ZNetScene"/>.
        /// </summary>
        /// <param name="name">The name of the prefab to destroy.</param>
        public void DestroyPrefab(string name)
        {
            RemovePrefab(name);

            if (ZNetScene.instance)
            {
                //TODO: remove all clones, too

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
        ///     Register all custom prefabs to m_prefabs/m_namedPrefabs in <see cref="ZNetScene" />.
        /// </summary>
        private void RegisterAllToZNetScene(On.ZNetScene.orig_Awake orig, ZNetScene self)
        {
            orig(self);

            Logger.LogInfo($"---- Adding custom prefabs to {self} ----");

            if (self && Instance.Prefabs.Count > 0)
            {
                foreach (var prefab in Instance.Prefabs)
                {
                    RegisterToZNetScene(prefab.Value);
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
        ///     Register a single prefab to the current <see cref="ZNetScene"/>.<br />
        ///     Checks for existance of the object via GetStableHashCode() and adds the prefab if it is not already added.
        /// </summary>
        /// <param name="gameObject"></param>
        public void RegisterToZNetScene(GameObject gameObject)
        {
            ZNetScene znet = ZNetScene.instance;

            if (znet)
            {
                string name = gameObject.name;
                int hash = name.GetStableHashCode();

                if (znet.m_namedPrefabs.ContainsKey(hash))
                {
                    Logger.LogWarning($"Prefab {name} already in ZNetScene");
                }
                else
                {
                    znet.m_prefabs.Add(gameObject);
                    znet.m_namedPrefabs.Add(hash, gameObject);
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
            /// <param name="type"><see cref="Type"/> to search for.</param>
            /// <param name="name">Name of the actual object to search for.</param>
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
                    InitCache(type);
                    return GetPrefab(type, name);
                }

                return null;
            }

            /// <summary>
            ///     Get an instance of an Unity Object from the current scene by name.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="name"></param>
            /// <returns></returns>
            public static T GetPrefab<T>(string name) where T : Object
            {
                return (T)GetPrefab(typeof(T), name);
            }

            /// <summary>
            ///     Get all instances of an Unity Object from the current scene by type.
            /// </summary>
            /// <param name="type"><see cref="Type"/> to search for.</param>
            /// <returns></returns>
            public static Dictionary<string, Object> GetPrefabs(Type type)
            {
                if (dictionaryCache.TryGetValue(type, out var map))
                {
                    return map;
                }
                else
                {
                    InitCache(type);
                    return GetPrefabs(type);
                }
            }

            private static void InitCache(Type type, Dictionary<string, Object> map = null)
            {
                map ??= new Dictionary<string, Object>();
                foreach (var unityObject in Resources.FindObjectsOfTypeAll(type))
                {
                    map[unityObject.name] = unityObject;
                }

                dictionaryCache[type] = map;
            }

            internal static void ClearCache()
            {
                dictionaryCache.Clear();
            }
        }
    }
}