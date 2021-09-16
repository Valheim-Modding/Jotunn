using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Jotunn.Entities;
using Jotunn.ConsoleCommands;
using System.Text.RegularExpressions;
using System.Linq;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using Mono.Cecil;

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
        public static CommandManager Instance
        {
            get
            {
                if (_instance == null) _instance = new CommandManager();
                return _instance;
            }
        }

        /// <summary>
        ///     The console commands that are built-in to Valheim. These cannot be changed or overriden, and
        ///     no other commands can be declared with the same names as these.
        /// </summary>
        public static ReadOnlyCollection<string> VanillaCommands => _vanillaCommands.AsReadOnly();

        private static List<string> _vanillaCommands = new List<string>();

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
            AddConsoleCommand(new HelpCommand());
            AddConsoleCommand(new ClearCommand());

            IL.Terminal.InputText += GatherVanillaCommands;

            On.Terminal.InputText += HandleCustomCommands;
        }

        /// <summary>
        ///     Adds a new console command to Valheim.
        /// </summary>
        /// <param name="cmd">The console command to add</param>
        public void AddConsoleCommand(ConsoleCommand cmd)
        {
            // Cannot override vanilla commands
            if (_vanillaCommands.Contains(cmd.Name))
            {
                Logger.LogError($"Cannot override vanilla command: {cmd.Name}");
                return;
            }

            // Cannot have two commands with same name
            if (_customCommands.Exists(c => c.Name == cmd.Name))
            {
                Logger.LogError($"Cannot have two console commands with same name: {cmd.Name}");
                return;
            }

            // Cannot have command with space in it
            if (cmd.Name.Contains(" "))
            {
                Logger.LogError($"Cannot have command containing space: '{cmd.Name}'");
                return;
            }

            _customCommands.Add(cmd);
        }

        private static void GatherVanillaCommands(ILContext il)
        {
            var c = new ILCursor(il);

            foreach (var i in c.Instrs)
            {
                if (i.OpCode == OpCodes.Ldstr)
                {
                    var str = (string)i.Operand;

                    bool IsACmd(Instruction i)
                    {
                        if (i.Next.OpCode == OpCodes.Call || i.Next.OpCode == OpCodes.Callvirt)
                        {
                            var methodReference = (MethodReference)i.Next.Operand;
                            var methodName = methodReference.Name;
                            if (methodName == nameof(string.StartsWith) ||
                                methodName == "op_Equality")
                            {
                                return true;
                            }
                        }

                        return false;
                    }

                    if (IsACmd(i))
                    {
                        _vanillaCommands.Add(str.Trim());
                    }
                }
            }
        }

        private void HandleCustomCommands(On.Terminal.orig_InputText orig, Terminal self)
        {
            orig(self);

            string text = self.m_input.text;
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            string[] parts = text.Split(' ');

            if (parts.Length == 0)
            {
                return;
            }

            ConsoleCommand cmd = CommandManager.Instance.CustomCommands.FirstOrDefault(c => c.Name.Equals(parts[0], StringComparison.InvariantCultureIgnoreCase));

            // If we found a command, execute it
            if (cmd != null)
            {
                // Prioritizing quoted strings, then all strings of non-white chars 
                string[] args = Regex.Matches(text, @"""[^""]+""|\S+")
                    .Cast<Match>()
                    // get rid of the quotes around arguments
                    .Select(x => x.Value.Trim('"'))
                    // we don't need the command itself here
                    .Skip(1)
                    .ToArray();

                cmd.Run(args);
            }
        }
    }
}
