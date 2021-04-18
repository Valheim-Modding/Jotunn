using JotunnLib.Managers;
using UnityEngine;

namespace JotunnLib.Entities
{
    /// <summary>
    ///     Helper class for creating Mock for a given vanilla Component
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class Mock<T> where T : Component
    {
        public static T Create(string name)
        {
            return MockManager.Instance.CreateMockedPrefab<T>(name);
        }
    }
}
