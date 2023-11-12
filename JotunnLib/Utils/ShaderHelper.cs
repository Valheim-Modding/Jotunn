using System;
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
        ///     Create a new, scaled texture from a given texture.
        /// </summary>
        /// <param name="texture">Source texture to scale</param>
        /// <param name="width">New width of the scaled texture</param>
        /// <returns></returns>
        public static Texture2D CreateScaledTexture(Texture2D texture, int width)
        {
            Texture2D copyTexture = new Texture2D(texture.width, texture.height, texture.format, false);
            copyTexture.SetPixels(texture.GetPixels());
            copyTexture.Apply();
            ScaleTexture(copyTexture, width);
            return copyTexture;
        }

        /// <summary>
        ///     Scale a texture to a certain width, aspect ratio is preserved.
        /// </summary>
        /// <param name="texture">Texture to scale</param>
        /// <param name="width">New width of the scaled texture</param>
        public static void ScaleTexture(Texture2D texture, int width)
        {
            Texture2D copyTexture = new Texture2D(texture.width, texture.height, texture.format, false);
            copyTexture.SetPixels(texture.GetPixels());
            copyTexture.Apply();

            int height = (int)Math.Round((float)width * texture.height / texture.width);
            texture.Reinitialize(width, height);
            texture.Apply();

            Color[] rpixels = texture.GetPixels(0);
            float incX = 1.0f / width;
            float incY = 1.0f / height;
            for (int px = 0; px < rpixels.Length; px++)
            {
                rpixels[px] = copyTexture.GetPixelBilinear(incX * ((float)px % width), incY * Mathf.Floor((float)px / width));
            }
            texture.SetPixels(rpixels, 0);
            texture.Apply();

            UnityEngine.Object.Destroy(copyTexture);

            /*for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var xp = 1f * x / width;
                    var yp = 1f * y / height;
                    var xo = (int)Mathf.Round(xp * copyTexture.width); // Other X pos
                    var yo = (int)Mathf.Round(yp * copyTexture.height); // Other Y pos
                    Color origPixel = copyTexture.GetPixel(xo, yo);
                    //origPixel.a = 1f;
                    texture.SetPixel(x, y, origPixel);
                }
            }
            texture.Apply();
            UnityEngine.Object.Destroy(copyTexture);*/
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
