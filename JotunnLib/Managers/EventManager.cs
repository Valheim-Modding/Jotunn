using System;
using JotunnLib.Events;
using UnityEngine;

namespace JotunnLib.Managers
{
    /// <summary>
    /// Handles all logic to do with hooking into the game's events.
    /// </summary>
    public class EventManager : IManager
    {
        private static EventManager _instance;
        internal static EventManager Instance
        {
            get
            {

                if (_instance == null) _instance = new EventManager();
                return _instance;
            }
        }

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

        public void Init()
        {
            
        }

        // To be implemented
        static event EventHandler PlayerDied;
        static event EventHandler PlayerSentChat;
        static event EventHandler<PlayerEventArgs> PlayerConnected;
        static event EventHandler<PlayerEventArgs> PlayerDisconnected;
    }
}
