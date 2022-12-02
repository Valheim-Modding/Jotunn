using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
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
        public const string Version = "2.10.0";

        /// <summary>
        ///     The name of the library.
        /// </summary>
        public const string ModName = "Jotunn";

        /// <summary>
        ///     The BepInEx plugin Mod GUID being used (com.jotunn.jotunn).
        /// </summary>
        public const string ModGuid = "com.jotunn.jotunn";

        internal static Main Instance;
        internal static Harmony Harmony;
        internal static GameObject RootObject;

        private List<IManager> Managers;

        private void Awake()
        {
            // Set instance
            Instance = this;

            // Harmony patches
            Harmony = new Harmony(ModGuid);
            Harmony.PatchAll(typeof(ModCompatibility));

            // Initialize Logger
            Jotunn.Logger.Init();

            // Root Container for GameObjects in the DontDestroyOnLoad scene
            RootObject = new GameObject("_JotunnRoot");
            DontDestroyOnLoad(RootObject);

            // Create and initialize all managers
            Managers = new List<IManager>
            {
                LocalizationManager.Instance,
                CommandManager.Instance,
                InputManager.Instance,
                SkillManager.Instance,
                PrefabManager.Instance,
                ItemManager.Instance,
                PieceManager.Instance,
                CreatureManager.Instance,
                ZoneManager.Instance,
                MockManager.Instance,
                KitbashManager.Instance,
                GUIManager.Instance,
                KeyHintManager.Instance,
                NetworkManager.Instance,
                SynchronizationManager.Instance,
                RenderManager.Instance,
                MinimapManager.Instance,
                UndoManager.Instance
            };
            foreach (IManager manager in Managers)
            {
                Logger.LogInfo("Initializing " + manager.GetType().Name);
                manager.Init();
            }

            ModQuery.Init();
            ModCompatibility.Init();

#if DEBUG
            // Enable helper on DEBUG build
            RootObject.AddComponent<DebugUtils.DebugHelper>();
#endif

            // Flip the "modded" switch of Valheim
            Game.isModded = true;

            Logger.LogInfo("Jötunn v" + Version + " loaded successfully");
        }

        private void Start()
        {
            InitializePatches();
        }

        private void OnApplicationQuit()
        {
            // Unload still loaded asset bundles to keep unity from crashing
            AssetBundle.UnloadAllAssetBundles(false);
        }

        /// <summary>
        ///     Invoke patch initialization methods for all loaded mods.
        /// </summary>
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
#pragma warning disable CS0618 // Type or member is obsolete
                            // on methods with the PatchInit attribute
                            foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public)
                                .Where(x => x.GetCustomAttributes(typeof(PatchInitAttribute), false).Length == 1))
                            {
                                var attribute = method.GetCustomAttributes(typeof(PatchInitAttribute), false).FirstOrDefault() as PatchInitAttribute;
                                types.Add(new Tuple<MethodInfo, int>(method, attribute.Priority));
                            }
#pragma warning restore CS0618 // Type or member is obsolete
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
    }
}
