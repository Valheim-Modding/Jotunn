using JotunnLib.Managers;
using UnityEngine;

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
            g.transform.SetParent(PrefabManager.PrefabContainer.transform);
            g.SetActive(false);

            var mock = g.AddComponent<T>();
            mock.name = PrefabExtensions.MockPrefix + name;

            return mock;
        }
    }
}
