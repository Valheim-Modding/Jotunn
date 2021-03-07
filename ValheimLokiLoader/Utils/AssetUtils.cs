using System;
using System.IO;
using UnityEngine;
using BepInEx;

namespace ValheimLokiLoader.Utils
{
    public static class AssetUtils
    {
        // Texture path relative to "plugins" BepInEx folder
        public static Texture2D LoadTexture(string texturePath)
        {
            string path = Path.Combine(Paths.PluginPath, texturePath);
            Debug.Log("Loading texture from: " + path);

            if (!File.Exists(path))
            {
                return null;
            }

            byte[] fileData = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(fileData);
            return tex;
        }
    }
}
