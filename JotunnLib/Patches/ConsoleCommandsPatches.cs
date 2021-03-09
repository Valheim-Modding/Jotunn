using System;
using System.Text.RegularExpressions;
using HarmonyLib;
using JotunnLib.Managers;

namespace JotunnLib.Patches
{
    internal class ConsoleCommandsPatches
    {
        [HarmonyPatch(typeof(Console), "InputText")]
        public static class ConsoleInputTextPatch
        {
            public static void Postfix(ref Console __instance)
            {
                string text = __instance.m_input.text;
                string[] parts = text.Split(' ');

                if (string.IsNullOrEmpty(text) && parts.Length == 0)
                {
                    __instance.Print("Invalid command");
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
                        __instance.Print("Cannot use this command without cheats enabled. Use 'imacheater' to enable cheats");
                    }

                    return;
                }

                // Display error otherwise
                __instance.Print("Invalid command");
            }
        }
    }
}
