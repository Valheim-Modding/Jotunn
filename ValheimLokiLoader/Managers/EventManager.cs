using System;
using UnityEngine;
using ValheimLokiLoader.Events;

namespace ValheimLokiLoader.Managers
{
    public static class EventManager
    {
        public static event EventHandler<PlayerEventArgs> PlayerSpawned;
        public static event EventHandler<PlayerPlacedPieceEventArgs> PlayerPlacedPiece;

        internal static void OnPlayerSpawned(Player player)
        {
            PlayerSpawned?.Invoke(player, new PlayerEventArgs() { Player = player });
        }

        internal static void OnPlayerPlacedPiece(PlayerPlacedPieceEventArgs args)
        {
            PlayerPlacedPiece?.Invoke(args.Player, args);
        }

        // To be implemented
        static event EventHandler PlayerDied;
        static event EventHandler PlayerSentChat;
        static event EventHandler<PlayerEventArgs> PlayerConnected;
        static event EventHandler<PlayerEventArgs> PlayerDisconnected;
    }
}
