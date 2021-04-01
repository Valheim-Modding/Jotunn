using System;
using System.Collections.Generic;
using UnityEngine;
using JotunnLib.Entities;
using JotunnLib.Utils;
using UnityObject = UnityEngine.Object;

namespace JotunnLib.Managers
{
    /// <summary>
    /// Handles all logic to do with managing custom prefabs added into the game.
    /// </summary>
    public class PrefabManager : Manager
    {
        public static PrefabManager Instance { get; private set; }
        public static GameObject PrefabContainer;

        public event EventHandler PrefabRegister, PrefabsLoaded;
        internal Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();
        private bool loaded = false;

        internal List<WeakReference> NetworkedModdedPrefabs = new List<WeakReference>();

        private void Awake()
        {
            if (Instance != null)
            {
                Logger.LogError("Error, two instances of singleton: " + this.GetType().Name);
                return;
            }

            Instance = this;
        }

        internal override void Init()
        {
            On.ZNetScene.Awake += AddCustomPrefabsToZNetSceneDictionary;
            PrefabContainer = new GameObject("Prefabs");
            PrefabContainer.transform.parent = JotunnLibMain.RootObject.transform;
            PrefabContainer.SetActive(false);
        }

        internal override void Register()
        {
            // TODO: Split register and load logic
        }

        internal override void Load()
        {
            Logger.LogInfo("---- Registering custom prefabs ----");

            // Call event handlers to load prefabs
            if (!loaded)
            {
                PrefabRegister?.Invoke(null, EventArgs.Empty);
            }

            // Load prefabs into game
            var namedPrefabs = ZNetScene.instance.m_namedPrefabs;

            foreach (var pair in Prefabs)
            {
                GameObject prefab = pair.Value;

                ZNetScene.instance.m_prefabs.Add(prefab);
                namedPrefabs.Add(prefab.name.GetStableHashCode(), prefab);

                Logger.LogInfo("Registered prefab: " + pair.Key);
            }

            // Send event that all prefabs are loaded
            if (!loaded)
            {
                PrefabsLoaded?.Invoke(null, EventArgs.Empty);
            }

            Logger.LogInfo("All prefabs loaded");
            loaded = true;
        }

        /// <summary>
        /// Register an existing GameObject as a prefab
        /// </summary>
        /// <param name="prefab">The GameObject to register as a prefab</param>
        /// <param name="name">The name for the prefab. If not provided, will use name of GameObject</param>
        public void RegisterPrefab(GameObject prefab, string name = null)
        {
            if (name == null)
            {
                name = prefab.name;
            }

            if (GetPrefab(name))
            {
                Logger.LogError("Prefab already exists: " + name);
                return;
            }

            prefab.name = name;
            prefab.transform.parent = PrefabContainer.transform;
            prefab.SetActive(true);
            Prefabs.Add(name, prefab);
        }

        /// <summary>
        /// Registers a new prefab from given PrefabConfig instance
        /// </summary>
        /// <param name="prefabConfig">Prefab configuration instance</param>
        public void RegisterPrefab(PrefabConfig prefabConfig)
        {
            // If no error occured, register prefab
            if (prefabConfig.Prefab != null)
            {
                prefabConfig.Register();
            }
        }

        /// <summary>
        /// Registers a new prefab that's an empty primitive, with just a ZNetView component
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public GameObject CreatePrefab(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                Logger.LogError("Failed to create prefab with invalid name: " + name);
                return null;
            }

            if (GetPrefab(name))
            {
                Logger.LogError("Failed to create prefab, name already exists: " + name);
                return null;
            }

            GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prefab.name = name;
            prefab.transform.parent = PrefabContainer.transform;

            // Add ZNetView and make prefabs persistent by default
            ZNetView newView = prefab.AddComponent<ZNetView>();
            newView.m_persistent = true;

            Prefabs.Add(name, prefab);
            return prefab;
        }

        /// <summary>
        /// Registers a new prefab that's a copy of given base.
        /// </summary>
        /// <param name="name">New prefab name</param>
        /// <param name="baseName">Base prefab name</param>
        /// <returns>New prefab object</returns>
        public GameObject CreatePrefab(string name, string baseName)
        {
            if (string.IsNullOrEmpty(name))
            {
                Logger.LogError("Failed to create prefab with invalid name: " + name);
                return null;
            }

            if (GetPrefab(name))
            {
                Logger.LogError("Failed to create prefab, name already exists: " + name);
                return null;
            }

            GameObject prefabBase = GetPrefab(baseName);

            if (!prefabBase)
            {
                Logger.LogError("Failed to create prefab, base does not exist: " + baseName);
                return null;
            }

            GameObject prefab = UnityEngine.Object.Instantiate(prefabBase, PrefabContainer.transform);
            prefab.name = name;
            prefab.SetActive(true);
            Prefabs.Add(name, prefab);

            return prefab;
        }

        /// <summary>
        /// Returns an existing prefab with given name, or null if none exist.
        /// </summary>
        /// <param name="name">Name of the prefab to search for</param>
        /// <returns></returns>
        public GameObject GetPrefab(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            if (Prefabs.ContainsKey(name))
            {
                return Prefabs[name];
            }

            if (!ZNetScene.instance)
            {
                Logger.LogError("\t-> ZNetScene instance null for some reason");
                return null;
            }

            var namedPrefabs = ZNetScene.instance.m_namedPrefabs;
            int key = name.GetStableHashCode();

            if (namedPrefabs.ContainsKey(key))
            {
                return namedPrefabs[key];
            }

            return null;
        }



        private void AddCustomPrefabsToZNetSceneDictionary(On.ZNetScene.orig_Awake orig, ZNetScene self)
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
        public void NetworkRegister(GameObject prefab)
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
        public GameObject InstantiateClone(GameObject gameObject, string nameToSet, bool zNetRegister = true)
        {
            const char separator = '_';

            var methodBase = new System.Diagnostics.StackTrace().GetFrame(1).GetMethod();
            var id = methodBase.DeclaringType.Assembly.GetName().Name + separator + methodBase.DeclaringType.Name + separator + methodBase.Name;

            var prefab = UnityEngine.Object.Instantiate(gameObject, PrefabContainer.transform);
            prefab.name = nameToSet + separator + id;

            if (zNetRegister)
            {
                NetworkRegister(prefab);
            }

            return prefab;
        }

    }
}