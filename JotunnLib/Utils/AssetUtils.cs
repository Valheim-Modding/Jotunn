using System;
using System.IO;
using UnityEngine;
using BepInEx;
using System.Reflection;
using System.Linq;

namespace JotunnLib.Utils
{
    /// <summary>
    ///     Util functions related to loading assets at runtime.
    /// </summary>
    public static class AssetUtils
    {
        /// <summary>
        ///     Loads a <see cref="Texture2D"/> from file at runtime.
        /// </summary>
        /// <param name="texturePath">Texture path relative to "plugins" BepInEx folder</param>
        /// <returns>Texture2D loaded, or null if invalid path</returns>
        public static Texture2D LoadTexture(string texturePath)
        {
            string path = Path.Combine(BepInEx.Paths.PluginPath, texturePath);

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
        ///     Loads a <see cref="Sprite"/> from file at runtime.
        /// </summary>
        /// <param name="texturePath">Texture path relative to "plugins" BepInEx folder</param>
        /// <returns>Texture2D loaded, or null if invalid path</returns>
        public static Sprite LoadSpriteFromFile(string spritePath)
        {
            var tex = LoadTexture(spritePath);

            if (tex != null)
            {
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(), 100);
            }

            return null;
        }

        /// <summary>
        ///     Loads a mesh from a .obj file at runtime.
        /// </summary>
        /// <param name="meshPath">Mesh path relative to "plugins" BepInEx folder</param>
        /// <returns>Texture2D loaded, or null if invalid path</returns>
        public static Mesh LoadMesh(string meshPath)
        {
            string path = Path.Combine(BepInEx.Paths.PluginPath, meshPath);

            if (!File.Exists(path))
            {
                return null;
            }

            return ObjImporter.ImportFile(path);
        }

        /// <summary>
        ///     Loads an asset bundle at runtime.
        /// </summary>
        /// <param name="bundlePath">Asset bundle path relative to "plugins" BepInEx folder</param>
        /// <returns>AssetBundle loaded, or null if invalid path</returns>
        public static AssetBundle LoadAssetBundle(string bundlePath)
        {
            string path = Path.Combine(BepInEx.Paths.PluginPath, bundlePath);

            if (!File.Exists(path))
            {
                return null;
            }

            return AssetBundle.LoadFromFile(path);
        }

        /// <summary>
        ///     Load an assembly-embedded <see cref="AssetBundle" />
        /// </summary>
        /// <param name="bundleName">Name of the bundle</param>
        /// <returns></returns>
        public static AssetBundle LoadAssetBundleFromResources(string bundleName, Assembly resourceAssembly)
        {
            if (resourceAssembly == null)
            {
                throw new ArgumentNullException("Parameter resourceAssembly can not be null.");
            }

            string resourceName = null;
            try
            {
                resourceName = resourceAssembly.GetManifestResourceNames().Single(str => str.EndsWith(bundleName));
            } catch (Exception) { }

            if (resourceName == null)
            {
                Logger.LogError($"AssetBundle {bundleName} not found in assembly manifest");
                return null;
            }

            AssetBundle ret;
            using (var stream = resourceAssembly.GetManifestResourceStream(resourceName))
            {
                ret = AssetBundle.LoadFromStream(stream);
            }

            return ret;
        }
    }
}
