using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using HarmonyLib;
using Jotunn.Extensions;
using Jotunn.Utils;
using SoftReferenceableAssets;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Jotunn.Managers
{
    public class AssetManager : IManager
    {
        private static AssetManager instance;

        /// <summary>
        ///     The singleton instance of this manager.
        /// </summary>
        public static AssetManager Instance => instance ??= new AssetManager();

        private Dictionary<AssetID, AssetRef> assets = new Dictionary<AssetID, AssetRef>();

        private Dictionary<Type, Dictionary<string, AssetID>> mapNameToAssetID;
        private Dictionary<Type, Dictionary<string, AssetID>> MapNameToAssetID => mapNameToAssetID ??= CreateNameToAssetID();

        /// <summary>
        ///     Hide .ctor
        /// </summary>
        private AssetManager() { }

        static AssetManager()
        {
            ((IManager)Instance).Init();
        }

        void IManager.Init()
        {
            Main.LogInit(nameof(AssetManager));
            Main.Harmony.PatchAll(typeof(Patches));
        }

        private static class Patches
        {
            [HarmonyPatch(typeof(AssetBundleLoader), nameof(AssetBundleLoader.InitializeDataSide)), HarmonyPostfix]
            private static void AssetBundleLoader_Load(AssetBundleLoader __instance)
            {
                foreach (var prefab in Instance.assets)
                {
                    AddAssetToBundleLoader(__instance, prefab.Key, prefab.Value);
                }
            }
        }

        public bool IsReady()
        {
            return Runtime.s_assetLoader != null;
        }

        public AssetID AddPrefab(GameObject prefab, GameObject original = null)
        {
            AssetID assetID = GenerateFakeAssetID(prefab);

            if (assets.ContainsKey(assetID))
            {
                return assetID;
            }

            AssetRef assetRef = new AssetRef(BepInExUtils.GetSourceModMetadata(), prefab, original);
            assets.Add(assetID, assetRef);

            if (AssetBundleLoader.Instance != null)
            {
                AddAssetToBundleLoader(AssetBundleLoader.Instance, assetID, assetRef);
            }

            return assetID;
        }

        private static void AddAssetToBundleLoader(AssetBundleLoader assetBundleLoader, AssetID assetID, AssetRef assetRef)
        {
            // create fake bundle, since an AssetBundle can't be created at runtime
            string bundleName = $"JVL_BundleWrapper_{assetRef.asset.name}";
            string assetPath = $"{assetRef.sourceMod.GUID}/Prefabs/{assetRef.asset.name}";
            AssetLocation location = new AssetLocation(bundleName, assetPath);
            BundleLoader bundleLoader = new BundleLoader(bundleName, "");
            bundleLoader.HoldReference();
            assetBundleLoader.m_bundleNameToLoaderIndex.Add(bundleName, assetBundleLoader.m_bundleLoaders.Length);
            assetBundleLoader.m_bundleLoaders = assetBundleLoader.m_bundleLoaders.AddItem(bundleLoader).ToArray();

            // copy dependencies from original prefab, if available
            int originalBundleLoaderIndex = assetBundleLoader.m_assetLoaders.FirstOrDefault(l => l.m_assetID == assetRef.originalID).m_bundleLoaderIndex;

            if (assetRef.originalID.IsValid && originalBundleLoaderIndex > 0)
            {
                Logger.LogDebug($"Original bundle loader index: {originalBundleLoaderIndex}");
                BundleLoader originalBundleLoader = assetBundleLoader.m_bundleLoaders[originalBundleLoaderIndex];

                bundleLoader.m_bundleLoaderIndicesOfThisAndDependencies = originalBundleLoader
                    .m_bundleLoaderIndicesOfThisAndDependencies
                    .Where(i => i != originalBundleLoaderIndex)
                    .AddItem(assetBundleLoader.m_bundleNameToLoaderIndex[bundleName])
                    .OrderBy(i => i)
                    .ToArray();
            }
            else
            {
                bundleLoader.SetDependencies(Array.Empty<string>());
            }

            // BundleLoader is a struct, so we need to update the array with the changed dependencies
            assetBundleLoader.m_bundleLoaders[assetBundleLoader.m_bundleNameToLoaderIndex[bundleName]] = bundleLoader;

            // add fake asset loader
            AssetLoader loader = new AssetLoader(assetID, location);
            loader.m_asset = assetRef.asset;
            loader.HoldReference();

            assetBundleLoader.m_assetIDToLoaderIndex.Add(assetID, assetBundleLoader.m_assetLoaders.Length);
            assetBundleLoader.m_assetLoaders = assetBundleLoader.m_assetLoaders.AddItem(loader).ToArray();

            Logger.LogDebug($"Added prefab '{assetRef.asset.name}' with ID {assetID} to AssetBundleLoader");
        }

        private static AssetID GenerateFakeAssetID(Object asset)
        {
            uint u = (uint)asset.name.GetStableHashCode();
            return new AssetID(u, u, u, u);
        }

        public GameObject ClonePrefab(GameObject asset, string newName, Transform parent)
        {
            GameObject clone = Object.Instantiate(asset, parent);
            clone.name = newName;

            AddPrefab(clone, asset);

            return clone;
        }

        public AssetID NameToAssetID(Type type, string name)
        {
            if (!IsReady())
            {
                throw new InvalidOperationException("Asset System is not initialized yet");
            }

            if (MapNameToAssetID.TryGetValue(type, out var nameToAssetID) && nameToAssetID.TryGetValue(name, out AssetID assetID))
            {
                return assetID;
            }

            if (MapNameToAssetID.TryGetValue(typeof(Object), out nameToAssetID) && nameToAssetID.TryGetValue(name, out assetID))
            {
                return assetID;
            }

            return new AssetID();
        }

        public AssetID NameToAssetID<T>(string name) where T : Object
        {
            return NameToAssetID(typeof(T), name);
        }

        private static Dictionary<Type, Dictionary<string, AssetID>> CreateNameToAssetID()
        {
            if (!Instance.IsReady())
            {
                throw new InvalidOperationException("Asset System is not initialized yet");
            }

            Dictionary<Type, Dictionary<string, AssetID>> nameToAssetID = new Dictionary<Type, Dictionary<string, AssetID>>();
            Dictionary<string, string> nameToFullPath = new Dictionary<string, string>();

            foreach (var pair in Runtime.GetAllAssetPathsInBundleMappedToAssetID().ToList())
            {
                string key = pair.Key.Split('/').Last();
                string extenstion = key.Split('.').Last();
                string asset = key.RemoveSuffix($".{extenstion}");

                if (pair.Key.StartsWith("Assets/world/Locations") && extenstion == "prefab")
                {
                    // ignore locations, to prevent skipping real ZNet prefabs
                    continue;
                }

                if (pair.Key == "Assets/UI/prefabs/radials/elements/Hammer.prefab")
                {
                    // skip UI element in favour of Assets/GameElements/Items/tools/Hammer.prefab
                    continue;
                }

                Type type = Instance.TypeFromExtension(extenstion);

                if (type == null && extenstion == "asset" && key.StartsWith("Recipe_"))
                {
                    type = typeof(Recipe);
                }

                if (type == null)
                {
                    Logger.LogDebug($"Unhandled extension '{extenstion}' for asset '{pair.Key}'");
                    type = typeof(Object);
                }

                if (!nameToAssetID.ContainsKey(type))
                {
                    nameToAssetID.Add(type, new Dictionary<string, AssetID>());
                }

                if (nameToAssetID[type].ContainsKey(asset))
                {
                    if (extenstion == "prefab")
                    {
                        Logger.LogWarning($"Ambiguous asset name for path: existing: {nameToFullPath[asset]}, new: {pair.Key}");
                    }

                    continue;
                }

                nameToAssetID[type].Add(asset, pair.Value);
                nameToFullPath[asset] = pair.Key;
            }

            return nameToAssetID;
        }

        private Type TypeFromExtension(string extension)
        {
            switch (extension.ToLower())
            {
                case "prefab":
                    return typeof(GameObject);
                case "mat":
                    return typeof(Material);
                case "obj":
                case "fbx":
                    return typeof(Mesh);
                case "png":
                case "jpg":
                case "tga":
                case "tif":
                    return typeof(Texture2D);
                case "wav":
                case "mp3":
                    return typeof(AudioClip);
                case "controller":
                    return typeof(RuntimeAnimatorController);
                case "physicmaterial":
                    return typeof(PhysicMaterial);
                case "shader":
                    return typeof(Shader);
                case "anim":
                    return typeof(AnimationClip);
                case "mixer":
                    return typeof(AudioMixer);
                case "txt":
                    return typeof(TextAsset);
                case "ttf":
                case "otf":
                    return typeof(TMPro.TMP_FontAsset);
                case "rendertexture":
                    return typeof(RenderTexture);
                case "lighting":
                    return typeof(LightingSettings);
                default:
                    return null;
            }
        }

        private struct AssetRef
        {
            public BepInPlugin sourceMod;
            public Object asset;
            public AssetID originalID;

            public AssetRef(BepInPlugin sourceMod, Object asset, Object original)
            {
                this.sourceMod = sourceMod;
                this.asset = asset;
                this.originalID = original ? Instance.NameToAssetID(original.GetType(), original.name) : default;
            }
        }

        public SoftReference<T> GetSoftReference<T>(string name) where T: Object
        {
            AssetID assetID = NameToAssetID<T>(name);
            return assetID.IsValid ? new SoftReference<T>(assetID) : default;
        }

        public SoftReference<Object> GetSoftReference(Type type, string name)
        {
            AssetID assetID = NameToAssetID(type, name);
            return assetID.IsValid ? new SoftReference<Object>(assetID) : default;
        }
    }
}
