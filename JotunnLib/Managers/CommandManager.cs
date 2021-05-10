using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Jotunn.Entities;
using Jotunn.ConsoleCommands;
using System.Text.RegularExpressions;
using System.Linq;

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
        public static ReadOnlyCollection<string> DefaultConsoleCommands => _defaultConsoleCommands.AsReadOnly();

        /// <summary>
        ///     The "dev" console commands that are built-in to Valheim. These cannot be changed or overriden, and
        ///     no other commands can be declared with the same names as these.
        /// </summary>
        public static ReadOnlyCollection<string> DefaultCheatConsoleCommands => _defaultCheatConsoleCommands.AsReadOnly();

        private static List<string> _defaultConsoleCommands = new List<string>()
        {
            // "help" command not included since we want to overwrite it
            
            // Basic (non-dev) commands
            "kick", "ban", "unban", "banned", "ping", "lodbias", "info", "devcommands"
        };
        private static readonly List<string> _defaultCheatConsoleCommands = new List<string>()
        {
            "genloc", "debugmode", "spawn", "pos", "goto", "exploremap", "resetmap", "killall", "tame",
            "hair", "beard", "location", "raiseskill", "resetskill", "freefly", "ffsmooth", "tod",
            "env", "resetenv", "wind", "god", "event", "stopevent", "randomevent", "save", "tameall",
            "resetcharacter", "removedrops", "setkey", "resetkeys", "listkeys", "players", "dpsdebug"
        };

        /// <summary>
        ///     A list of all the custom console commands that have been added to the game through this manager,
        ///     either by Jotunn or by mods using Jotunn.
        /// </summary>
        public ReadOnlyCollection<ConsoleCommand> ConsoleCommands => _consoleCommands.AsReadOnly();

        private List<ConsoleCommand> _consoleCommands = new List<ConsoleCommand>();

        /// <summary>
        ///     Initialize console commands that come with Jotunn.
        /// </summary>
        public void Init()
        {
            On.Console.InputText += Console_InputText;

            AddConsoleCommand(new HelpCommand());
            AddConsoleCommand(new ClearCommand());
        }

        /// <summary>
        ///     Adds a new console command to Valheim.
        /// </summary>
        /// <param name="cmd">The console command to add</param>
        public void AddConsoleCommand(ConsoleCommand cmd)
        {
            // Cannot override default command
            if (_defaultConsoleCommands.Contains(cmd.Name))
            {
                Logger.LogError($"Cannot override default command: {cmd.Name}");
                return;
            }

            // Cannot have two commands with same name
            if (_consoleCommands.Exists(c => c.Name == cmd.Name))
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

            _consoleCommands.Add(cmd);
        }

        private void Console_InputText(On.Console.orig_InputText orig, Console self)
        {
            orig(self);

            string text = self.m_input.text;
            string[] parts = text.Split(' ');

            if (string.IsNullOrEmpty(text) && parts.Length == 0)
            {
                self.Print("Invalid command");
                return;
            }

            ConsoleCommand cmd = CommandManager.Instance.ConsoleCommands.FirstOrDefault(c => c.Name.Equals(parts[0], StringComparison.InvariantCultureIgnoreCase));

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
                return;
            }

            // If a default command, don't display error
            if (CommandManager.DefaultConsoleCommands.Contains(parts[0]))
            {
                return;
            }

            // If a cheat command, check if cheats enabled
            if (CommandManager.DefaultCheatConsoleCommands.Contains(parts[0]))
            {
                if (!Console.instance.IsCheatsEnabled())
                {
                    self.Print("Cannot use this command without cheats enabled. Use 'devcommands' to enable cheats");
                }

                return;
            }

            // Display error otherwise
            self.Print("Invalid command");
        }
    }
}
