using UnityEngine;
using JotunnLib;

namespace TestMod.ConsoleCommands
{
    public class ListPlayersCommand : ConsoleCommand
    {
        public override string Name => "list_players";

        public override string Help => "Lists all online players";

        public override void Run(string[] args)
        {
            Console.instance.Print("All players:");

            foreach (Player player in Player.GetAllPlayers())
            {
                Console.instance.Print(player.GetPlayerName());
            }
        }
    }
}
