using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace JotunnLib.Entities
{
    /// <summary>
    /// Helper class for creating Mock for a given vanilla Component
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class Mock<T> where T : Component
    {
        public static T Create(string name)
        {
            var g = new GameObject(name + "_" + nameof(Mock<T>));
            UnityObject.DontDestroyOnLoad(g);
            g.transform.SetParent(Prefab.Parent.transform);
            g.SetActive(false);

            var mock = g.AddComponent<T>();
            mock.name = Prefab.MockPrefix + name;

            return mock;
        }
    }
}
