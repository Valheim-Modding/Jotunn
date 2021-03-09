using System;
using UnityEngine;
using ValheimLokiLoader.Events;

namespace ValheimLokiLoader.Managers
{
    public class EventManager : Manager
    {
        public static EventManager Instance { get; private set; }

        public static event EventHandler<PlayerEventArgs> PlayerSpawned;
        public static event EventHandler<PlayerPlacedPieceEventArgs> PlayerPlacedPiece;

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("Error, two instances of singleton: " + this.GetType().Name);
                return;
            }

            Instance = this;
        }

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
