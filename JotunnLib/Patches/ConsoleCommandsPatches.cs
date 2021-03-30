using System;
using System.Text.RegularExpressions;
using JotunnLib.Managers;
using JotunnLib.Entities;
using JotunnLib.Utils;
using Steamworks;

namespace JotunnLib.Patches
{
    internal class ConsoleCommandsPatches : PatchInitializer
    {
        internal override void Init()
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

            ConsoleCommand cmd = CommandManager.Instance.ConsoleCommands.Find(c => c.Name == parts[0]);

            // If we found a command, execute it
            if (cmd != null)
            {
                /*

                TODO: Make arguments split take quotes into account

                string argsStr = "";

                if (text.Contains(' '))
                {
                    argsStr = text.Substring(text.IndexOf(' '), text.Length);
                }

                string[] args = Regex.Matches(argsStr, @",(?=([^\"]*\"[^\"]*\")*[^\"]*$)")
                    .Cast<Match>()
                    .Select(m => m.Value)
                    .ToArray();
                */

                string[] args = new string[parts.Length - 1];
                Array.Copy(parts, 1, args, 0, parts.Length - 1);
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
                    self.Print("Cannot use this command without cheats enabled. Use 'imacheater' to enable cheats");
                }

                return;
            }

            // Display error otherwise
            self.Print("Invalid command");
        }
    }
}
