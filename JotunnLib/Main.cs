using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using BepInEx;
using Jotunn.Managers;
using Jotunn.Utils;

namespace Jotunn
{
    /// <summary>
    ///     Main class implementing BaseUnityPlugin.
    /// </summary>
    [BepInPlugin(ModGuid, ModName, Version)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class Main : BaseUnityPlugin
    {
        /// <summary>
        ///     The current version of the Jotunn library.
        /// </summary>
        public const string Version = "2.0.5";

        /// <summary>
        ///     The name of the library.
        /// </summary>
        public const string ModName = "Jotunn";

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

            // Create and initialize all managers
            RootObject = new GameObject("_JotunnRoot");
            GameObject.DontDestroyOnLoad(RootObject);

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
                GUIManager.Instance,
                SaveManager.Instance,
                SynchronizationManager.Instance
            };
            foreach (IManager manager in managers)
            {
                manager.Init();
                Logger.LogInfo("Initialized " + manager.GetType().Name);
            }

            Logger.LogInfo("Jotunn v" + Version + " loaded successfully");
        }


        /// <summary>
        ///     Initialize patches
        /// </summary>
        private void Start()
        {
            InitializePatches();
        }

        private void Update()
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
                GUI.Label(new Rect(Screen.width - 100, 5, 100, 25), "Jotunn v" + Version);
            }

            // Fake MonoBehaviour event for GUIManager
            GUIManager.Instance.OnGUI();
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
                Logger.LogInfo($"Applying patches in {tuple.Item1.DeclaringType.Name}.{tuple.Item1.Name}");
                tuple.Item1.Invoke(null, null);
            }
        }
    }
}
