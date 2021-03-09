using UnityEngine;
using JotunnLib;

namespace TestMod.ConsoleCommands
{
    public class TpCommand : ConsoleCommand
    {
        public override string Name => "tp";

        public override string Help => "Teleport to player";

        public override void Run(string[] args)
        {
            if (args.Length < 1)
            {
                Console.instance.Print("Usage: tp <name>");
                return;
            }

            string name = string.Join(" ", args);

            foreach (Player player in Player.GetAllPlayers())
            {
                if (player.GetPlayerName() == name)
                {
                    Player.m_localPlayer.TeleportTo(player.transform.position, player.transform.rotation, true);
                    Console.instance.Print("Teleported to: " + player);
                    return;
                }
        }

            Console.instance.Print("Could not find player: " + name);
        }
    }
}
