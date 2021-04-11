using JotunnLib.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JotunnLib.Managers
{
    /// <summary>
    ///     Handles all logic to do with managing mocked prefabs added into the game.
    /// </summary>
    class MockManager : Manager
    {
        /// <summary>
        ///     The singleton instance of this manager.
        /// </summary>
        public static MockManager Instance { get; private set; }

        internal GameObject MockPrefabContainer;

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
            MockPrefabContainer = new GameObject("MockPrefabs");
            MockPrefabContainer.transform.parent = Main.RootObject.transform;
            MockPrefabContainer.SetActive(false);

            On.ObjectDB.Awake += removeMockPrefabs;
        }

        internal T CreateMockedPrefab<T>(string prefabName) where T : Component
        {
            //string name = prefabName + "_" + nameof(Mock<T>);
            string name = PrefabExtensions.JVLMockPrefix + prefabName;
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
                mock.name = PrefabExtensions.JVLMockPrefix + prefabName;
                
                return mock;
            }

        }

        private void removeMockPrefabs(On.ObjectDB.orig_Awake orig, ObjectDB self)
        {
            orig(self);
            var isValid = self.IsValid();

            if (isValid && MockPrefabContainer.transform.childCount > 0)
            {
                Logger.LogInfo("Destroying Mock prefabs");

                foreach (var transform in MockPrefabContainer.transform)
                {
                    Destroy(((Transform)transform).gameObject);
                }
            }
        }
    }
}
