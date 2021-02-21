using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ValheimLokiLoader
{
    public static class CommandManager
    {
        public static List<ConsoleCommand> ConsoleCommands = new List<ConsoleCommand>();
        public static readonly List<string> DefaultConsoleCommands = new List<string>() {
            "kick", "ban", "unban", "banned", "ping", "lodbias", "info"
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
