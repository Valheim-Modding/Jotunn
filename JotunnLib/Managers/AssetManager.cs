using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using HarmonyLib;
using Jotunn.Extensions;
using Jotunn.Utils;
using SoftReferenceableAssets;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

namespace Jotunn.Managers
{
    /// <summary>
    ///     Manager to handle interactions with the vanilla asset system, called SoftReferenceableAssets.
    ///     See the <a href="https://valheim.com/support/modding-faq-for-the-asset-bundle-update-0-217-40/">Vanilla FAQ</a> for more information.
    /// </summary>
    public class AssetManager : IManager
    {
        private static AssetManager instance;

        /// <summary>
        ///     The singleton instance of this manager.
        /// </summary>
        public static AssetManager Instance => instance ??= new AssetManager();

        private Dictionary<AssetID, AssetRef> assets = new Dictionary<AssetID, AssetRef>();

        private Dictionary<Type, Dictionary<string, AssetID>> mapNameToAssetID;
        internal Dictionary<Type, Dictionary<string, AssetID>> MapNameToAssetID => mapNameToAssetID ??= CreateNameToAssetID();

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
            [HarmonyPatch(typeof(AssetBundleLoader), nameof(AssetBundleLoader.OnInitCompleted)), HarmonyPostfix]
            private static void AssetBundleLoader_Load(AssetBundleLoader __instance)
            {
                foreach (var prefab in Instance.assets)
                {
                    AddAssetToBundleLoader(__instance, prefab.Key, prefab.Value);
                }
            }
        }

        /// <summary>
        ///     Checks if the vanilla loader is ready.
        ///     If false, <see cref="SoftReferenceableAssets.Runtime.Loader">Runtime.Loader</see> must not be accessed and no assets may be loaded.
        ///     If the vanilla loader is initialized too early, mods can become incompatible.
        /// </summary>
        /// <returns>true if the vanilla asset loader is ready, false otherwise</returns>
        public bool IsReady()
        {
            return Runtime.s_assetLoader != null && ((AssetBundleLoader)Runtime.s_assetLoader).Initialized;
        }

        /// <summary>
        ///     Registers a new asset with the same asset dependencies as the original asset and generates a unique AssetID.<br />
        ///     Assets can be added at any time and will be registered as soon as the vanilla loader is ready.
        /// </summary>
        /// <param name="asset">The asset to register</param>
        /// <param name="original">Assets to copy dependencies from</param>
        /// <returns>AssetID generated from the prefab's name</returns>
        public AssetID AddAsset(Object asset, Object original)
        {
            AssetID assetID = GenerateAssetID(asset);

            if (assets.ContainsKey(assetID))
            {
                return assetID;
            }

            AssetRef assetRef = new AssetRef(BepInExUtils.GetSourceModMetadata(), asset, original);
            assets.Add(assetID, assetRef);

            if (AssetBundleLoader.Instance != null)
            {
                AddAssetToBundleLoader(AssetBundleLoader.Instance, assetID, assetRef);
            }

            return assetID;
        }

        /// <summary>
        ///     Registers a new asset and generates a unique AssetID.<br />
        ///     Assets can be added at any time and will be registered as soon as the vanilla loader is ready.
        /// </summary>
        /// <param name="asset">The asset to register</param>
        /// <returns>AssetID generated from the prefab's name</returns>
        public AssetID AddAsset(Object asset)
        {
            return AddAsset(asset, null);
        }

        private static void AddAssetToBundleLoader(AssetBundleLoader assetBundleLoader, AssetID assetID, AssetRef assetRef)
        {
            // create fake bundle, since an AssetBundle can't be created at runtime
            string bundleName = $"JVL_BundleWrapper_{assetRef.asset.name}";
            string assetPath = $"{assetRef.sourceMod.GUID}/Prefabs/{assetRef.asset.name}";

            if (assetBundleLoader.m_bundleNameToLoaderIndex.ContainsKey(bundleName))
            {
                return;
            }

            AssetLocation location = new AssetLocation(bundleName, assetPath);
            BundleLoader bundleLoader = new BundleLoader(bundleName, "");
            bundleLoader.HoldReference();
            assetBundleLoader.m_bundleNameToLoaderIndex.Add(bundleName, assetBundleLoader.m_bundleLoaders.Length);
            assetBundleLoader.m_bundleLoaders = assetBundleLoader.m_bundleLoaders.AddItem(bundleLoader).ToArray();

            // copy dependencies from original prefab, if available
            int originalBundleLoaderIndex = assetBundleLoader.m_assetLoaders.FirstOrDefault(l => l.m_assetID == assetRef.originalID).m_bundleLoaderIndex;

            if (assetRef.originalID.IsValid && originalBundleLoaderIndex > 0)
            {
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

            Instance.MapNameToAssetID[assetRef.asset.GetType()][assetRef.asset.name] = assetID;

            Logger.LogDebug($"Added prefab '{assetRef.asset.name}' with ID {assetID} to AssetBundleLoader");
        }

        /// <summary>
        ///     Generates a unique AssetID, based on the asset name
        /// </summary>
        /// <param name="asset"></param>
        /// <returns>AssetID generated from the prefab's name</returns>
        public AssetID GenerateAssetID(Object asset)
        {
            uint u = (uint)asset.name.GetStableHashCode();
            return new AssetID(u, u, u, u);
        }

        /// <summary>
        ///     Clones a prefab and registers it in the SoftReference system with the same dependencies as the original asset
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="newName"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public GameObject ClonePrefab(GameObject asset, string newName, Transform parent)
        {
            GameObject clone = Object.Instantiate(asset, parent);
            clone.name = newName;

            AddAsset(clone, asset);

            return clone;
        }

        /// <summary>
        ///     Finds the AssetID by an asset name at runtime.<br />
        ///     The closed matching base type must be used.
        ///     E.g. for prefabs use <see cref="GameObject"/>, for Textures use <see cref="Texture2D"/> etc.<br />
        ///     If no asset is found, an invalid AssetID is returned.
        /// </summary>
        /// <param name="type">Asset type to search for</param>
        /// <param name="name">Asset name to search for</param>
        /// <returns>The AssetID of the searched asset if found, otherwise an invalid AssetID</returns>
        /// <exception cref="InvalidOperationException">Thrown if the vanilla asset system is not initialized yet</exception>
        public AssetID GetAssetID(Type type, string name)
        {
            if (!IsReady())
            {
                throw new InvalidOperationException("The vanilla asset system is not initialized yet");
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

        /// <summary>
        ///     Finds the AssetID by an asset name at runtime.<br />
        ///     The closed matching base type must be used.
        ///     E.g. for prefabs use <see cref="GameObject"/>, for Textures use <see cref="Texture2D"/> etc.<br />
        ///     If no asset is found, an invalid AssetID is returned.
        /// </summary>
        /// <param name="name">Asset name to search for</param>
        /// <typeparam name="T">Asset type to search for</typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown if the vanilla asset system is not initialized yet</exception>
        public AssetID GetAssetID<T>(string name) where T : Object
        {
            return GetAssetID(typeof(T), name);
        }

        /// <summary>
        ///     Finds the AssetID by an asset name at runtime and returns a SoftReference to the asset.<br />
        /// </summary>
        /// <param name="type">Asset type to search for</param>
        /// <param name="name">Asset name to search for</param>
        /// <returns></returns>
        public SoftReference<Object> GetSoftReference(Type type, string name)
        {
            AssetID assetID = GetAssetID(type, name);
            return assetID.IsValid ? new SoftReference<Object>(assetID) : default;
        }

        /// <summary>
        ///     Finds the AssetID by an asset name at runtime and returns a SoftReference to the asset.<br />
        /// </summary>
        /// <param name="name">Asset name to search for</param>
        /// <typeparam name="T">Asset type to search for</typeparam>
        /// <returns></returns>
        public SoftReference<T> GetSoftReference<T>(string name) where T: Object
        {
            AssetID assetID = GetAssetID<T>(name);
            return assetID.IsValid ? new SoftReference<T>(assetID) : default;
        }

        private static Dictionary<Type, Dictionary<string, AssetID>> CreateNameToAssetID()
        {
            if (!Instance.IsReady())
            {
                throw new InvalidOperationException("The vanilla asset system is not initialized yet");
            }

            Dictionary<Type, Dictionary<string, AssetID>> nameToAssetID = new Dictionary<Type, Dictionary<string, AssetID>>();
            Dictionary<string, string> nameToFullPath = new Dictionary<string, string>();

            foreach (var pair in Runtime.GetAllAssetPathsInBundleMappedToAssetID().ToList())
            {
                string key = pair.Key.Split('/').Last();
                string extenstion = key.Split('.').Last();
                string asset = key.RemoveSuffix($".{extenstion}");

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
                    type = typeof(Object);
                }

                if (!nameToAssetID.ContainsKey(type))
                {
                    nameToAssetID.Add(type, new Dictionary<string, AssetID>());
                }

                if (nameToAssetID[type].ContainsKey(asset) && SkipAmbiguousPath(nameToFullPath[asset], pair.Key, extenstion))
                {
                    continue;
                }

                nameToAssetID[type][asset] = pair.Value;
                nameToFullPath[asset] = pair.Key;
            }

            return nameToAssetID;
        }

        private static bool SkipAmbiguousPath(string oldPath, string newPath, string extension)
        {
            if (extension == "prefab")
            {
                if (oldPath.StartsWith("Assets/world/Locations"))
                {
                    return false;
                }
                else if (newPath.StartsWith("Assets/world/Locations"))
                {
                    return true;
                }

                Logger.LogWarning($"Ambiguous asset name for path. old: {oldPath}, new: {newPath}, using old path");
            }

            return true;
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
                this.originalID = original && Instance.IsReady() ? Instance.GetAssetID(original.GetType(), original.name) : default;
            }
        }
    }
}
