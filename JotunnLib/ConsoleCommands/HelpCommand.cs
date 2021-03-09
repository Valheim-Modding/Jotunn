using JotunnLib.Managers;

namespace JotunnLib.ConsoleCommands
{
    class HelpCommand : ConsoleCommand
    {
        public override string Name => "help";

        public override string Help => "Shows this menu";

        public override void Run(string[] args)
        {
            Console.instance.Print("Available commands:");

            foreach (ConsoleCommand cmd in CommandManager.Instance.ConsoleCommands)
            {
                Console.instance.Print(cmd.Name + " - " + cmd.Help);
            }
        }
    }
}
