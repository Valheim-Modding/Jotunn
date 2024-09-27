using System;
using System.Reflection;
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
        public string ThemeName { get; set; }

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

            ThemeName = roomConfig.ThemeName;

            if (Prefab != null && Prefab.TryGetComponent<Room>(out var room))
            {
                var existingRoom = room;
                Room = roomConfig.Apply(existingRoom);
            }
            else
            {
                var newRoom = Prefab.AddComponent<Room>();
                Room = roomConfig.Apply(newRoom);
            }

            FixReference = fixReference;

            // DungeonGenerator.PlaceRoom*() utilize soft references directly, thus registering the assets here.
            RoomData = new DungeonDB.RoomData()
            {
                m_prefab = new SoftReference<GameObject>(AssetManager.Instance.AddAsset(Prefab)),
                m_loadedRoom = Room,
                m_enabled = Room.m_enabled,
                m_theme = GetRoomTheme(ThemeName)
            };
        }

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

            ThemeName = roomConfig.ThemeName;

            if (prefab != null && prefab.TryGetComponent<Room>(out var room))
            {
                var existingRoom = room;
                Room = roomConfig.Apply(existingRoom);
            }
            else
            {
                var newRoom = prefab.AddComponent<Room>();
                Room = roomConfig.Apply(newRoom);
            }

            FixReference = fixReference;

            // DungeonGenerator.PlaceRoom*() utilize soft references directly, thus registering the assets here.
            RoomData = new DungeonDB.RoomData()
            {
                m_prefab = new SoftReference<GameObject>(AssetManager.Instance.AddAsset(Prefab)),
                m_loadedRoom = Room,
                m_enabled = Room.m_enabled,
                m_theme = GetRoomTheme(ThemeName)
            };
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

        /// <summary>
        ///     Helper method to determine if a given themeName matches any vanilla <see cref="global::Room.Theme"/> values.
        /// </summary>
        /// <param name="themeName">Name of the theme to test.</param>
        /// <returns>true if the themeName matches a built-in value, or false.</returns>
        public static bool IsVanillaTheme(string themeName)
        {
            return Enum.TryParse(themeName, false, out Room.Theme _);
        }

        /// <summary>
        ///     Helper method to get the <see cref="global::Room.Theme"/> value, if the given themeName matches any vanilla values.
        /// </summary>
        /// <param name="themeName">Name of the theme.</param>
        /// <returns>The <see cref="global::Room.Theme"/> value, or Room.Theme.None if no match is found.</returns>        
        public static Room.Theme GetRoomTheme(string themeName)
        {
            if (Enum.TryParse(themeName, false, out Room.Theme theme))
            {
                return theme;
            }

            return Room.Theme.None;
        }
    }
}
