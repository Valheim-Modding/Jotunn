using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Jotunn.ConsoleCommands;
using Jotunn.Entities;

namespace Jotunn.Managers
{
    /// <summary>
    ///     Manager for handling custom console and chat commands.
    /// </summary>
    public class CommandManager : IManager
    {
        private static CommandManager _instance;
        /// <summary>
        ///     The singleton instance of this manager.
        /// </summary>
        public static CommandManager Instance
        {
            get
            {
                if (_instance == null) _instance = new CommandManager();
                return _instance;
            }
        }

        /// <summary>
        ///     A list of all the custom console commands that have been added to the game through this manager,
        ///     either by Jotunn or by mods using Jotunn.
        /// </summary>
        public ReadOnlyCollection<ConsoleCommand> CustomCommands => _customCommands.AsReadOnly();

        private List<ConsoleCommand> _customCommands = new List<ConsoleCommand>();

        /// <summary>
        ///     Initialize console commands that come with Jotunn.
        /// </summary>
        public void Init()
        {
            AddConsoleCommand(new ClearCommand());

            On.Console.Awake += AddCustomCommands;
            //On.Terminal.InputText += HandleCustomCommands;
        }

        /// <summary>
        ///     Adds a new console command to Valheim.
        /// </summary>
        /// <param name="cmd">The console command to add</param>
        public void AddConsoleCommand(ConsoleCommand cmd)
        {
            // Cannot have two commands with same name
            if (_customCommands.Exists(c => c.Name == cmd.Name))
            {
                Logger.LogWarning($"Cannot have two console commands with same name: {cmd.Name}");
                return;
            }

            // Cannot have command with space in it
            if (cmd.Name.Contains(" "))
            {
                Logger.LogWarning($"Cannot have command containing space: '{cmd.Name}'");
                return;
            }

            _customCommands.Add(cmd);
        }
        
        private void AddCustomCommands(On.Console.orig_Awake orig, Console self)
        {
            orig(self);

            if (_customCommands.Any())
            {
                Logger.LogInfo($"Adding {_customCommands.Count} commands to the Console");
                
                foreach (var cmd in _customCommands)
                {
                    // Cannot override vanilla commands
                    if (self.m_commandList.Contains(cmd.Name))
                    {
                        Logger.LogWarning($"Cannot override existing command: {cmd.Name}");
                        return;
                    }

                    // Add to the vanilla system
                    new Terminal.ConsoleCommand(cmd.Name, cmd.Help, args =>
                    {
                        cmd.Run(args.Args.Skip(1).ToArray());
                    });
                }

                self.updateCommandList();
            }
        }

        /*private void HandleCustomCommands(On.Terminal.orig_InputText orig, Terminal self)
        {
            orig(self);

            string text = self.m_input.text;
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            string[] parts = text.Split(' ');

            if (parts.Length == 0)
            {
                return;
            }

            ConsoleCommand cmd = CommandManager.Instance.CustomCommands.FirstOrDefault(c => c.Name.Equals(parts[0], StringComparison.InvariantCultureIgnoreCase));

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
            }
        }*/
    }
}
