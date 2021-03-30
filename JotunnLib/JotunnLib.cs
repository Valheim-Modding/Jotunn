﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using BepInEx;
using JotunnLib.ConsoleCommands;
using JotunnLib.Managers;
using JotunnLib.Utils;

namespace JotunnLib
{
    [BepInPlugin("com.bepinex.plugins.jotunnlib", "JotunnLib", Version)]
    internal class JotunnLib : BaseUnityPlugin
    {
        // Version
        public const string Version = "0.2.0";

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
            // Initialize the patches
            PatchInitializer.InitializePatches();

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
                Debug.Log("Initialized " + manager.GetType().Name);
            }

            initCommands();

            Debug.Log("JotunnLib v" + Version + " loaded successfully");
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
