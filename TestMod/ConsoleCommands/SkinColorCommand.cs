using UnityEngine;
using JotunnLib;

namespace TestMod.ConsoleCommands
{
    public class SkinColorCommand : ConsoleCommand
    {
        public override string Name => "skin_color";

        public override string Help => "Sets player skin color";

        public override void Run(string[] args)
        {
            if (args.Length != 3)
            {
                Console.instance.Print("Usage: skin_color <r> <g> <b>");
                return;
            }

            float r = float.Parse(args[0]);
            float g = float.Parse(args[1]);
            float b = float.Parse(args[2]);
            Vector3 color = new Vector3(r, g, b);
            Player.m_localPlayer.SetSkinColor(color);

            Console.instance.Print("Set skin color");
        }
    }
}
