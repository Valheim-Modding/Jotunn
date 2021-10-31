using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Jotunn.Managers
{
    /// <summary>
    ///     Handles all logic to do with managing mocked prefabs added into the game.
    /// </summary>
    internal class MockManager : IManager
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
        
        /// <summary>
        ///     Legacy ValheimLib prefix used by the Mock System to recognize Mock gameObject that must be replaced at some point.
        /// </summary>
        [Obsolete("Legacy ValheimLib mock prefix. Use JVLMockPrefix \"JVLmock_\" instead.")]
        public const string MockPrefix = "VLmock_";

        /// <summary>
        ///     Prefix used by the Mock System to recognize Mock gameObject that must be replaced at some point.
        /// </summary>
        public const string JVLMockPrefix = "JVLmock_";

        /// <summary>
        ///     Internal container for mocked prefabs
        /// </summary>
        internal GameObject MockPrefabContainer;

        /// <summary>
        ///     Creates the container and registers all hooks
        /// </summary>
        public void Init()
        {
            MockPrefabContainer = new GameObject("MockPrefabs");
            MockPrefabContainer.transform.parent = Main.RootObject.transform;
            MockPrefabContainer.SetActive(false);

            On.ObjectDB.Awake += RemoveMockPrefabs;
        }

        public T CreateMockedPrefab<T>(string prefabName) where T : Component
        {
            string name = JVLMockPrefix + prefabName;

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
                Logger.LogInfo("Destroying Mock prefabs");

                foreach (var transform in MockPrefabContainer.transform)
                {
                    Object.Destroy(((Transform)transform).gameObject);
                }
            }
        }

        /// <summary>
        ///     Will try to find the real vanilla prefab from the given mock
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="unityObject"></param>
        /// <returns>the real prefab</returns>
        public static T GetRealPrefabFromMock<T>(Object unityObject) where T : Object
        {
            return (T)GetRealPrefabFromMock(unityObject, typeof(T));
        }

        private static readonly Regex copyRegex = new Regex(@" \([0-9]+\)");

#pragma warning disable CS0618
        /// <summary>
        ///     Will try to find the real vanilla prefab from the given mock
        /// </summary>
        /// <param name="unityObject"></param>
        /// <param name="mockObjectType"></param>
        /// <returns>the real prefab</returns>
        public static Object GetRealPrefabFromMock(Object unityObject, Type mockObjectType)
        {
            if (unityObject)
            {
                var unityObjectName = unityObject.name;
                var isVLMock = unityObjectName.StartsWith(MockPrefix);
                var isJVLMock = unityObjectName.StartsWith(JVLMockPrefix);
                if (isVLMock || isJVLMock)
                {
                    if (isVLMock) unityObjectName = unityObjectName.Substring(MockPrefix.Length);
                    if (isJVLMock) unityObjectName = unityObjectName.Substring(JVLMockPrefix.Length);

                    // Cut off the suffix in the name to correctly query the original material
                    if (unityObject is Material)
                    {
                        const string materialInstance = " (Instance)";
                        if (unityObjectName.EndsWith(materialInstance))
                        {
                            unityObjectName = unityObjectName.Substring(0, unityObjectName.Length - materialInstance.Length);
                        }
                    }
                    //Allow duplicated JVLmocks (child names must be unique)
                    Match match = copyRegex.Match(unityObjectName);
                    if (match.Success)
                    {
                        unityObjectName = unityObjectName.Substring(0, match.Index);
                    }

                    Object ret = PrefabManager.Cache.GetPrefab(mockObjectType, unityObjectName);

                    if (!ret)
                    {
                        throw new Exception($"Mock prefab {unityObjectName} could not be resolved");
                    }

                    return ret;
                }
            }

            return null;
        }
#pragma warning restore CS0618
    }
}
