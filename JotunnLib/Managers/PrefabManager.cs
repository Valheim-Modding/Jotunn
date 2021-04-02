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

        private void Awake()
        {
            if (Instance != null)
            {
                Logger.LogError($"Two instances of singleton {GetType()}");
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

            Logger.LogInfo("Initialized PrefabManager");
        }

        private string CreateUID()
        {
            const char separator = '_';

            var methodBase = new System.Diagnostics.StackFrame(2).GetMethod();
            var id = methodBase.DeclaringType.Assembly.GetName().Name + separator + methodBase.DeclaringType.Name + separator + methodBase.Name;

            return separator + id;
        }

        /// <summary>
        /// Adds a prefab to the manager. Added prefabs get registered to the <see cref="ZNetScene"/> on Awake().
        /// </summary>
        /// <param name="name"></param>
        /// <param name="prefab"></param>
        public void AddPrefab(string name, GameObject prefab)
        {
            if (Prefabs.ContainsKey(name))
            {
                Logger.LogWarning($"Prefab {name} already exists");
                return;
            }

            prefab.name = name + CreateUID();
            prefab.transform.parent = PrefabContainer.transform;
            //prefab.SetActive(true);
            Prefabs.Add(name, prefab);
        }

        /// <summary>
        /// Creates and adds a new prefab that's an empty primitive.
        /// </summary>
        /// <param name="name">The name of the new GameObject</param>
        /// <param name="addZNetView">When true a ZNetView component is added to the new GameObject for ZDO generation and networking. Default: true</param>
        /// <returns></returns>
        public GameObject AddEmptyPrefab(string name, bool addZNetView = true)
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
            prefab.name = name + CreateUID();
            prefab.transform.parent = PrefabContainer.transform;

            if (addZNetView)
            {
                // Add ZNetView and make prefabs persistent by default
                ZNetView newView = prefab.AddComponent<ZNetView>();
                newView.m_persistent = true;
            }

            Prefabs.Add(name, prefab);
            return prefab;
        }

        /// <summary>
        /// Registers a new prefab that's a copy of given base.
        /// </summary>
        /// <param name="name">New prefab name</param>
        /// <param name="baseName">Base prefab name</param>
        /// <returns>New prefab object</returns>
        public GameObject AddClonedPrefab(string name, string baseName)
        {
            return AddClonedPrefab(name, GetPrefab(baseName));
        }

        /// <summary>
        /// Allows you to clone a given prefab without modifying the original.
        /// </summary>
        /// <param name="name">name for the new clone</param>
        /// <param name="gameObject">prefab that you want to clone</param>
        /// <returns></returns>
        public GameObject AddClonedPrefab(string name, GameObject gameObject)
        {
            var prefab = UnityEngine.Object.Instantiate(gameObject, PrefabContainer.transform);
            prefab.name = name + CreateUID();

            Prefabs.Add(base.name, prefab);
            return prefab;
        }

        /// <summary>
        /// Returns an existing prefab with given name, or null if none exist.
        /// </summary>
        /// <param name="name">Name of the prefab to search for</param>
        /// <returns></returns>
        internal GameObject GetPrefab(string name)
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

        internal void DestroyPrefab(string name)
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

                if (del != null)
                {
                    ZNetScene.instance.m_prefabs.Remove(del);
                    DestroyImmediate(del);
                }
            }
        }

        /// <summary>
        ///     Add all registered prefabs to the namedPrefabs in <see cref="ZNetScene" />.
        /// </summary>
        /// <param name="instance"></param>
        public static void RegisterAllToZNetScene(On.ZNetScene.orig_Awake orig, ZNetScene self)
        {
            orig(self);

            if (self && Instance.Prefabs.Count > 0)
            {
                Logger.LogMessage("Adding custom prefabs to ZNetScene");

                foreach (var prefab in Instance.Prefabs)
                {
                    var name = prefab.Key;

                    Logger.LogInfo($"GameObject: {name}");

                    RegisterToZNetScene(name, prefab.Value);
                }
            }
        }

        public static void RegisterToZNetScene(string name, GameObject gameObject)
        {
            var znet = ZNetScene.instance;

            if (znet)
            {
                if (!znet.m_namedPrefabs.ContainsKey(name.GetStableHashCode()))
                {
                    znet.m_prefabs.Add(gameObject);
                    znet.m_namedPrefabs.Add(name.GetStableHashCode(), gameObject);
                    Logger.LogInfo($"Added prefab {name}");
                }
            }
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

    }
}