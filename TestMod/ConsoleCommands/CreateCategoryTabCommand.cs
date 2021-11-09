using Jotunn.Entities;
using Jotunn.Managers;

namespace TestMod.ConsoleCommands
{
    public class CreateCategoryTabCommand : ConsoleCommand
    {
        public override string Name => "create_cat";

        public override string Help => "Create a new category tab on the fly";

        public override void Run(string[] args)
        {
            if (args.Length < 2)
            {
                return;
            }

            PieceManager.Instance.AddPieceCategory(args[0], args[1]);
        }
    }
}
