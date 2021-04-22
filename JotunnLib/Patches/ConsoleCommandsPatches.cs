using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx.Logging;
using Jotunn.Managers;
using Jotunn.Entities;
using Jotunn.Utils;
using Steamworks;
using Debug = UnityEngine.Debug;

namespace Jotunn.Patches
{
    internal class ConsoleCommandsPatches 
    {
        [PatchInit(0)]
        public static void Init()
        {
            On.Console.InputText += Console_InputText;
        }

        private static void Console_InputText(On.Console.orig_InputText orig, Console self)
        {
            orig(self);

            string text = self.m_input.text;
            string[] parts = text.Split(' ');

            if (string.IsNullOrEmpty(text) && parts.Length == 0)
            {
                self.Print("Invalid command");
                return;
            }

            ConsoleCommand cmd = CommandManager.Instance.ConsoleCommands.FirstOrDefault(c => c.Name == parts[0]);

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
