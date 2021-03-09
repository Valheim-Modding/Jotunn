using System;
using UnityEngine;

namespace JotunnLib.Events
{
    public class PlayerPlacedPieceEventArgs : PlayerEventArgs
    {
        public Piece Piece { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public bool Success { get; set; }
    }
}
