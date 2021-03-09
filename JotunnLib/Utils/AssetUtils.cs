using System;
using System.IO;
using UnityEngine;
using BepInEx;

namespace JotunnLib.Utils
{
    public static class AssetUtils
    {
        // Texture path relative to "plugins" BepInEx folder
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

        // Mesh path relative to "plugins" BepInEx folder
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
