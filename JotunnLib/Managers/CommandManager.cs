using System.Collections.Generic;
using System.Collections.ObjectModel;
using JotunnLib.Entities;
using JotunnLib.ConsoleCommands;

namespace JotunnLib.Managers
{
    /// <summary>
    ///     Handles loading of all custom console and chat commands.
    /// </summary>
    public class CommandManager : IManager
    {
        private static CommandManager _instance;
        /// <summary>
        ///     The singleton instance of this manager.
        /// </summary>
        public static CommandManager Instance { 
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

            _consoleCommands.Add(cmd);
        }
    }
}
