using System;
using JotunnLib.Events;

namespace JotunnLib.Managers
{
    /// <summary>
    /// Handles all logic to do with hooking into the game's events.
    /// </summary>
    public class EventManager : Manager
    {
        internal static EventManager Instance { get; private set; }

        public static event EventHandler<PlayerEventArgs> PlayerSpawned;
        public static event EventHandler<PlayerPlacedPieceEventArgs> PlayerPlacedPiece;

        private void Awake()
        {
            if (Instance != null)
            {
                Logger.LogError($"Cannot have multiple instances of singleton: {GetType().Name}");
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
