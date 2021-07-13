using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using BepInEx;
using Jotunn.Managers;
using Jotunn.Utils;

namespace Jotunn
{
    /// <summary>
    ///     Main class implementing BaseUnityPlugin.
    /// </summary>
    [BepInPlugin(ModGuid, ModName, Version)]
    [NetworkCompatibility(CompatibilityLevel.OnlySyncWhenInstalled, VersionStrictness.Minor)]
    public class Main : BaseUnityPlugin
    {
        /// <summary>
        ///     The current version of the Jotunn library.
        /// </summary>
        public const string Version = "2.1.3";

        /// <summary>
        ///     The name of the library.
        /// </summary>
        public const string ModName = "Jötunn";

        /// <summary>
        ///     The BepInEx plugin Mod GUID being used (com.jotunn.jotunn).
        /// </summary>
        public const string ModGuid = "com.jotunn.jotunn";

        internal static GameObject RootObject;

        private List<IManager> managers;

        private void Awake()
        {
            // Initialize Logger
            Jotunn.Logger.Init();

            // Root Container for GameObjects in the DontDestroyOnLoad scene
            RootObject = new GameObject("_JotunnRoot");
            DontDestroyOnLoad(RootObject);

            // Create and initialize all managers
            managers = new List<IManager>()
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
                //SaveManager.Instance,  // Temporarely disabled, causes FPS issues in the current implementation
                //ZoneManager.Instance,  // Had some problems reported, needs more tests
                SynchronizationManager.Instance
                
            };
            foreach (IManager manager in managers)
            {
                Logger.LogInfo("Initializing " + manager.GetType().Name);
                manager.Init();
            }

#if DEBUG
            // Enable some helper on DEBUG build
            RootObject.AddComponent<DebugUtils.DebugHelper>();
            RootObject.AddComponent<DebugUtils.ZoneCounter>();
#endif
            Logger.LogInfo("Jötunn v" + Version + " loaded successfully");
        }

        private void Start()
        {
            InitializePatches();
        }

        private void Update()
        {
            if (ZNet.instance != null)
            {
                if (ZNet.instance.IsServerInstance() || ZNet.instance.IsLocalInstance())
                {
                    SynchronizationManager.Instance.AdminListUpdate();
                }
            }
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
                Jotunn.Logger.LogInfo($"Applying patches in {tuple.Item1.DeclaringType.Name}.{tuple.Item1.Name}");
                tuple.Item1.Invoke(null, null);
            }
        }
    }
}
