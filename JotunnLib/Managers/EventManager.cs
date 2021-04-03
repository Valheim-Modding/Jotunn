using System;
using UnityEngine;
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

        public EventManager()
        {
            if (Instance != null)
            {
                Logger.LogError("Error, two instances of singleton: " + this.GetType().Name);
                return;
            }

            Instance = this;
        }

        internal override void Clear()
        {
            Instance = null;
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
