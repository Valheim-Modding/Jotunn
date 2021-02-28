using System;
using UnityEngine;

namespace ValheimLokiLoader.Events
{
    public class PlayerPlacedPieceEventArgs : EventArgs
    {
        public Player Player { get; set; }
        public Piece Piece { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public bool Success { get; set; }
    }
}
