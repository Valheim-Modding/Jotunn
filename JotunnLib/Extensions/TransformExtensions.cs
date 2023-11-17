using System.Collections.Generic;
using UnityEngine;

namespace Jotunn.Extensions
{
    internal static class TransformExtensions
    {
        /// <summary>
        ///     Extension method to find nested children by name using either
        ///     a breadth-first or depth-first search. Default is breadth-first.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="childName"></param>
        /// <returns></returns>
        public static Transform FindDeepChild(
            this Transform transform,
            string childName,
            bool breadthFirst = true
        )
        {
            if (breadthFirst)
            {
                var queue = new Queue<Transform>();
                queue.Enqueue(transform);
                while (queue.Count > 0)
                {
                    var child = queue.Dequeue();
                    if (child.name == childName)
                    {
                        return child;
                    }

                    foreach (Transform t in child)
                    {
                        queue.Enqueue(t);
                    }
                }
                return null;
            }
            else
            {
                foreach (Transform child in transform)
                {
                    if (child.name == childName)
                    {
                        return child;
                    }
                    var result = child.FindDeepChild(childName);
                    if (result != null)
                    {
                        return result;
                    }
                }
                return null;
            }
        }
    }
}
