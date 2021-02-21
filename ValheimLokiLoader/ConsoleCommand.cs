using System.Collections.Generic;
using UnityEngine;

namespace ValheimLokiLoader
{
    public abstract class ConsoleCommand
    {
        public abstract string Name { get; }
        public abstract string Help { get; }
        public abstract void Run(string[] args);

        public static List<ConsoleCommand> Commands = new List<ConsoleCommand>();
        public static readonly List<string> DefaultCommands = new List<string>() {
            "kick", "ban", "unban", "banned", "ping", "lodbias", "info"
        };

        public static void Add(ConsoleCommand cmd)
        {
            // Cannot override default command
            if (DefaultCommands.Contains(cmd.Name))
            {
                Debug.LogError("Cannot override default command: " + cmd.Name);
                return;
            }

            // Cannot have two commands with same name
            if (Commands.Exists(c => c.Name == cmd.Name))
            {
                Debug.LogError("Cannot have two console commands with same name: " + cmd.Name);
                return;
            }

            Commands.Add(cmd);
        }
    }
}
