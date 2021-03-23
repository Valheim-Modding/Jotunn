using System;
using System.Collections.Generic;
using UnityEngine;
using JotunnLib.Entities;
using JotunnLib.Utils;

namespace JotunnLib.Managers
{
    /// <summary>
    /// Handles all logic to do with managing custom prefabs added into the game.
    /// </summary>
    public class PrefabManager : Manager
    {
        public static PrefabManager Instance { get; private set; }
        internal static GameObject PrefabContainer;

        public event EventHandler PrefabRegister, PrefabsLoaded;
        internal Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();
        private bool loaded = false;

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("Error, two instances of singleton: " + this.GetType().Name);
                return;
            }

            Instance = this;
        }

        internal override void Init()
        {
            PrefabContainer = new GameObject("Prefabs");
            PrefabContainer.transform.parent = JotunnLib.RootObject.transform;
            PrefabContainer.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(PrefabContainer);

            Debug.Log("Initialized PrefabManager");
        }

        internal override void Register()
        {
            // TODO: Split register and load logic
        }

        internal override void Load()
        {
            Debug.Log("---- Registering custom prefabs ----");

            // Call event handlers to load prefabs
            if (!loaded)
            {
                PrefabRegister?.Invoke(null, EventArgs.Empty);
            }

            // Load prefabs into game
            var namedPrefabs = ReflectionUtils.GetPrivateField<Dictionary<int, GameObject>>(ZNetScene.instance, "m_namedPrefabs");

            foreach (var pair in Prefabs)
            {
                GameObject prefab = pair.Value;

                ZNetScene.instance.m_prefabs.Add(prefab);
                namedPrefabs.Add(prefab.name.GetStableHashCode(), prefab);

                Debug.Log("Registered prefab: " + pair.Key);
            }

            // Send event that all prefabs are loaded
            if (!loaded)
            {
                PrefabsLoaded?.Invoke(null, EventArgs.Empty);
            }

            Debug.Log("All prefabs loaded");
            loaded = true;
        }

        internal void RegisterPrefab(GameObject prefab, string name)
        {
            if (GetPrefab(name))
            {
                Debug.LogError("Prefab already exists: " + name);
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
        /// <param name="prefabConfig">Prefab configuration</param>
        public void RegisterPrefab(PrefabConfig prefabConfig)
        {
            if (prefabConfig.BasePrefabName == null || prefabConfig.BasePrefabName == "")
            {
                prefabConfig.Prefab = CreatePrefab(prefabConfig.Name);
            }
            else
            {
                prefabConfig.Prefab = CreatePrefab(prefabConfig.Name, prefabConfig.BasePrefabName);
            }

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
                Debug.LogError("Failed to create prefab with invalid name: " + name);
                return null;
            }

            if (GetPrefab(name))
            {
                Debug.LogError("Failed to create prefab, name already exists: " + name);
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
                Debug.LogError("Failed to create prefab with invalid name: " + name);
                return null;
            }

            if (GetPrefab(name))
            {
                Debug.LogError("Failed to create prefab, name already exists: " + name);
                return null;
            }

            GameObject prefabBase = GetPrefab(baseName);

            if (!prefabBase)
            {
                Debug.LogError("Failed to create prefab, base does not exist: " + baseName);
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
                Debug.LogError("\t-> ZNetScene instance null for some reason");
                return null;
            }

            var namedPrefabs = ReflectionUtils.GetPrivateField<Dictionary<int, GameObject>>(ZNetScene.instance, "m_namedPrefabs");
            int key = name.GetStableHashCode();

            if (namedPrefabs.ContainsKey(key))
            {
                return namedPrefabs[key];
            }

            return null;
        }
    }
}
