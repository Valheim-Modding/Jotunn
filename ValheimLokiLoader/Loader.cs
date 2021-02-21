using UnityEngine;
using BepInEx;
using HarmonyLib;
using ValheimLokiLoader.ConsoleCommands;

namespace ValheimLokiLoader
{
    [BepInPlugin("com.bepinex.plugins.loki-loader", "Loki Loader", "0.0.1")]
    class Loader : BaseUnityPlugin
    {
        void Awake()
        {
            Harmony harmony = new Harmony("loki-loader");
            harmony.PatchAll();

            initializeCommands();

            Debug.Log("Loki Loader loaded successfully");
        }

        void initializeCommands()
        {
            ConsoleCommand.Add(new HelpCommand());
            ConsoleCommand.Add(new TestCommand());
            ConsoleCommand.Add(new ClearCommand());
            ConsoleCommand.Add(new AddSkillCommand());
        }
    }
}
