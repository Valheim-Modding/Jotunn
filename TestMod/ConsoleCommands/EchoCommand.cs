using Jotunn.Entities;

namespace TestMod.ConsoleCommands
{
    public class EchoCommand : ConsoleCommand
    {
        public override string Name => "echo";

        public override string Help => "Echoes all text entered to the console or chat";

        public override void Run(string[] args, Terminal context)
        {
            if (args.Length < 1)
            {
                context.AddString("Usage: echo <text>");
            }

            context.AddString(string.Join(" ", args, 0, args.Length));
        }
    }
}
