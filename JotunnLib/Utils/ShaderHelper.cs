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
            result.AddRange(gameObject.GetComponentsInChildren<MeshRenderer>(true));
            result.AddRange(gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true));
            return result;
        }

        /// <summary>
        ///     Get a list of all renderer <see cref="Material"/> of a GameObject and its childs
        /// </summary>
        /// <param name="gameObject">Parent GameObject</param>
        /// <returns>List of <see cref="Material"/></returns>
        public static List<Material> GetRendererMaterials(GameObject gameObject)
        {
            List<Material> result = new List<Material>();
            foreach (Renderer randy in GetRenderers(gameObject))
            {
                result.AddRange(randy.materials);
            }
            return result;
        }

        /// <summary>
        ///     Get a list of all shared renderer <see cref="Material"/> of a GameObject and its childs
        /// </summary>
        /// <param name="gameObject">Parent GameObject</param>
        /// <returns>List of <see cref="Material"/></returns>
        public static List<Material> GetRendererSharedMaterials(GameObject gameObject)
        {
            List<Material> result = new List<Material>();
            foreach (Renderer randy in GetRenderers(gameObject))
            {
                result.AddRange(randy.sharedMaterials);
            }
            return result;
        }

        /// <summary>
        ///     Get a list of all normal and shared renderer <see cref="Material"/> of a GameObject and its childs
        /// </summary>
        /// <param name="gameObject">Parent GameObject</param>
        /// <returns>List of <see cref="Material"/></returns>
        public static List<Material> GetAllRendererMaterials(GameObject gameObject)
        {
            List<Material> result = new List<Material>();
            foreach (Renderer randy in GetRenderers(gameObject))
            {
                result.AddRange(randy.materials);
                result.AddRange(randy.sharedMaterials);
            }
            return result;
        }
        
        /// <summary>
        ///     Dumps all shader information of a GameObject and its childs onto debug log
        /// </summary>
        /// <param name="gameObject"></param>
        public static void ShaderDump(GameObject gameObject)
        {
            var mats = GetRendererMaterials(gameObject);
            foreach (Material mat in mats)
            {
                Logger.LogDebug(mat.shader.ToString());
                foreach (string prop in mat.shaderKeywords)
                {
                    Logger.LogDebug(prop);
                }

                foreach (string prop in mat.GetTexturePropertyNames())
                {
                    Logger.LogDebug(prop);
                }

                for (int i = 0; i < mat.shader.GetPropertyCount(); ++i)
                {
                    Logger.LogDebug(mat.shader.GetPropertyName(i));
                }
            }
        }

    }
}
