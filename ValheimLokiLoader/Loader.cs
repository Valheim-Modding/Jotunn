using UnityEngine;
using BepInEx;
using HarmonyLib;
using ValheimLokiLoader.ConsoleCommands;
using ValheimLokiLoader.Managers;

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
            initializeTests();

            Debug.Log("Loki Loader loaded successfully");
        }

        void initializeCommands()
        {
            CommandManager.AddConsoleCommand(new HelpCommand());
            CommandManager.AddConsoleCommand(new TestCommand());
            CommandManager.AddConsoleCommand(new ClearCommand());
            CommandManager.AddConsoleCommand(new TpCommand());
            CommandManager.AddConsoleCommand(new ListPlayersCommand());
            CommandManager.AddConsoleCommand(new SkinColorCommand());
            CommandManager.AddConsoleCommand(new RaiseSkillCommand());
            CommandManager.AddConsoleCommand(new PieceTestCommand());
        }

        void initializeTests()
        {
            // Test adding a skill
            // SkillManager.AddSkill("dank", "dank meme 420 test");
        }
    }
}
