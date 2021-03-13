using System;
using UnityEngine;

namespace JotunnLib.Events
{
    /// <summary>
    /// Event args for when a player attempts to place a piece.
    /// </summary>
    public class PlayerPlacedPieceEventArgs : PlayerEventArgs
    {
        /// <summary>
        /// The piece the player tried to place
        /// </summary>
        public Piece Piece { get; set; }

        /// <summary>
        /// The position the piece was placed in
        /// </summary>
        public Vector3 Position { get; set; }

        /// <summary>
        /// The rotation the piece was placed woth
        /// </summary>
        public Quaternion Rotation { get; set; }

        /// <summary>
        /// Whether the piece was sucessfully placed or not. This may be unsuccessful due to player placing the
        /// piece in invalid locations, or not having enough resources to build it.
        /// </summary>
        public bool Success { get; set; }
    }
}
