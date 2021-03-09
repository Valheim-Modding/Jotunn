using UnityEngine;
using JotunnLib;

namespace TestMod.ConsoleCommands
{
    public class TestCommand : ConsoleCommand
    {
        public override string Name => "test_cmd";

        public override string Help => "Some testing command";

        public override void Run(string[] args)
        {
            Console.instance.Print("All items:");
            foreach (GameObject obj in ObjectDB.instance.m_items)
            {
                ItemDrop item = obj.GetComponent<ItemDrop>();
                Console.instance.Print(item.m_itemData.m_shared.m_name);
            }
        }
    }
}
