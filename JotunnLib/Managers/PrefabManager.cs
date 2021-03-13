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
            
            // Clear existing
            Prefabs.Clear();

            foreach (Transform child in PrefabContainer.transform)
            {
                GameObject.Destroy(child.gameObject);
            }

            // Call event handlers to load prefabs
            PrefabRegister?.Invoke(null, EventArgs.Empty);

            var namedPrefabs = ReflectionUtils.GetPrivateField<Dictionary<int, GameObject>>(ZNetScene.instance, "m_namedPrefabs");

            // Load prefabs into game
            foreach (var pair in Prefabs)
            {
                GameObject prefab = pair.Value;

                ZNetScene.instance.m_prefabs.Add(prefab);
                namedPrefabs.Add(prefab.name.GetStableHashCode(), prefab);

                Debug.Log("Registered prefab: " + pair.Key);
            }

            // Send event that all prefabs are loaded
            PrefabsLoaded?.Invoke(null, EventArgs.Empty);
            Debug.Log("All prefabs loaded");
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

            prefabConfig.Register();
        }

        public GameObject CreatePrefab(string name)
        {
            GameObject prefab = new GameObject(name);
            prefab.transform.parent = PrefabContainer.transform;

            // TODO: Add any components required for it to work as a prefab (ZNetView, etc?)
            Prefabs.Add(name, prefab);

            return prefab;
        }

        public GameObject CreatePrefab(string name, string baseName)
        {
            if (GetPrefab(name))
            {
                Debug.LogError("Prefab already exists: " + name);
                return null;
            }

            GameObject prefabBase = GetPrefab(baseName);

            if (!prefabBase)
            {
                Debug.LogError("Prefab base does not exist: " + baseName);
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
            if (Prefabs.ContainsKey(name))
            {
                return Prefabs[name];
            }

            if (!ZNetScene.instance)
            {
                Debug.LogError("\t-> ZNetScene instance null for some reason");
                return null;
            }

            foreach (GameObject obj in ZNetScene.instance.m_prefabs)
            {
                if (obj.name == name)
                {
                    return obj;
                }
            }

            return null;
        }
    }
}
