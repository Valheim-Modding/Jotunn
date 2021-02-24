using System;
using System.Collections.Generic;
using UnityEngine;

namespace ValheimLokiLoader.Managers
{
    public static class PrefabManager
    {
        public static event EventHandler PrefabLoad;
        public static Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();
        private static GameObject prefabContainer;
        private static bool prefabsLoaded = false;

        internal static void Init()
        {
            prefabContainer = new GameObject("_LokiPrefabs");
            prefabContainer.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(prefabContainer);
            Debug.Log("Initialized PrefabManager");
        }

        internal static void LoadPrefabs()
        {
            if (!prefabsLoaded)
            {
                PrefabLoad(null, null);
                prefabsLoaded = true;
            }
        }

        public static void AddPrefab(GameObject prefab, string name)
        {
            prefab.name = name;
            prefab.transform.parent = prefabContainer.transform;
            prefab.SetActive(true);
            Prefabs.Add(name, prefab);
        }

        public static GameObject GetPrefab(string name)
        {
            if (Prefabs.ContainsKey(name))
            {
                return Prefabs[name];
            }

            return ZNetScene.instance.GetPrefab(name);
        }
    }
}
