using System;
using System.IO;
using UnityEngine;
using BepInEx;

namespace JotunnLib.Utils
{
    /// <summary>
    /// Util functions related to loading assets at runtime.
    /// </summary>
    public static class AssetUtils
    {
        /// <summary>
        /// Loads a 2D texture from file at runtime.
        /// </summary>
        /// <param name="texturePath">Texture path relative to "plugins" BepInEx folder</param>
        /// <returns>Texture2D loaded, or null if invalid path</returns>
        public static Texture2D LoadTexture(string texturePath)
        {
            string path = Path.Combine(Paths.PluginPath, texturePath);

            if (!File.Exists(path))
            {
                return null;
            }

            byte[] fileData = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(fileData);
            return tex;
        }

        /// <summary>
        /// Loads a mesh from a .obj file at runtime.
        /// </summary>
        /// <param name="meshPath">Mesh path relative to "plugins" BepInEx folder</param>
        /// <returns>Texture2D loaded, or null if invalid path</returns>
        public static Mesh LoadMesh(string meshPath)
        {
            string path = Path.Combine(Paths.PluginPath, meshPath);

            if (!File.Exists(path))
            {
                return null;
            }

            return ObjImporter.ImportFile(path);
        }
    }
}
