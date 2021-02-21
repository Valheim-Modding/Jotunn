using System.Collections.Generic;

namespace ValheimLokiLoader.ConsoleCommands
{
    class ClearCommand : ConsoleCommand
    {
        public override string Name => "clear";

        public override string Help => "Clears the console";

        public override void Run(string[] args)
        {
            Util.GetPrivateField<List<string>>(Console.instance, "m_chatBuffer").Clear();
            Console.instance.m_output.text = "";
        }
    }
}
