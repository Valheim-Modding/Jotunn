using System.Collections.Generic;
using System.Collections.ObjectModel;
using JotunnLib.Entities;
using JotunnLib.ConsoleCommands;

namespace JotunnLib.Managers
{
    /// <summary>
    ///     Handles loading of all custom console and chat commands.
    /// </summary>
    public class CommandManager : Manager
    {
        /// <summary>
        ///     The singleton instance of this manager.
        /// </summary>
        public static CommandManager Instance { get; private set; }

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

        private void Awake()
        {
            if (Instance != null)
            {
                Logger.LogError($"Cannot have multiple instances of singleton: {GetType()}");
                return;
            }
            Instance = this;
        }

        /// <summary>
        ///     Initialize console commands that come with Jotunn.
        /// </summary>
        internal override void Init()
        {
            RegisterConsoleCommand(new HelpCommand());
            RegisterConsoleCommand(new ClearCommand());
        }

        /// <summary>
        ///     Adds a new console command to Valheim.
        /// </summary>
        /// <param name="cmd">The console command to add</param>
        public void RegisterConsoleCommand(ConsoleCommand cmd)
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
