using System;
using System.IO;
using UnityEngine;
using System.Reflection;
using System.Linq;
using BepInEx;
using HarmonyLib;

namespace Jotunn.Utils
{
    /// <summary>
    ///     Util functions related to loading assets at runtime.
    /// </summary>
    public static class AssetUtils
    {
        /// <summary>
        ///     Path separator for AssetBundles
        /// </summary>
        public const char AssetBundlePathSeparator = '$';

        /// <summary>
        ///     Method reference to <see cref="ImageConversion.LoadImage(Texture2D, byte[])" />.
        ///     Workaround for compiling a net 4.x mod against the nestandard 2.1 method reference.
        /// </summary>
        private static MethodInfo LoadImageMethod { get; } = AccessTools.Method(typeof(ImageConversion), nameof(ImageConversion.LoadImage), new Type[] { typeof(Texture2D), typeof(byte[]) });

        /// <summary>
        ///     Loads a <see cref="Texture2D"/> from file at runtime.
        /// </summary>
        /// <param name="texturePath">Texture path relative to "plugins" BepInEx folder</param>
        /// <param name="relativePath">Is the given path relative</param>
        /// <returns>Texture2D loaded, or null if invalid path</returns>
        public static Texture2D LoadTexture(string texturePath, bool relativePath = true)
        {
            string path = texturePath;

            if (relativePath)
            {
                path = Path.Combine(BepInEx.Paths.PluginPath, texturePath);
            }

            if (!File.Exists(path))
            {
                return null;
            }

            // Ensure it's a texture
            if (!path.EndsWith(".png") && !path.EndsWith(".jpg"))
            {
                throw new Exception("LoadTexture can only load png or jpg textures");
            }

            byte[] fileData = File.ReadAllBytes(path);
            return LoadImage(fileData);
        }

        /// <summary>
        ///     Wrapper for https://docs.unity3d.com/ScriptReference/ImageConversion.LoadImage.html,
        ///     creates a new <see cref="Texture2D"/>.
        /// </summary>
        /// <param name="data">The byte array containing the image data to load.</param>
        /// <returns>A new texture with the loaded image if the data can be loaded, null otherwise</returns>
        public static Texture2D LoadImage(byte[] data)
        {
            Texture2D tex = new Texture2D(2, 2);
            bool success = LoadImage(tex, data);
            return success ? tex : null;
        }

        /// <summary>
        ///     Wrapper for https://docs.unity3d.com/ScriptReference/ImageConversion.LoadImage.html.
        /// </summary>
        /// <param name="texture">The texture to load the image into.</param>
        /// <param name="data">The byte array containing the image data to load.</param>
        /// <returns>true if the data can be loaded, false otherwise</returns>
        public static bool LoadImage(Texture2D texture, byte[] data)
        {
            return (bool)LoadImageMethod.Invoke(null, new object[] { texture, data });
        }

        /// <summary>
        ///     Creates a readable copy of a rectangular region from a <see cref="Texture2D"/>.
        /// </summary>
        /// <param name="texture">Source texture.</param>
        /// <param name="textureRect">Region of the texture to copy.</param>
        /// <returns>A readable <see cref="Texture2D"/> of the specified region, or null if the texture is null.</returns>
        public static Texture2D DuplicateTexture(Texture2D texture, Rect textureRect)
        {
            if (!texture)
            {
                return null;
            }

            // The resulting sprite dimensions
            int width = (int)textureRect.width;
            int height = (int)textureRect.height;

            // The location of the target icon in the texture
            int x = (int)textureRect.x;
            int y = (int)textureRect.y;

            RenderTexture previous = RenderTexture.active;

            // Our RenderTexture for displaying the whole sprite atlas.
            // Be sure to match format of your texture or else it may display strangely
            // such as a darker image than the original.
            RenderTexture renderTex = RenderTexture.GetTemporary(
                texture.width,
                texture.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.sRGB
            );

            Graphics.Blit(texture, renderTex);
            RenderTexture.active = renderTex;

            // Create a copy of the target icon texture that is readable
            Texture2D readableTexture = new Texture2D(width, height);
            readableTexture.ReadPixels(new Rect(x, y, width, height), 0, 0);
            readableTexture.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);

            return readableTexture;
        }

        /// <summary>
        ///     Creates a readable copy of a <see cref="Texture2D"/>.
        /// </summary>
        /// <param name="texture">Source texture.</param>
        /// <returns>A readable copy of the texture, or null if the texture is null.</returns>
        public static Texture2D DuplicateTexture(Texture2D texture)
        {
            return DuplicateTexture(texture, new Rect(0, 0, texture.width, texture.height));
        }

        /// <summary>
        ///     Creates a readable copy of a <see cref="Sprite"/>'s texture region.
        ///     If the sprite is part of an atlas, only its texture rectangle is copied.<br/>
        ///     Use <c>AssetUtils.DuplicateTexture(sprite.texture)</c> to copy the full texture.
        /// </summary>
        /// <param name="sprite">Source sprite.</param>
        /// <returns>A readable <see cref="Texture2D"/> of the sprite’s texture region, or null if the texture is null.</returns>
        public static Texture2D DuplicateTexture(Sprite sprite)
        {
            return DuplicateTexture(sprite.texture, sprite.textureRect);
        }

        /// <summary>
        ///     Creates a readable copy of a <see cref="Sprite"/>.
        /// </summary>
        /// <param name="sprite">Source sprite.</param>
        /// <returns>A new <see cref="Sprite"/> with a readable copy of its texture region, or null if the texture is null.</returns>
        public static Sprite DuplicateSprite(Sprite sprite)
        {
            Texture2D tex = DuplicateTexture(sprite);
            return tex ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f)) : null;
        }

        /// <summary>
        ///     Loads a <see cref="Sprite"/> from file at runtime.
        /// </summary>
        /// <param name="spritePath">Texture path relative to "plugins" BepInEx folder</param>
        /// <returns>Texture2D loaded, or null if invalid path</returns>
        public static Sprite LoadSpriteFromFile(string spritePath)
        {
            return LoadSpriteFromFile(spritePath, Vector2.zero);
        }

        /// <summary>
        ///     Loads a <see cref="Sprite"/> from file at runtime.
        /// </summary>
        /// <param name="spritePath">Texture path relative to "plugins" BepInEx folder</param>
        /// <param name="pivot">The pivot to use in the resulting Sprite</param>
        /// <returns>Texture2D loaded, or null if invalid path</returns>
        public static Sprite LoadSpriteFromFile(string spritePath, Vector2 pivot)
        {
            var tex = LoadTexture(spritePath);

            if (tex != null)
            {
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot);
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
        ///     Load an assembly-embedded <see cref="AssetBundle" />. Use this if the automatic detection of the assembly fails.
        /// </summary>
        /// <param name="bundleName">Name of the bundle. Folders are point-seperated e.g. folder/bundle becomes folder.bundle</param>
        /// <param name="resourceAssembly">Executing assembly</param>
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

        /// <summary>
        ///     Load an assembly-embedded <see cref="AssetBundle" />. The calling assembly is automatically detected.
        /// </summary>
        /// <param name="bundleName">Name of the bundle. Folders are point-seperated e.g. folder/bundle becomes folder.bundle</param>
        /// <returns></returns>
        public static AssetBundle LoadAssetBundleFromResources(string bundleName)
        {
            return LoadAssetBundleFromResources(bundleName, ReflectionHelper.GetCallingAssembly());
        }

        /// <summary>
        ///     Load an assembly-embedded file as a <see cref="string" />. Use this if the automatic detection of the assembly fails.
        /// </summary>
        /// <param name="fileName">Name of the file. Folders are point-seperated e.g. folder/file.json becomes folder.file.json</param>
        /// <param name="resourceAssembly">Executing assembly</param>
        /// <returns></returns>
        public static string LoadTextFromResources(string fileName, Assembly resourceAssembly)
        {
            if (resourceAssembly == null)
            {
                throw new ArgumentNullException("Parameter resourceAssembly can not be null.");
            }

            string resourceName = null;
            try
            {
                resourceName = resourceAssembly.GetManifestResourceNames().Single(str => str.EndsWith(fileName));
            } catch (Exception) { }

            if (resourceName == null)
            {
                Logger.LogError($"File {fileName} not found in assembly manifest");
                return null;
            }

            string ret;
            using (var stream = resourceAssembly.GetManifestResourceStream(resourceName))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    ret = reader.ReadToEnd();
                }
            }

            return ret;
        }

        /// <summary>
        ///     Load an assembly-embedded file as a <see cref="string" />. The calling assembly is automatically detected.
        /// </summary>
        /// <param name="fileName">Name of the file. Folders are point-seperated e.g. folder/file.json becomes folder.file.json</param>
        /// <returns></returns>
        public static string LoadTextFromResources(string fileName)
        {
            return LoadTextFromResources(fileName, ReflectionHelper.GetCallingAssembly());
        }

        /// <summary>
        ///     Loads the contents of a file as a char string
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string LoadText(string path)
        {
            string absPath = Path.Combine(BepInEx.Paths.PluginPath, path);

            if (!File.Exists(absPath))
            {
                Logger.LogError($"Error, failed to load contents from non-existant path: ${absPath}");
                return null;
            }

            return File.ReadAllText(absPath);
        }
        
        /// <summary>
        ///     Loads a <see cref="Sprite"/> from a file path or an asset bundle (separated by <see cref="AssetBundlePathSeparator"/>)
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        public static Sprite LoadSprite(string assetPath)
        {
            string path = Path.Combine(BepInEx.Paths.PluginPath, assetPath);

            if (!File.Exists(path))
            {
                return null;
            }

            // Check if asset is from a bundle or from a path
            if (path.Contains(AssetBundlePathSeparator.ToString()))
            {
                string[] parts = path.Split(AssetBundlePathSeparator);
                string bundlePath = parts[0];
                string assetName = parts[1];

                // TODO: This is very likely going to need some caching for asset bundles
                AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
                Sprite ret = bundle.LoadAsset<Sprite>(assetName);
                bundle.Unload(false);
                return ret;
            }

            // Load texture and create sprite
            Texture2D texture = LoadTexture(path, false);
            
            if (!texture)
            {
                return null;
            }

            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), Vector2.zero);
        }

        internal static bool TryLoadPrefab(BepInPlugin sourceMod, AssetBundle assetBundle, string assetName, out GameObject prefab)
        {
            try
            {
                prefab = assetBundle.LoadAsset<GameObject>(assetName);
            }
            catch (Exception e)
            {
                prefab = null;
                Logger.LogError(sourceMod, $"Failed to load prefab '{assetName}' from AssetBundle {assetBundle}:\n{e}");
                return false;
            }

            if (!prefab)
            {
                Logger.LogError(sourceMod, $"Failed to load prefab '{assetName}' from AssetBundle {assetBundle}");
                return false;
            }

            return true;
        }
    }
}
