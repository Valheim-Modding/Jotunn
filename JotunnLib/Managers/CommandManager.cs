using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using HarmonyLib;
using Jotunn.ConsoleCommands;
using Jotunn.Entities;

namespace Jotunn.Managers
{
    /// <summary>
    ///     Manager for handling custom console and chat commands.
    /// </summary>
    public class CommandManager : IManager
    {
        private static CommandManager _instance;
        /// <summary>
        ///     The singleton instance of this manager.
        /// </summary>
        public static CommandManager Instance => _instance ??= new CommandManager();

        /// <summary>
        ///     Hide .ctor
        /// </summary>
        private CommandManager() {}

        /// <summary>
        ///     Internal Action delegate to add custom entities into vanilla command's option list
        /// </summary>
        internal static Action<string, List<string>> OnGetTabOptions;

        /// <summary>
        ///     A list of all the custom console commands that have been added to the game through this manager,
        ///     either by Jotunn or by mods using Jotunn.
        /// </summary>
        public ReadOnlyCollection<ConsoleCommand> CustomCommands => _customCommands.AsReadOnly();

        private List<ConsoleCommand> _customCommands = new List<ConsoleCommand>();

        /// <summary>
        ///     Initialize console commands that come with Jotunn.
        /// </summary>
        public void Init()
        {
            AddConsoleCommand(new ClearCommand());

            Main.Harmony.PatchAll(typeof(Patches));
        }

        private static class Patches
        {
            [HarmonyPatch(typeof(Console), nameof(Console.Awake)), HarmonyPostfix]
            private static void AddCustomCommands(Console __instance) => Instance.AddCustomCommands(__instance);

            [HarmonyPatch(typeof(Terminal.ConsoleCommand), nameof(Terminal.ConsoleCommand.GetTabOptions)), HarmonyPostfix]
            private static void ConsoleCommand_GetTabOptions(Terminal.ConsoleCommand __instance, ref List<string> __result) => Instance.ConsoleCommand_GetTabOptions(__instance, ref __result);
        }

        /// <summary>
        ///     Adds a new console command to Valheim.
        /// </summary>
        /// <param name="cmd">The console command to add</param>
        public void AddConsoleCommand(ConsoleCommand cmd)
        {
            // Cannot have two commands with same name
            if (_customCommands.Exists(c => c.Name == cmd.Name))
            {
                Logger.LogWarning($"Cannot have two console commands with same name: {cmd.Name}");
                return;
            }

            // Cannot have command with space in it
            if (cmd.Name.Contains(" "))
            {
                Logger.LogWarning($"Cannot have command containing space: '{cmd.Name}'");
                return;
            }

            _customCommands.Add(cmd);
        }

        private void AddCustomCommands(Console self)
        {
            if (_customCommands.Any())
            {
                Logger.LogInfo($"Adding {_customCommands.Count} commands to the Console");

                foreach (var cmd in _customCommands)
                {
                    // Cannot override vanilla commands
                    if (self.m_commandList.Contains(cmd.Name))
                    {
                        Logger.LogWarning($"Cannot override existing command: {cmd.Name}");
                        return;
                    }

                    // Add to the vanilla system
                    new Terminal.ConsoleCommand(cmd.Name, cmd.Help, args =>
                        {
                            cmd.Run(args.Args.Skip(1).ToArray());
                        },
                        isCheat: cmd.IsCheat, isNetwork: cmd.IsNetwork, onlyServer: cmd.OnlyServer,
                        isSecret: cmd.IsSecret, optionsFetcher: cmd.CommandOptionList);
                }

                self.updateCommandList();
            }
        }

        /// <summary>
        ///     Fire <see cref="OnGetTabOptions"/> for any ConsoleCommand when its tabOptions member
        ///     is first populated to add Jötunn entities to the option list
        /// </summary>
        /// <param name="self"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private void ConsoleCommand_GetTabOptions(Terminal.ConsoleCommand self, ref List<string> result)
        {
            if (self.m_tabOptions == null && self.m_tabOptionsFetcher != null)
            {
                OnGetTabOptions?.SafeInvoke(self.Command, result);
            }
        }
    }
}
