using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using BepInEx;
using BepInEx.Configuration;
using JotunnLib.Managers;
using JotunnLib.Utils;

namespace JotunnLib
{
    [BepInPlugin(ModGuid, "JotunnLib", Version)]
    [NetworkCompatibilty(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    public class Main : BaseUnityPlugin
    {
        /// <summary>
        ///     The current version of the Jotunn library.
        /// </summary>
        public const string Version = "0.2.0";

        /// <summary>
        ///     The BepInEx plugin Mod GUID being used.
        /// </summary>
        public const string ModGuid = "com.bepinex.plugins.jotunnlib";

        internal static GameObject RootObject;

        // Load order for managers
        private readonly List<Type> managerTypes = new List<Type>()
        {
            typeof(LocalizationManager),
            typeof(EventManager),
            typeof(CommandManager),
            typeof(InputManager),
            typeof(SkillManager),
            typeof(PrefabManager),
            typeof(ItemManager),
            typeof(PieceManager),
            typeof(MockManager),
            typeof(ZoneManager),
            typeof(GUIManager),
            typeof(SaveManager),
            typeof(SynchronizationManager)
        };
        private readonly List<Manager> managers = new List<Manager>();

        internal void Awake()
        {
            // Initialize Logger
            JotunnLib.Logger.Init();

            // Create and initialize all managers
            RootObject = new GameObject("_JotunnLibRoot");
            GameObject.DontDestroyOnLoad(RootObject);

            foreach (Type managerType in managerTypes)
            {
                managers.Add((Manager)RootObject.AddComponent(managerType));
            }

            foreach (Manager manager in managers)
            {
                manager.Init();
                Logger.LogInfo("Initialized " + manager.GetType().Name);
            }

            Logger.LogInfo("JotunnLib v" + Version + " loaded successfully");

#if DEBUG
            Config.Bind("ModCompatibilityTest", "Enable", true, new ConfigDescription("Enable to test Mod compatibility testing", null));
            Config.SettingChanged += ModCompatibility.Config_SettingChanged;
            ModCompatibility.enableTestCase = (bool)Config["ModCompatibilityTest", "Enable"].BoxedValue;
#endif
        }


        /// <summary>
        ///     Initialize patches
        /// </summary>
        internal void Start()
        {
            initializePatches();
        }

        internal void Update()
        {
#if DEBUG
            if (Input.GetKeyDown(KeyCode.F6))
            { // Set a breakpoint here to break on F6 key press
            }
#endif
        }

        private void OnGUI()
        {
            // Display version in main menu
            if (SceneManager.GetActiveScene().name == "start")
            {
                GUI.Label(new Rect(Screen.width - 100, 5, 100, 25), "JotunnLib v" + Version);
            }
        }

        /// <summary>
        ///     Invoke patch initialization methods for all loaded mods.
        /// </summary>
        private void initializePatches()
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
                        catch (Exception ex)
                        { }
                    }
                }
                catch (Exception e)
                { }
            }

            // Invoke the method
            foreach (Tuple<MethodInfo, int> tuple in types.OrderBy(x => x.Item2))
            {
                Logger.LogInfo($"Applying patches in {tuple.Item1.DeclaringType.Name}.{tuple.Item1.Name}");
                tuple.Item1.Invoke(null, null);
            }
        }
    }
}
