using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using Jotunn.Entities;
using MonoMod.RuntimeDetour;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Jotunn.Managers
{
    /// <summary>
    ///     Manager for handling custom prefabs added to the game.
    /// </summary>
    public class PrefabManager : IManager
    {
        private static PrefabManager _instance;
        /// <summary>
        ///     The singleton instance of this manager.
        /// </summary>
        public static PrefabManager Instance
        {
            get
            {
                if (_instance == null) _instance = new PrefabManager();
                return _instance;
            }
        }
        
        /// <summary>
        ///     Event that gets fired after the vanilla prefabs are in memory and available for cloning.
        ///     Your code will execute every time before a new <see cref="ObjectDB"/> is copied (on every menu start).
        ///     If you want to execute just once you will need to unregister from the event after execution.
        /// </summary>
        public static event Action OnVanillaPrefabsAvailable;

        /// <summary>
        ///     Event that gets fired after registering all custom prefabs to <see cref="ZNetScene"/>.
        ///     Your code will execute every time a new ZNetScene is created (on every game start). 
        ///     If you want to execute just once you will need to unregister from the event after execution.
        /// </summary>
        public static event Action OnPrefabsRegistered;

        /// <summary>
        ///     Container for custom prefabs in the DontDestroyOnLoad scene.
        /// </summary>
        internal GameObject PrefabContainer;

        /// <summary>
        ///     Dictionary of all added custom prefabs by name hash.
        /// </summary>
        internal Dictionary<int, CustomPrefab> Prefabs = new Dictionary<int, CustomPrefab>();

        /// <summary>
        ///     Creates the prefab container and registers all hooks.
        /// </summary>
        public void Init()
        {
            PrefabContainer = new GameObject("Prefabs");
            PrefabContainer.transform.parent = Main.RootObject.transform;
            PrefabContainer.SetActive(false);
            
            On.ZNetScene.Awake += RegisterAllToZNetScene;
            SceneManager.sceneUnloaded += (current) => Cache.ClearCache();

            // Fire events as a late action in the detour so all mods can load before
            // Leave space for mods to forcefully run after us. 1000 is an arbitrary "good amount" of space.
            using (new DetourContext(int.MaxValue - 1000))
            {
                On.ObjectDB.CopyOtherDB += InvokeOnVanillaObjectsAvailable; ;
                On.ZNetScene.Awake += InvokeOnPrefabsRegistered;
            }
        }

        /// <summary>
        ///     Add a custom prefab to the manager with known source mod metadata.
        /// </summary>
        /// <param name="prefab">Prefab to add</param>
        /// <param name="sourceMod">Metadata of the mod adding this prefab</param>
        internal void AddPrefab(GameObject prefab, BepInPlugin sourceMod)
        {
            CustomPrefab customPrefab = new CustomPrefab(prefab, sourceMod);
            AddPrefab(customPrefab);
        }

        /// <summary>
        ///     Add a custom prefab to the manager.<br />
        ///     Checks if a prefab with the same name is already added.<br />
        ///     Added prefabs get registered to the <see cref="ZNetScene"/> on <see cref="ZNetScene.Awake"/>.
        /// </summary>
        /// <param name="prefab">Prefab to add</param>
        public void AddPrefab(GameObject prefab)
        {
            CustomPrefab customPrefab = new CustomPrefab(prefab, false);
            AddPrefab(customPrefab);
        }

        /// <summary>
        ///     Add a custom prefab to the manager.<br />
        ///     Checks if a prefab with the same name is already added.<br />
        ///     Added prefabs get registered to the <see cref="ZNetScene"/> on <see cref="ZNetScene.Awake"/>.
        /// </summary>
        /// <param name="customPrefab">Prefab to add</param>
        public void AddPrefab(CustomPrefab customPrefab)
        {
            if (!customPrefab.IsValid())
            {
                Logger.LogWarning($"Custom prefab {customPrefab} is not valid");
                return;
            }

            int hash = customPrefab.Prefab.name.GetStableHashCode();
            if (Prefabs.ContainsKey(hash))
            {
                Logger.LogWarning($"Prefab '{customPrefab}' already exists");
                return;
            }

            customPrefab.Prefab.transform.SetParent(PrefabContainer.transform, false);

            Prefabs.Add(hash, customPrefab);
        }

        /// <summary>
        ///     Create a new prefab from an empty primitive.
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
                Logger.LogWarning($"Failed to create prefab with invalid name: {name}");
                return null;
            }
            if (GetPrefab(name))
            {
                Logger.LogWarning($"Failed to create prefab, name already exists: {name}");
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
        /// <returns>Newly created prefab object</returns>
        public GameObject CreateClonedPrefab(string name, string baseName)
        {
            if (string.IsNullOrEmpty(baseName))
            {
                Logger.LogWarning($"Failed to clone prefab with invalid baseName: {baseName}");
                return null;
            }

            // Try to get the prefab
            GameObject prefab = GetPrefab(baseName);
            if (!prefab)
            {
                Logger.LogWarning($"Failed to clone prefab, can not find base prefab with name: {baseName}");
                return null;
            }

            return CreateClonedPrefab(name, prefab);
        }

        /// <summary>
        ///     Create a copy of a given prefab without modifying the original.
        /// </summary>
        /// <param name="name">Name of the new prefab.</param>
        /// <param name="prefab">Prefab instance to copy.</param>
        /// <returns>Newly created prefab object</returns>
        public GameObject CreateClonedPrefab(string name, GameObject prefab)
        {
            if (string.IsNullOrEmpty(name))
            {
                Logger.LogWarning($"Failed to clone prefab with invalid name: {name}");
                return null;
            }
            if (!prefab)
            {
                Logger.LogWarning($"Failed to clone prefab, base prefab is not valid");
                return null;
            }
            if (GetPrefab(name))
            {
                Logger.LogWarning($"Failed to clone prefab, name already exists: {name}");
                return null;
            }

            var newPrefab = Object.Instantiate(prefab, PrefabContainer.transform, false);
            newPrefab.name = name;

            return newPrefab;
        }

        /// <summary>
        ///     Get a prefab by its name.<br /><br />
        ///     Search hierarchy:
        ///     <list type="number">
        ///         <item>Custom prefab with the exact name</item>
        ///         <item>Vanilla prefab with the exact name from <see cref="ZNetScene"/> if already instantiated</item>
        ///         <item>Vanilla prefab from the prefab cache</item>
        ///     </list>
        /// </summary>
        /// <param name="name">Name of the prefab to search for.</param>
        /// <returns>The existing prefab, or null if none exists with given name</returns>
        public GameObject GetPrefab(string name)
        {
            int hash = name.GetStableHashCode();

            if (Prefabs.TryGetValue(hash, out var custom))
            {
                return custom.Prefab;
            }

            if (ZNetScene.instance &&
                ZNetScene.instance.m_namedPrefabs.TryGetValue(hash, out var prefab))
            {
                return prefab;
            }

            return Cache.GetPrefab<GameObject>(name);
        }

        /// <summary>
        ///     Remove a custom prefab from the manager.
        /// </summary>
        /// <param name="name">Name of the prefab to remove</param>
        public void RemovePrefab(string name)
        {
            int hash = name.GetStableHashCode();
            Prefabs.Remove(hash);
        }

        /// <summary>
        ///     Destroy a custom prefab.<br />
        ///     Removes it from the manager and if already instantiated also from the <see cref="ZNetScene"/>.
        /// </summary>
        /// <param name="name">The name of the prefab to destroy</param>
        public void DestroyPrefab(string name)
        {
            int hash = name.GetStableHashCode();

            if (!Prefabs.TryGetValue(hash, out var custom))
            {
                return;
            }

            if (ZNetScene.instance)
            {
                //TODO: remove all clones, too

                if (ZNetScene.instance.m_namedPrefabs.TryGetValue(hash, out var del))
                {
                    ZNetScene.instance.m_prefabs.Remove(del);
                    ZNetScene.instance.m_nonNetViewPrefabs.Remove(del);
                    ZNetScene.instance.m_namedPrefabs.Remove(hash);
                    ZNetScene.instance.Destroy(del);
                }
            }

            if (custom.Prefab)
            {
                Object.Destroy(custom.Prefab);
            }

            Prefabs.Remove(hash);
        }

        /// <summary>
        ///     Register all custom prefabs to m_prefabs/m_namedPrefabs in <see cref="ZNetScene" />.
        /// </summary>
        private void RegisterAllToZNetScene(On.ZNetScene.orig_Awake orig, ZNetScene self)
        {
            orig(self);

            if (Prefabs.Any())
            {
                Logger.LogInfo($"Adding {Prefabs.Count} custom prefabs to the ZNetScene");

                List<CustomPrefab> toDelete = new List<CustomPrefab>();

                foreach (var customPrefab in Prefabs.Values)
                {
                    try
                    {
                        if (customPrefab.FixReference)
                        {
                            customPrefab.Prefab.FixReferences();
                            customPrefab.FixReference = false;
                        }

                        RegisterToZNetScene(customPrefab.Prefab);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Error caught while adding prefab {customPrefab}: {ex}");
                        toDelete.Add(customPrefab);
                    }
                }

                // Delete custom prefabs with errors
                foreach (var prefab in toDelete)
                {
                    if (prefab.Prefab)
                    {
                        DestroyPrefab(prefab.Prefab.name);
                    }
                }
            }
        }

        /// <summary>
        ///     Register a single prefab to the current <see cref="ZNetScene"/>.<br />
        ///     Checks for existence of the object via GetStableHashCode() and adds the prefab if it is not already added.
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
                    Logger.LogDebug($"Prefab {name} already in ZNetScene");
                }
                else
                {
                    if (gameObject.GetComponent<ZNetView>() != null)
                    {
                        znet.m_prefabs.Add(gameObject);
                    }
                    else
                    {
                        znet.m_nonNetViewPrefabs.Add(gameObject);
                    }
                    znet.m_namedPrefabs.Add(hash, gameObject);
                    Logger.LogDebug($"Added prefab {name}");
                }
            }
        }
        
        /// <summary>
        ///     Safely invoke the <see cref="OnVanillaPrefabsAvailable"/> event
        /// </summary>
        /// 
        private void InvokeOnVanillaObjectsAvailable(On.ObjectDB.orig_CopyOtherDB orig, ObjectDB self, ObjectDB other)
        {
            OnVanillaPrefabsAvailable?.SafeInvoke();

            orig(self, other);
        }
        
        private void InvokeOnPrefabsRegistered(On.ZNetScene.orig_Awake orig, ZNetScene self)
        {
            orig(self);

            OnPrefabsRegistered?.SafeInvoke();
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
