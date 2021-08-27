using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Jotunn.Utils
{
    /// <summary>
    ///     Various static utility methods for working with Shaders
    /// </summary>
    public static class ShaderHelper
    {
        /// <summary>
        ///     Get a list of all <see cref="MeshRenderer"/> and <see cref="SkinnedMeshRenderer"/> in this GameObject and its childs.
        /// </summary>
        /// <param name="gameObject">Parent GameObject</param>
        /// <returns>List of <see cref="MeshRenderer"/> and <see cref="SkinnedMeshRenderer"/></returns>
        public static List<Renderer> GetRenderers(GameObject gameObject)
        {
            List<Renderer> result = new List<Renderer>();
            result.AddRange(gameObject.GetComponents<MeshRenderer>());
            result.AddRange(gameObject.GetComponents<SkinnedMeshRenderer>());
            result.AddRange(gameObject.GetComponentsInChildren<MeshRenderer>(true));
            result.AddRange(gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true));
            return result;
        }

        /// <summary>
        ///     Get a list of all <see cref="Material"/> of a GameObject and its childs
        /// </summary>
        /// <param name="gameObject">Parent GameObject</param>
        /// <returns>List of <see cref="Material"/></returns>
        public static List<Material> GetMaterials(GameObject gameObject)
        {
            List<Material> result = new List<Material>();
            foreach (Renderer randy in GetRenderers(gameObject))
            {
                result.AddRange(randy.materials);
            }
            return result;
        }
        
        /// <summary>
        ///     Get a list of all shared <see cref="Material"/> of a GameObject and its childs
        /// </summary>
        /// <param name="gameObject">Parent GameObject</param>
        /// <returns>List of <see cref="Material"/></returns>
        public static List<Material> GetSharedMaterials(GameObject gameObject)
        {
            List<Material> result = new List<Material>();
            foreach (Renderer randy in GetRenderers(gameObject))
            {
                result.AddRange(randy.sharedMaterials);
            }
            return result;
        }
        
        /// <summary>
        ///     Get a list of all normal and shared <see cref="Material"/> of a GameObject and its childs
        /// </summary>
        /// <param name="gameObject">Parent GameObject</param>
        /// <returns>List of <see cref="Material"/></returns>
        public static List<Material> GetAllMaterials(GameObject gameObject)
        {
            List<Material> result = new List<Material>();
            foreach (Renderer randy in GetRenderers(gameObject))
            {
                result.AddRange(randy.materials);
                result.AddRange(randy.sharedMaterials);
            }
            return result;
        }
    }
}
