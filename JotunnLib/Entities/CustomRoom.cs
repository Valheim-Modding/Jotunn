using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Jotunn.Configs;
using Jotunn.Managers;
using SoftReferenceableAssets;
using UnityEngine;

namespace Jotunn.Entities
{
    /// <summary>
    ///     Main interface for adding custom dungeon rooms to the game.<br />
    ///     All custom rooms have to be wrapped inside this class to add it to Jötunns <see cref="DungeonManager"/>.
    /// </summary>
    public class CustomRoom : CustomEntity
    {
        /// <summary>
        ///     The prefab for this custom room.
        /// </summary>
        public GameObject Prefab { get; }

        /// <summary>
        ///     The <see cref="global::Room"/> component for this custom room as a shortcut.
        /// </summary>
        public Room Room { get; }

        /// <summary>
        ///     The name of this custom room as a shortcut.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Indicator if references from <see cref="Entities.Mock{T}"/>s will be replaced at runtime.
        /// </summary>
        public bool FixReference { get; set; }

        /// <summary>
        ///     Theme name of this room.
        /// </summary>
        public string ThemeName { get; private set; }

        /// <summary>
        ///     <see cref="Room.Theme"/> of this room.
        /// </summary>
        public Room.Theme Theme { get; private set; }

        /// <summary>
        ///     Associated <see cref="DungeonDB.RoomData"/> holding data used during generation.
        /// </summary>
        public DungeonDB.RoomData RoomData { get; private set; }

        /// <summary>
        ///     Custom room from a prefab loaded from an <see cref="AssetBundle"/> with a <see cref="global::Room"/> made from a <see cref="RoomConfig"/>.<br />
        ///     Can fix references for <see cref="Entities.Mock{T}"/>s.
        /// </summary>
        /// <param name="assetBundle">A preloaded <see cref="AssetBundle"/></param>
        /// <param name="assetName">Name of the prefab in the bundle.</param>
        /// <param name="fixReference">If true references for <see cref="Entities.Mock{T}"/> objects get resolved at runtime by Jötunn.</param>
        /// <param name="roomConfig">The config for this custom room.</param>
        public CustomRoom(AssetBundle assetBundle, string assetName, bool fixReference, RoomConfig roomConfig) : base(Assembly.GetCallingAssembly())
        {
            Prefab = assetBundle.LoadAsset<GameObject>(assetName);
            Name = Prefab.name;
            if (Prefab != null && Prefab.TryGetComponent<Room>(out var room))
            {
                Room = room;
            }
            else
            {
                Room = Prefab.AddComponent<Room>();
            }
            FixReference = fixReference;
            ApplyConfig(roomConfig);
        }


        /// <summary>
        ///     Custom room from a prefab with a <see cref="global::Room"/> made from a <see cref="RoomConfig"/>.
        /// </summary>
        /// <param name="prefab">The prefab for this custom room.</param>
        /// <param name="roomConfig">The config for this custom room.</param>
        public CustomRoom(GameObject prefab, RoomConfig roomConfig)
            : this(prefab, false, roomConfig) { }


        /// <summary>
        ///     Custom room from a prefab loaded from an <see cref="AssetBundle"/> with a <see cref="global::Room"/> made from a <see cref="RoomConfig"/>.<br />
        ///     Can fix references for <see cref="Entities.Mock{T}"/>s.
        /// </summary>
        /// <param name="prefab">The prefab for this custom room.</param>
        /// <param name="fixReference">If true references for <see cref="Entities.Mock{T}"/> objects get resolved at runtime by Jötunn.</param>
        /// <param name="roomConfig">The config for this custom room.</param>
        public CustomRoom(GameObject prefab, bool fixReference, RoomConfig roomConfig) : base(Assembly.GetCallingAssembly())
        {
            Prefab = prefab;
            Name = prefab.name;
            if (prefab != null && prefab.TryGetComponent<Room>(out var room))
            {
                Room = room;
            }
            else
            {
                Room = prefab.AddComponent<Room>();
            }
            FixReference = fixReference;
            ApplyConfig(roomConfig);
        }

        /// <summary>
        ///     Helper method to determine if a prefab with a given name is a custom room created with Jötunn.
        /// </summary>
        /// <param name="prefabName">Name of the prefab to test.</param>
        /// <returns>true if the prefab is added as a custom item to the <see cref="DungeonManager"/>.</returns>
        public static bool IsCustomRoom(string prefabName)
        {
            return DungeonManager.Instance.Rooms.ContainsKey(prefabName);
        }

        private void ApplyConfig(RoomConfig roomConfig)
        {
            // Ensure Room values are overwritten only when a value's present.
            if (roomConfig.Enabled != null) Room.m_enabled = roomConfig.Enabled.Value;
            if (roomConfig.Entrance != null) Room.m_entrance = roomConfig.Entrance.Value;
            if (roomConfig.Endcap != null) Room.m_endCap = roomConfig.Endcap.Value;
            if (roomConfig.Divider != null) Room.m_divider = roomConfig.Divider.Value;
            if (roomConfig.EndcapPrio != null) Room.m_endCapPrio = roomConfig.EndcapPrio.Value;
            if (roomConfig.MinPlaceOrder != null) Room.m_minPlaceOrder = roomConfig.MinPlaceOrder.Value;
            if (roomConfig.Weight != null) Room.m_weight = roomConfig.Weight.Value;
            if (roomConfig.FaceCenter != null) Room.m_faceCenter = roomConfig.FaceCenter.Value;
            if (roomConfig.Perimeter != null) Room.m_perimeter = roomConfig.Perimeter.Value;

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

            // DungeonGenerator.PlaceRoom*() utilize soft references directly, thus registering the assets here.
            RoomData = new DungeonDB.RoomData()
            {
                m_prefab = new SoftReference<GameObject>(AssetManager.Instance.AddAsset(Prefab)),
                m_loadedRoom = Room,
                m_prefabData = new RoomPrefabData()
                {
                    m_enabled = Room.m_enabled,
                    m_theme = Theme
                }
            };
        }
    }
}
