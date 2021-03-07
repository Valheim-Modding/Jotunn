using UnityEngine;
using UnityEngine.SceneManagement;
using BepInEx;
using HarmonyLib;
using ValheimLokiLoader.ConsoleCommands;
using ValheimLokiLoader.Managers;

namespace ValheimLokiLoader
{
    [BepInPlugin("com.bepinex.plugins.loki-loader", "Loki Loader", "0.0.1")]
    internal class Loader : BaseUnityPlugin
    {
        void Awake()
        {
            Harmony harmony = new Harmony("loki-loader");
            harmony.PatchAll();

            PrefabManager.Init();
            PieceManager.Init();

            initCommands();

            Debug.Log("Loki Loader loaded successfully");
        }

        void initCommands()
        {
            CommandManager.AddConsoleCommand(new HelpCommand());
            CommandManager.AddConsoleCommand(new ClearCommand());
        }
    }
}
