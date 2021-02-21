using System;
using HarmonyLib;

namespace ValheimLokiLoader.Patches
{
    class ConsoleCommandsPatches
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

                ConsoleCommand cmd = CommandManager.ConsoleCommands.Find(c => c.Name == parts[0]);

                // If we found a command, execute it
                if (cmd != null)
                {
                    string[] args = new string[parts.Length - 1];
                    Array.Copy(parts, 1, args, 0, parts.Length - 1);
                    cmd.Run(args);
                    return;
                }

                // If no command, display error
                __instance.Print("Invalid command");
            }
        }
    }
}
