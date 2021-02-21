using UnityEngine;

namespace ValheimLokiLoader.ConsoleCommands
{
    public class TpCommand : ConsoleCommand
    {
        public override string Name => "tp";

        public override string Help => "teleport to player";

        public override void Run(string[] args)
        {
            if (args.Length != 1)
            {
                Console.instance.Print("Usage: tp <name>");
                return;
            }

            string name = args[0];

            if (name == "?")
            {
                Console.instance.Print("All players:");
            }

            foreach (Player player in Player.GetAllPlayers())
            {
                if (name == "?")
                {
                    Console.instance.Print(player.GetPlayerName());
                    return;
                }
                else
                {
                    if (player.GetPlayerName() == name)
                    {
                        Player.m_localPlayer.TeleportTo(player.transform.position, player.transform.rotation, true);
                        Console.instance.Print("Teleported to: " + player);
                        return;
                    }
                }
            }

            Console.instance.Print("Could not find player: " + name);
        }
    }
}
