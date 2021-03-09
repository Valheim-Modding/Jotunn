using System.Collections.Generic;
using JotunnLib.Utils;

namespace JotunnLib.ConsoleCommands
{
    class ClearCommand : ConsoleCommand
    {
        public override string Name => "clear";

        public override string Help => "Clears the console";

        public override void Run(string[] args)
        {
            ReflectionUtils.GetPrivateField<List<string>>(Console.instance, "m_chatBuffer").Clear();
            Console.instance.m_output.text = "";
        }
    }
}
