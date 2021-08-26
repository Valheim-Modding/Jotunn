using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

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
            string name = PrefabExtension.JVLMockPrefix + prefabName;

            Transform transform = MockPrefabContainer.transform.Find(name);
            if (transform != null)
            {
                return transform.gameObject.GetComponent<T>();
            }

            GameObject g = new GameObject(name);
            g.transform.parent = MockPrefabContainer.transform;
            g.SetActive(false);

            T mock = g.AddComponent<T>();
            if (mock == null)
            {
                Logger.LogWarning($"Could not create mock for prefab {prefabName} of type {typeof(T)}");
                return null;
            }
            mock.name = name;

            Logger.LogDebug($"Mock {name} created");

            return mock;
        }

        private void RemoveMockPrefabs(On.ObjectDB.orig_Awake orig, ObjectDB self)
        {
            orig(self);

            if (SceneManager.GetActiveScene().name == "main")
            {
                if (MockPrefabContainer.transform.childCount > 0)
                {
                    Logger.LogInfo("Destroying Mock prefabs");

                    foreach (var transform in MockPrefabContainer.transform)
                    {
                        Object.Destroy(((Transform)transform).gameObject);
                    }
                }
            }
        }
    }
}
