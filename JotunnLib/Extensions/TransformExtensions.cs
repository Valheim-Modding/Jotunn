using UnityEngine;

namespace Jotunn.Extensions
{
    /// <summary>
    ///     Convenience methods for Transforms
    /// </summary>
    public static class TransformExtensions
    {
        /// <summary>
        ///     Extension method to find nested children by name using either
        ///     a breadth-first or depth-first search. Default is breadth-first.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="childName">Name of the child object to search for.</param>
        /// <param name="searchType">Whether to preform a breadth first or depth first search. Default is breadth first.</param>
        /// <returns></returns>
        public static Transform FindDeepChild(
            this Transform transform,
            string childName,
            global::Utils.IterativeSearchType searchType = global::Utils.IterativeSearchType.BreadthFirst
        )
        {
            return global::Utils.FindChild(transform, childName, searchType);
        }
    }
}
