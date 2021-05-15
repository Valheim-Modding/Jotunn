using Jotunn.Managers;
using Jotunn.Entities;

namespace Jotunn.ConsoleCommands
{
    internal class HelpCommand : ConsoleCommand
    {
        public override string Name => "help";

        public override string Help => "Shows this menu";

        public override void Run(string[] args)
        {
            Console.instance.Print("Available custom commands:");

            foreach (ConsoleCommand cmd in CommandManager.Instance.CustomCommands)
            {
                Console.instance.Print(cmd.Name + " - " + cmd.Help);
            }
        }
    }
}
