using System;
using System.Collections.Generic;
using UnityEngine;
using BepInEx;
using HarmonyLib;
using JotunnLib.ConsoleCommands;
using JotunnLib.Managers;

namespace JotunnLib
{
    [BepInPlugin("com.bepinex.plugins.jotunnlib", "JotunnLib", "0.0.1")]
    internal class JotunnLib : BaseUnityPlugin
    {
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

        private void Awake()
        {
            // Initialize harmony patches
            Harmony harmony = new Harmony("jotunnlib");
            harmony.PatchAll();

            // Create and initialize all managers
            GameObject root = new GameObject("_JotunnLibRoot");

            foreach (Type managerType in managerTypes)
            {
                managers.Add((Manager)root.AddComponent(managerType));
            }

            foreach (Manager manager in managers)
            {
                manager.Init();
            }

            initCommands();

            Debug.Log("JotunnLib loaded successfully");
        }

        private void initCommands()
        {
            CommandManager.Instance.RegisterConsoleCommand(new HelpCommand());
            CommandManager.Instance.RegisterConsoleCommand(new ClearCommand());
        }
    }
}
