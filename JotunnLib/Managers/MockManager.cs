using Jotunn.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Jotunn.Managers
{
    /// <summary>
    ///     Handles all logic to do with managing mocked prefabs added into the game.
    /// </summary>
    class MockManager : IManager
    {
        private static MockManager _instance;
        /// <summary>
        ///     The singleton instance of this manager.
        /// </summary>
        public static MockManager Instance
        {
            get
            {
                if (_instance == null) _instance = new MockManager();
                return _instance;
            }
        }

        internal GameObject MockPrefabContainer;


        public void Init()
        {
            MockPrefabContainer = new GameObject("MockPrefabs");
            MockPrefabContainer.transform.parent = Main.RootObject.transform;
            MockPrefabContainer.SetActive(false);

            On.ObjectDB.Awake += RemoveMockPrefabs;
        }

        internal T CreateMockedPrefab<T>(string prefabName) where T : Component
        {
            //string name = prefabName + "_" + nameof(Mock<T>);
            string name = PrefabExtension.JVLMockPrefix + prefabName;
            Transform transform = MockPrefabContainer.transform.Find(name);

            if (transform)
            {
                Logger.LogDebug($"Mock {name} already registered");
                
                return transform.gameObject.GetComponent<T>();
            }
            else
            {
                Logger.LogDebug($"Mock {name} created");
                
                var g = new GameObject(name);
                g.transform.SetParent(MockPrefabContainer.transform);
                g.SetActive(false);

                var mock = g.AddComponent<T>();
                mock.name = PrefabExtension.JVLMockPrefix + prefabName;
                
                return mock;
            }

        }

        private void RemoveMockPrefabs(On.ObjectDB.orig_Awake orig, ObjectDB self)
        {
            orig(self);

            if (SceneManager.GetActiveScene().name == "main" && self.IsValid())
            {
                if (MockPrefabContainer.transform.childCount > 0)
                {
                    Logger.LogInfo("Destroying Mock prefabs");

                    foreach (var transform in MockPrefabContainer.transform)
                    {
                        GameObject.Destroy(((Transform)transform).gameObject);
                    }
                }
            }
        }
    }
}
