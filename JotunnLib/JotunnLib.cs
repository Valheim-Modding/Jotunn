﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using BepInEx;
using JotunnLib.ConsoleCommands;
using JotunnLib.Managers;
using JotunnLib.Utils;

namespace JotunnLib
{
    [BepInPlugin(ModGuid, "JotunnLib", Version)]
    public class JotunnLib : BaseUnityPlugin
    {
        // Version
        public const string Version = "0.2.0";
        public const string ModGuid = "com.bepinex.plugins.jotunnlib";

        public static BepInEx.Logging.ManualLogSource Logger;

        // Load order for managers
        private readonly List<Type> managerTypes = new List<Type>()
        {
            typeof(LocalizationManager),
            typeof(EventManager),
            typeof(CommandManager),
            typeof(InputManager),
            typeof(SkillManager),
            typeof(PrefabManager),
            typeof(PieceManager),
            typeof(ObjectManager),
            typeof(ZoneManager),
        };

        private readonly List<Manager> managers = new List<Manager>();

        internal static GameObject RootObject;

        private void Awake()
        {
            Logger = base.Logger;
            // Initialize the patches
            InitializePatches();

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

            initCommands();

            Logger.LogInfo("JotunnLib v" + Version + " loaded successfully");
        }

        private void InitializePatches()
        {
            // Reflect through everything

            List<Tuple<MethodInfo, int>> types = new List<Tuple<MethodInfo, int>>();

            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in asm.GetTypes())
                {
                    foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public).Where(x => x.GetCustomAttributes(typeof(PatchInitAttribute), false).Length == 1))
                    {
                        var attribute = method.GetCustomAttributes(typeof(PatchInitAttribute), false).FirstOrDefault() as PatchInitAttribute;
                        types.Add(new Tuple<MethodInfo, int>(method, attribute.Priority));
                    }
                }
            }

            // Invoke the method
            foreach (Tuple<MethodInfo, int> entry in types.OrderBy(x => x.Item2))
            {
                Debug.Log($"Applying patches in {entry.Item1.DeclaringType.Name}.{entry.Item1.Name}");
                entry.Item1.Invoke(null, null);
            }
        }

        private void OnGUI()
        {
            // Display version in main menu
            if (SceneManager.GetActiveScene().name == "start")
            {
                GUI.Label(new Rect(Screen.width - 100, 5, 100, 25), "JotunnLib v" + Version);
            }
        }

        private void initCommands()
        {
            CommandManager.Instance.RegisterConsoleCommand(new HelpCommand());
            CommandManager.Instance.RegisterConsoleCommand(new ClearCommand());
        }
    }
}
