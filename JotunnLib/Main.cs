using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace Jotunn
{
    /// <summary>
    ///     Main class implementing BaseUnityPlugin.
    /// </summary>
    [BepInPlugin(ModGuid, ModName, Version)]
    [NetworkCompatibility(CompatibilityLevel.VersionCheckOnly, VersionStrictness.Minor)]
    public class Main : BaseUnityPlugin
    {
        /// <summary>
        ///     The current version of the Jotunn library.
        /// </summary>
        public const string Version = "2.4.3";

        /// <summary>
        ///     The name of the library.
        /// </summary>
        public const string ModName = "Jotunn";

        /// <summary>
        ///     The BepInEx plugin Mod GUID being used (com.jotunn.jotunn).
        /// </summary>
        public const string ModGuid = "com.jotunn.jotunn";

        internal static Main Instance;
        internal static GameObject RootObject;

        private List<IManager> Managers;

        private void Awake()
        {
            // Set instance
            Instance = this;

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
                MockManager.Instance,
                KitbashManager.Instance,
                GUIManager.Instance,
                KeyHintManager.Instance,
                //SaveManager.Instance,  // Temporarely disabled, causes FPS issues in the current implementation
                ZoneManager.Instance,
                NetworkManager.Instance,
                SynchronizationManager.Instance,
                //MapOverlayManager.Instance,  // Not ready yet
                RenderManager.Instance,
            };
            foreach (IManager manager in Managers)
            {
                Logger.LogInfo("Initializing " + manager.GetType().Name);
                manager.Init();
            }

#if DEBUG
            // Enable helper on DEBUG build
            RootObject.AddComponent<DebugUtils.DebugHelper>();
#endif
            
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
            // Reflect through everything
            List<Tuple<MethodInfo, int>> types = new List<Tuple<MethodInfo, int>>();

            // Check in all assemblies
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    // And all types
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
    }
}
