using System.Collections.Generic;
using System.Linq;
using Jotunn.Entities;
using Jotunn.Managers;

namespace TestMod.ConsoleCommands
{
    public class RemoveCategoryTabCommand : ConsoleCommand
    {
        public override string Name => "remove_cat";

        public override string Help => "Remove a category tab on the fly";

        public override void Run(string[] args)
        {
            if (args.Length < 2)
            {
                return;
            }

#pragma warning disable CS0618 // Type or member is obsolete
            PieceManager.Instance.RemovePieceCategory(args[0], args[1]);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        public override List<string> CommandOptionList()
        {
            return PieceManager.Instance.GetPieceTables().Select(x => x.name).ToList();
        }
    }
}
