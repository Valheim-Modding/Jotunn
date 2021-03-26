using System.Collections.Generic;
using UnityEngine;
using JotunnLib.Entities;

namespace JotunnLib.Managers
{
    public class CommandManager : Manager
    {
        public static CommandManager Instance { get; private set; }

        // TODO: Make these lists immutable
        public static readonly List<string> DefaultConsoleCommands = new List<string>()
        {
            // "help" command not included since we want to overwrite it
            
            // Basic commands
            "kick", "ban", "unban", "banned", "ping", "lodbias", "info", "devcommands"
        };
        public static readonly List<string> DefaultCheatConsoleCommands = new List<string>()
        {
            "genloc", "debugmode", "spawn", "pos", "goto", "exploremap", "resetmap", "killall", "tame",
            "hair", "beard", "location", "raiseskill", "resetskill", "freefly", "ffsmooth", "tod",
            "env", "resetenv", "wind", "god", "event", "stopevent", "randomevent", "save", "tameall",
            "resetcharacter", "removedrops", "setkey", "resetkeys", "listkeys", "players", "dpsdebug"
        };

        internal List<ConsoleCommand> ConsoleCommands = new List<ConsoleCommand>();

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("Error, two instances of singleton: " + this.GetType().Name);
                return;
            }

            Instance = this;
        }

        public void RegisterConsoleCommand(ConsoleCommand cmd)
        {
            // Cannot override default command
            if (DefaultConsoleCommands.Contains(cmd.Name))
            {
                Debug.LogError("Cannot override default command: " + cmd.Name);
                return;
            }

            // Cannot have two commands with same name
            if (ConsoleCommands.Exists(c => c.Name == cmd.Name))
            {
                Debug.LogError("Cannot have two console commands with same name: " + cmd.Name);
                return;
            }

            ConsoleCommands.Add(cmd);
        }
    }
}
