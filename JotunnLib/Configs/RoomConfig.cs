using System;
using Jotunn.Entities;
using UnityEngine;

namespace Jotunn.Configs
{
    /// <summary>
    ///     Configuration class for adding custom rooms.<br />
    ///     Use this in a constructor of <see cref="CustomRoom"/>
    /// </summary>
    public class RoomConfig
    {
        /// <summary>
        ///     Theme name of this room.  If adding a room to a vanilla dungeon, use nameof(Room.Theme.value)
        /// </summary>
        public string ThemeName { get; set; }

        /// <summary>
        ///     If set to false, room will not be added to <see cref="DungeonGenerator.m_availableRooms"/>, thus
        ///     won't be used during generation.
        /// </summary>
        public bool? Enabled { get; set; }

        /// <summary>
        ///     Flag indicating if this room is a dungeon entrance.
        /// </summary>
        public bool? Entrance { get; set; }

        /// <summary>
        ///     Flag indicating if this room is an endcap.
        /// </summary>
        public bool? Endcap { get; set; }

        /// <summary>
        ///     Flag indicating if this room is a divider.
        /// </summary>
        public bool? Divider { get; set; }

        /// <summary>
        ///     A rank value to prioritize this endcap over others during generation.
        /// </summary>
        public int? EndcapPrio { get; set; }

        /// <summary>
        ///     Exclude this room if the adjoining connection's <see cref="RoomConnection.m_placeOrder"/> is less than this value. 
        /// </summary>
        public int? MinPlaceOrder { get; set; }

        /// <summary>
        ///     A weight value used to sort available rooms during some modes of generation.  Defaults to 1f.
        /// </summary>
        public float? Weight { get; set; }

        /// <summary>
        ///     Flag to orient this room towards the center.  Used only for generating camps (draugr/fuling villages).
        /// </summary>
        public bool? FaceCenter { get; set; }

        /// <summary>
        ///     Flag to ensure this room is only placed around the perimeter of a camp.  Used only for generating camps (draugr/fuling villages).
        /// </summary>
        public bool? Perimeter { get; set; }

        /// <summary>
        ///     Create a config with a theme name.
        /// </summary>
        /// <param name="themeName"></param>
        public RoomConfig(string themeName)
        {
            ThemeName = themeName;
        }

        /// <summary>
        ///     Create a new <see cref="RoomConfig"/>
        /// </summary>
        public RoomConfig() { }

        /// <summary>
        ///     Converts the RoomConfig to a Valheim style <see cref="Room"/>.
        /// </summary>
        public Room Apply(Room room)
        {
            // Ensure Room values are overwritten only when a value's present.
            if (Enabled != null) room.m_enabled = Enabled.Value;
            if (Entrance != null) room.m_entrance = Entrance.Value;
            if (Endcap != null) room.m_endCap = Endcap.Value;
            if (Divider != null) room.m_divider = Divider.Value;
            if (EndcapPrio != null) room.m_endCapPrio = EndcapPrio.Value;
            if (MinPlaceOrder != null) room.m_minPlaceOrder = MinPlaceOrder.Value;
            if (Weight != null) room.m_weight = Weight.Value;
            if (FaceCenter != null) room.m_faceCenter = FaceCenter.Value;
            if (Perimeter != null) room.m_perimeter = Perimeter.Value;
            if (Perimeter != null) room.m_perimeter = Perimeter.Value;

            return room;
        }
    }
}
