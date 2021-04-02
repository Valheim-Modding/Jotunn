using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using JotunnLib.Entities;

namespace JotunnLib.Managers
{
    public class CommandManager : Manager
    {
        public static CommandManager Instance { get; private set; }

        public static ReadOnlyCollection<string> DefaultConsoleCommands => _defaultConsoleCommands.AsReadOnly();

        private static List<string> _defaultConsoleCommands = new List<string>()
        {
            // "help" command not included since we want to overwrite it
            
            // Basic commands
            "kick", "ban", "unban", "banned", "ping", "lodbias", "info", "devcommands"
        };

        public static ReadOnlyCollection<string> DefaultCheatConsoleCommands => _defaultCheatConsoleCommands.AsReadOnly();

        private static readonly List<string> _defaultCheatConsoleCommands = new List<string>()
        {
            "genloc", "debugmode", "spawn", "pos", "goto", "exploremap", "resetmap", "killall", "tame",
            "hair", "beard", "location", "raiseskill", "resetskill", "freefly", "ffsmooth", "tod",
            "env", "resetenv", "wind", "god", "event", "stopevent", "randomevent", "save", "tameall",
            "resetcharacter", "removedrops", "setkey", "resetkeys", "listkeys", "players", "dpsdebug"
        };

        public ReadOnlyCollection<ConsoleCommand> ConsoleCommands => _consoleCommands.AsReadOnly();

        private List<ConsoleCommand> _consoleCommands = new List<ConsoleCommand>();

        private void Awake()
        {
            if (Instance != null)
            {
                Logger.LogError("Error, two instances of singleton: " + this.GetType().Name);
                return;
            }

            Instance = this;
        }

        public void RegisterConsoleCommand(ConsoleCommand cmd)
        {
            // Cannot override default command
            if (_defaultConsoleCommands.Contains(cmd.Name))
            {
                Logger.LogError("Cannot override default command: " + cmd.Name);
                return;
            }

            // Cannot have two commands with same name
            if (_consoleCommands.Exists(c => c.Name == cmd.Name))
            {
                Logger.LogError("Cannot have two console commands with same name: " + cmd.Name);
                return;
            }

            _consoleCommands.Add(cmd);
        }
    }
}
