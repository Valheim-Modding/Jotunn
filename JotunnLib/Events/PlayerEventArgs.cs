using System;

namespace JotunnLib.Events
{
    /// <summary>
    /// Event args that involve a player.
    /// </summary>
    public class PlayerEventArgs : EventArgs
    {
        /// <summary>
        /// The player that triggered the event
        /// </summary>
        public Player Player { get; set; }
    }
}
