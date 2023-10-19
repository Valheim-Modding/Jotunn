using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace Jotunn
{
    /// <summary>
    ///     Main class implementing BaseUnityPlugin.
    /// </summary>
    [BepInPlugin(ModGuid, ModName, Version)]
    [BepInDependency("com.bepis.bepinex.configurationmanager", BepInDependency.DependencyFlags.SoftDependency)]
    [NetworkCompatibility(CompatibilityLevel.VersionCheckOnly, VersionStrictness.Minor)]
    public class Main : BaseUnityPlugin
    {
        /// <summary>
        ///     The current version of the Jotunn library.
        /// </summary>
        public const string Version = "2.14.5";

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
            InitializePatches();
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

        /// <summary>
        ///     Invoke patch initialization methods for all loaded mods.
        /// </summary>
        [Obsolete]
        private void InitializePatches()
        {
            List<Tuple<MethodInfo, int>> types = new List<Tuple<MethodInfo, int>>();
            HashSet<Assembly> searchedAssemblies = new HashSet<Assembly>();

            // Check in all Jotunn mods
            foreach (var baseUnityPlugin in BepInExUtils.GetDependentPlugins().Values)
            {
                try
                {
                    Assembly asm = baseUnityPlugin.GetType().Assembly;

                    // Skip already searched assemblies
                    if (searchedAssemblies.Contains(asm))
                    {
                        continue;
                    }

                    searchedAssemblies.Add(asm);

                    // Search in all types
                    foreach (var type in asm.GetTypes())
                    {
                        try
                        {
                            // on methods with the PatchInit attribute
                            foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public)
                                .Where(x => x.GetCustomAttributes(typeof(PatchInitAttribute), false).Length == 1))
                            {
                                var attribute = method.GetCustomAttributes(typeof(PatchInitAttribute), false).FirstOrDefault() as PatchInitAttribute;
                                types.Add(new Tuple<MethodInfo, int>(method, attribute.Priority));
                            }
                        }
                        catch (Exception)
                        { }
                    }
                }
                catch (Exception)
                { }
            }

            // Invoke the method
            foreach (Tuple<MethodInfo, int> tuple in types.OrderBy(x => x.Item2))
            {
                Jotunn.Logger.LogDebug($"Applying patches in {tuple.Item1.DeclaringType.Name}.{tuple.Item1.Name}");
                tuple.Item1.Invoke(null, null);
            }
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
