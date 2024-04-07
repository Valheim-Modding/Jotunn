using System;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using SoftReferenceableAssets;
using UnityEngine;

namespace Jotunn
{
    /// <summary>
    ///     Main class implementing BaseUnityPlugin.
    /// </summary>
    [BepInPlugin(ModGuid, ModName, Version)]
    [BepInDependency("com.bepis.bepinex.configurationmanager", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.maxsch.valheim.LocalizationCache", BepInDependency.DependencyFlags.SoftDependency)]
    [NetworkCompatibility(CompatibilityLevel.VersionCheckOnly, VersionStrictness.Minor)]
    public class Main : BaseUnityPlugin
    {
        /// <summary>
        ///     The current version of the Jotunn library.
        /// </summary>
        public const string Version = "2.18.2";

        /// <summary>
        ///     The name of the library.
        /// </summary>
        public const string ModName = "Jotunn";

        /// <summary>
        ///     The BepInEx plugin Mod GUID being used (com.jotunn.jotunn).
        /// </summary>
        public const string ModGuid = "com.jotunn.jotunn";

        internal static Main Instance;
        internal static Harmony Harmony = new Harmony(ModGuid);

        private static GameObject rootObject;

        internal static GameObject RootObject => GetRootObject();

        private void Awake()
        {
            Instance = this;
            GetRootObject();

            ModCompatibility.Init();
            ((IManager)SynchronizationManager.Instance).Init();
            Runtime.MakeAllAssetsLoadable();

            // Flip the "modded" switch of Valheim
            Game.isModded = true;
        }

        private void Start()
        {
#if DEBUG
            if (!Chainloader.PluginInfos.ContainsKey("randyknapp.mods.auga"))
            {
                // Enable helper on DEBUG build
                RootObject.AddComponent<DebugUtils.DebugHelper>();
            }
#endif

#pragma warning disable CS0612 // Method is obsolete
            PatchInit.InitializePatches();
#pragma warning restore CS0612 // Method is obsolete

            AutomaticLocalizationsLoading.Init();
        }

        private void OnApplicationQuit()
        {
            // Unload still loaded asset bundles to keep unity from crashing
            AssetBundle.UnloadAllAssetBundles(false);
        }

        private static GameObject GetRootObject()
        {
            if (rootObject)
            {
                return rootObject;
            }

            // create root container for GameObjects in the DontDestroyOnLoad scene
            rootObject = new GameObject("_JotunnRoot");
            DontDestroyOnLoad(rootObject);
            return rootObject;
        }

        internal static void LogInit(string module)
        {
            Jotunn.Logger.LogInfo($"Initializing {module}");

            if (!Instance)
            {
                string message = $"{module} was accessed before Jotunn Awake, this can cause unexpected behaviour. " +
                                 "Please make sure to add `[BepInDependency(Jotunn.Main.ModGuid)]` next to your BaseUnityPlugin";
                Jotunn.Logger.LogWarning(BepInExUtils.GetSourceModMetadata(), message);
            }
        }
    }
}
