using UnityEngine;
using JotunnLib.Entities;

namespace TestMod.ConsoleCommands
{
    public class PrintItemsCommand : ConsoleCommand
    {
        public override string Name => "print_items";

        public override string Help => "Prints all existing items";

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
