using Jotunn.Entities;

namespace Jotunn.Configs
{
    /// <summary>
    ///     Configuration class for adding custom rooms.<br />
    ///     Use this in a constructor of <see cref="CustomRoom"/>
    /// </summary>
    public class RoomConfig
    {
        /// <summary>
        ///     Theme name of this room.
        /// </summary>
        public string ThemeName { get; set; }

        /// <summary>
        ///     <see cref="Room.Theme"/> of this room.
        /// </summary>
        public Room.Theme Theme { get; set; }

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
        ///     Create a config with a <see cref="Room.Theme"/>.
        /// </summary>
        /// <param name="theme"></param>
        public RoomConfig(Room.Theme theme)
        {
            Theme = theme;
            ThemeName = string.Empty;
        }

        /// <summary>
        ///     Create a config with a theme name.
        /// </summary>
        /// <param name="themeName"></param>
        public RoomConfig(string themeName)
        {
            Theme = 0;
            ThemeName = themeName;
        }
        
        
        /// <summary>
        ///     Converts the RoomConfig to a Valheim style <see cref="Room"/>.
        /// </summary>
        public Room ApplyConfig(RoomConfig roomConfig)
        {
            // Create a new Room instance
            Room room = new Room();
            
            // Ensure Room values are overwritten only when a value's present.
            if (roomConfig.Enabled != null) room.m_enabled = roomConfig.Enabled.Value;
            if (roomConfig.Entrance != null) room.m_entrance = roomConfig.Entrance.Value;
            if (roomConfig.Endcap != null) room.m_endCap = roomConfig.Endcap.Value;
            if (roomConfig.Divider != null) room.m_divider = roomConfig.Divider.Value;
            if (roomConfig.EndcapPrio != null) room.m_endCapPrio = roomConfig.EndcapPrio.Value;
            if (roomConfig.MinPlaceOrder != null) room.m_minPlaceOrder = roomConfig.MinPlaceOrder.Value;
            if (roomConfig.Weight != null) room.m_weight = roomConfig.Weight.Value;
            if (roomConfig.FaceCenter != null) room.m_faceCenter = roomConfig.FaceCenter.Value;
            if (roomConfig.Perimeter != null) room.m_perimeter = roomConfig.Perimeter.Value;
            
            // Room can be matched by either a Room.Theme flag, or a ThemeName string.
            if (roomConfig.Theme == 0)
            {
                ThemeName = roomConfig.ThemeName;
                Theme = 0;
            }
            else
            {
                Theme = roomConfig.Theme;
                ThemeName = string.Empty;
            }

            return room;
        }
    }
}
