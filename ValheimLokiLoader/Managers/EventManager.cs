using System;
using ValheimLokiLoader.Events;

namespace ValheimLokiLoader.Managers
{
    public static class EventManager
    {
        public static event EventHandler<PlayerEventArgs> PlayerSpawned;

        internal static void OnPlayerSpawned(Player player)
        {
            PlayerSpawned?.Invoke(player, new PlayerEventArgs() { Player = player });
        }

        // To be implemented
        public static event EventHandler PlayerDied;
        public static event EventHandler PlayerSentChat;
        public static event EventHandler<PlayerEventArgs> PlayerConnected;
        public static event EventHandler<PlayerEventArgs> PlayerDisconnected;
    }
}
