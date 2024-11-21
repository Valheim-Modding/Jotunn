using Jotunn.Configs;
using Jotunn.Settings;
using Jotunn.Utils;
using UnityEngine;

namespace Jotunn.Entities
{
    /// <summary>
    ///     Class for in-game settings for custom pieces
    /// </summary>
    public class CustomPieceSettings
    {
        /// <summary>
        ///     Setting to enable or disable the configuration for this piece
        /// </summary>
        public Setting<bool> SettingsEnabled { get; set; }

        /// <summary>
        ///     Setting for the category of this piece
        /// </summary>
        public Setting<string> Category { get; set; }

        /// <summary>
        ///     Setting for the piece table of this piece
        /// </summary>
        public Setting<string> PieceTable { get; set; }

        /// <summary>
        ///     Create a new settings object for a custom piece
        /// </summary>
        /// <param name="piece"></param>
        public CustomPieceSettings(CustomPiece piece)
        {
            var sourceMod = piece.SourceMod;
            var prefabName = piece.PiecePrefab.name;

            SettingsEnabled = new BepInExSetting<bool>(sourceMod, prefabName, "Enabled", false, $"Enable settings for {prefabName}", 10);
            SettingsEnabled.OnChanged += () =>
            {
                Bind();
                ConfigManagerUtils.BuildSettingList();
            };

            Category = new BepInExDropdownSetting<string>(sourceMod, prefabName, "Category", piece.Category, PieceCategories.GetNames().Keys, $"Tool Category", 9);
            Category.OnChanged += () => piece.Category = Category.Value;

            PieceTable = new BepInExDropdownSetting<string>(sourceMod, prefabName, "Tool", PieceTables.GetDisplayName(piece.PieceTable), PieceTables.GetNames().Keys, $"Tool prefab", 8);
            PieceTable.OnChanged += () => piece.PieceTable = PieceTable.Value;
        }

        internal void Bind()
        {
            SettingsEnabled?.Bind();
            Category?.UpdateBinding(SettingsEnabled?.Value ?? false);
            PieceTable?.UpdateBinding(SettingsEnabled?.Value ?? false);
        }
    }
}
