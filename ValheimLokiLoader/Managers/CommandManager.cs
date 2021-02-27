using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ValheimLokiLoader.Managers
{
    public static class CommandManager
    {
        internal static List<ConsoleCommand> ConsoleCommands = new List<ConsoleCommand>();
        public static readonly List<string> DefaultConsoleCommands = new List<string>()
        {
            // "help" command not included since we want to overwrite it
            
            // Basic commands
            "kick", "ban", "unban", "banned", "ping", "lodbias", "info", "imacheater"
        };
        public static readonly List<string> DefaultCheatConsoleCommands = new List<string>()
        {
            "genloc", "debugmode", "spawn", "pos", "goto", "exploremap", "resetmap", "killall", "tame",
            "hair", "beard", "location", "raiseskill", "resetskill", "freefly", "ffsmooth", "tod",
            "env", "resetenv", "wind", "god", "event", "stopevent", "randomevent", "save",
            "resetcharacter", "removedrops", "setkey", "resetkeys", "listkeys", "players", "dpsdebug"
        };

        public static void AddConsoleCommand(ConsoleCommand cmd)
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
