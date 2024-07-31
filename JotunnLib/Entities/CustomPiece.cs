using System;
using System.Reflection;
using Jotunn.Configs;
using Jotunn.Managers;
using Jotunn.Settings;
using Jotunn.Utils;
using UnityEngine;

namespace Jotunn.Entities
{
    /// <summary>
    ///     Main interface for adding custom pieces to the game.<br />
    ///     All custom pieces have to be wrapped inside this class to add it to Jötunns <see cref="PieceManager"/>.
    /// </summary>
    public class CustomPiece : CustomEntity
    {
        /// <summary>
        ///     The prefab for this custom piece.
        /// </summary>
        public GameObject PiecePrefab { get; }

        /// <summary>
        ///     The <see cref="global::Piece"/> component for this custom piece as a shortcut. 
        /// </summary>
        public Piece Piece { get; }

        /// <summary>
        ///     Name of the <see cref="global::PieceTable"/> this custom piece belongs to.
        /// </summary>
        public string PieceTable
        {
            get => pieceTable;
            set
            {
                var oldPieceTable = pieceTable;
                pieceTable = value;

                if (Piece && !string.IsNullOrEmpty(pieceTable))
                {
                    PieceManager.Instance.RemoveFromPieceTable(Piece, oldPieceTable);
                    PieceManager.Instance.AddToPieceTable(Piece, pieceTable);
                }
            }
        }

        /// <summary>
        ///     Name of the category this custom piece belongs to.<br />
        ///     When setting this value, Piece.m_category will be updated as well.
        /// </summary>
        public string Category
        {
            get => category;
            set
            {
                category = value;

                if (Piece && !string.IsNullOrEmpty(category))
                {
                    Piece.m_category = PieceManager.Instance.AddPieceCategory(category);
                }
            }
        }

        /// <summary>
        ///     Indicator if references from <see cref="Entities.Mock{T}"/>s will be replaced at runtime.
        /// </summary>
        public bool FixReference { get; set; }

        public Setting<bool> SettingsEnabled { get; set; }

        public Setting<string> CategorySetting { get; set; }

        public Setting<string> PieceTableSetting { get; set; }

        /// <summary>
        ///     Indicator if references from configs should get replaced
        /// </summary>
        internal bool FixConfig { get; set; }

        private string PieceName
        {
            get => PiecePrefab ? PiecePrefab.name : fallbackPieceName;
        }

        private string fallbackPieceName;

        private string category;
        private string pieceTable;

        /// <summary>
        ///     Custom piece from a prefab.<br />
        ///     Will be added to the <see cref="global::PieceTable"/> provided by name.<br />
        ///     Can fix references from <see cref="Mock{T}"/>s or not.
        /// </summary>
        /// <param name="piecePrefab">The prefab for this custom piece.</param>
        /// <param name="pieceTable">
        ///     Name of the <see cref="global::PieceTable"/> the custom piece should be added to.
        ///     Can by the "internal" or the <see cref="GameObject"/>s name (e.g. "_PieceTableHammer" or "Hammer")
        /// </param>
        /// <param name="fixReference">If true references for <see cref="Entities.Mock{T}"/> objects get resolved at runtime by Jötunn.</param>
        public CustomPiece(GameObject piecePrefab, string pieceTable, bool fixReference) : base(Assembly.GetCallingAssembly())
        {
            PiecePrefab = piecePrefab;
            Piece = piecePrefab.GetComponent<Piece>();
            PieceTable = pieceTable;
            FixReference = fixReference;
            CreateSettings();
        }

        /// <summary>
        ///     Custom piece from a prefab with a <see cref="PieceConfig"/> attached.<br />
        ///     The members and references from the <see cref="PieceConfig"/> will be referenced by Jötunn at runtime.
        /// </summary>
        /// <param name="piecePrefab">The prefab for this custom piece.</param>
        /// <param name="pieceConfig">The <see cref="PieceConfig"/> for this custom piece.</param>
        [Obsolete("Use CustomPiece(GameObject, bool, PieceConfig) instead and define if references should be fixed")]
        public CustomPiece(GameObject piecePrefab, PieceConfig pieceConfig) : base(Assembly.GetCallingAssembly())
        {
            PiecePrefab = piecePrefab;
            Piece = piecePrefab.GetComponent<Piece>();
            PieceTable = pieceConfig.PieceTable;
            FixReference = false;
            FixConfig = true;
            Category = pieceConfig.Category;

            pieceConfig.Apply(piecePrefab);
            CreateSettings();
        }

        /// <summary>
        ///     Custom piece from a prefab with a <see cref="PieceConfig"/> attached.<br />
        ///     The members and references from the <see cref="PieceConfig"/> will be referenced by Jötunn at runtime.
        /// </summary>
        /// <param name="piecePrefab">The prefab for this custom piece.</param>
        /// <param name="fixReference">If true references for <see cref="Entities.Mock{T}"/> objects get resolved at runtime by Jötunn.</param>
        /// <param name="pieceConfig">The <see cref="PieceConfig"/> for this custom piece.</param>
        public CustomPiece(GameObject piecePrefab, bool fixReference, PieceConfig pieceConfig) : base(Assembly.GetCallingAssembly())
        {
            PiecePrefab = piecePrefab;
            Piece = piecePrefab.GetComponent<Piece>();
            PieceTable = pieceConfig.PieceTable;
            FixReference = fixReference;
            FixConfig = true;
            Category = pieceConfig.Category;

            pieceConfig.Apply(piecePrefab);
            CreateSettings();
        }

        /// <summary>
        ///     Custom piece from a prefab loaded from an <see cref="AssetBundle"/>.<br />
        ///     Will be added to the <see cref="global::PieceTable"/> provided by name.<br />
        ///     Can fix references from <see cref="Mock{T}"/>s or not.
        /// </summary>
        /// <param name="assetBundle">A preloaded <see cref="AssetBundle"/></param>
        /// <param name="assetName">Name of the prefab in the bundle.</param>
        /// <param name = "pieceTable" >
        ///     Name of the <see cref="global::PieceTable"/> the custom piece should be added to.
        ///     Can by the "internal" or the <see cref="GameObject"/>s name (e.g. "_PieceTableHammer" or "Hammer")
        /// </param>
        /// <param name="fixReference">If true references for <see cref="Entities.Mock{T}"/> objects get resolved at runtime by Jötunn.</param>
        public CustomPiece(AssetBundle assetBundle, string assetName, string pieceTable, bool fixReference) : base(Assembly.GetCallingAssembly())
        {
            fallbackPieceName = assetName;

            if (!AssetUtils.TryLoadPrefab(SourceMod, assetBundle, assetName, out GameObject prefab))
            {
                return;
            }

            PiecePrefab = prefab;
            Piece = PiecePrefab.GetComponent<Piece>();
            PieceTable = pieceTable;
            FixReference = fixReference;
            CreateSettings();
        }

        /// <summary>
        ///     Custom piece from a prefab loaded from an <see cref="AssetBundle"/> with a <see cref="PieceConfig"/> attached.<br />
        ///     The members and references from the <see cref="PieceConfig"/> will be referenced by Jötunn at runtime.
        /// </summary>
        /// <param name="assetBundle">A preloaded <see cref="AssetBundle"/></param>
        /// <param name="assetName">Name of the prefab in the bundle.</param>
        /// <param name="pieceConfig">The <see cref="PieceConfig"/> for this custom piece.</param>
        [Obsolete("Use CustomPiece(AssetBundle, string, bool, PieceConfig) instead and define if references should be fixed")]
        public CustomPiece(AssetBundle assetBundle, string assetName, PieceConfig pieceConfig) : base(Assembly.GetCallingAssembly())
        {
            fallbackPieceName = assetName;

            if (!AssetUtils.TryLoadPrefab(SourceMod, assetBundle, assetName, out GameObject prefab))
            {
                return;
            }

            PiecePrefab = prefab;
            Piece = PiecePrefab.GetComponent<Piece>();
            PieceTable = pieceConfig.PieceTable;
            FixReference = false;
            FixConfig = true;
            Category = pieceConfig.Category;

            pieceConfig.Apply(PiecePrefab);
            CreateSettings();
        }

        /// <summary>
        ///     Custom piece from a prefab loaded from an <see cref="AssetBundle"/> with a <see cref="PieceConfig"/> attached.<br />
        ///     The members and references from the <see cref="PieceConfig"/> will be referenced by Jötunn at runtime.
        /// </summary>
        /// <param name="assetBundle">A preloaded <see cref="AssetBundle"/></param>
        /// <param name="assetName">Name of the prefab in the bundle.</param>
        /// <param name="fixReference">If true references for <see cref="Entities.Mock{T}"/> objects get resolved at runtime by Jötunn.</param>
        /// <param name="pieceConfig">The <see cref="PieceConfig"/> for this custom piece.</param>
        public CustomPiece(AssetBundle assetBundle, string assetName, bool fixReference, PieceConfig pieceConfig) : base(Assembly.GetCallingAssembly())
        {
            fallbackPieceName = assetName;

            if (!AssetUtils.TryLoadPrefab(SourceMod, assetBundle, assetName, out GameObject prefab))
            {
                return;
            }

            PiecePrefab = prefab;
            Piece = PiecePrefab.GetComponent<Piece>();
            PieceTable = pieceConfig.PieceTable;
            FixReference = fixReference;
            FixConfig = true;
            Category = pieceConfig.Category;

            pieceConfig.Apply(PiecePrefab);
            CreateSettings();
        }

        /// <summary>
        ///     Custom piece created as an "empty" primitive.<br />
        ///     Will be added to the <see cref="global::PieceTable"/> provided by name.
        /// </summary>
        /// <param name="name">Name of the new prefab. Must be unique.</param>
        /// <param name="addZNetView">If true a ZNetView component will be added to the prefab for network sync.</param>
        /// <param name = "pieceTable" >
        ///     Name of the <see cref="global::PieceTable"/> the custom piece should be added to.
        ///     Can by the "internal" or the <see cref="GameObject"/>s name (e.g. "_PieceTableHammer" or "Hammer")
        /// </param>
        public CustomPiece(string name, bool addZNetView, string pieceTable) : base(Assembly.GetCallingAssembly())
        {
            PiecePrefab = PrefabManager.Instance.CreateEmptyPrefab(name, addZNetView);

            if (!PiecePrefab)
            {
                fallbackPieceName = name;
                return;
            }

            Piece = PiecePrefab.AddComponent<Piece>();
            Piece.m_name = name;
            PieceTable = pieceTable;
            CreateSettings();
        }

        /// <summary>
        ///     Custom piece created as an "empty" primitive with a <see cref="PieceConfig"/> attached.<br />
        ///     The members and references from the <see cref="PieceConfig"/> will be referenced by Jötunn at runtime.
        /// </summary>
        /// <param name="name">Name of the new prefab. Must be unique.</param>
        /// <param name="addZNetView">If true a ZNetView component will be added to the prefab for network sync.</param>
        /// <param name="pieceConfig">The <see cref="PieceConfig"/> for this custom piece.</param>
        public CustomPiece(string name, bool addZNetView, PieceConfig pieceConfig) : base(Assembly.GetCallingAssembly())
        {
            PiecePrefab = PrefabManager.Instance.CreateEmptyPrefab(name, addZNetView);

            if (!PiecePrefab)
            {
                fallbackPieceName = name;
                return;
            }

            Piece = PiecePrefab.AddComponent<Piece>();
            PieceTable = pieceConfig.PieceTable;
            FixConfig = true;
            Category = pieceConfig.Category;

            pieceConfig.Apply(PiecePrefab);
            CreateSettings();
        }

        /// <summary>
        ///     Custom piece created as a copy of a vanilla Valheim prefab.<br />
        ///     Will be added to the <see cref="global::PieceTable"/> provided by name.
        /// </summary>
        /// <param name="name">The new name of the prefab after cloning.</param>
        /// <param name="baseName">The name of the base prefab the custom item is cloned from.</param>
        /// <param name = "pieceTable" >
        ///     Name of the <see cref="global::PieceTable"/> the custom piece should be added to.
        ///     Can by the "internal" or the <see cref="GameObject"/>s name (e.g. "_PieceTableHammer" or "Hammer")
        /// </param>
        public CustomPiece(string name, string baseName, string pieceTable) : base(Assembly.GetCallingAssembly())
        {
            PiecePrefab = PrefabManager.Instance.CreateClonedPrefab(name, baseName);

            if (!PiecePrefab)
            {
                fallbackPieceName = name;
                return;
            }

            Piece = PiecePrefab.GetComponent<Piece>();
            PieceTable = pieceTable;
            CreateSettings();
        }

        /// <summary>
        ///     Custom piece created as a copy of a vanilla Valheim prefab with a <see cref="PieceConfig"/> attached.<br />
        ///     The members and references from the <see cref="PieceConfig"/> will be referenced by Jötunn at runtime.
        /// </summary>
        /// <param name="name">The new name of the prefab after cloning.</param>
        /// <param name="baseName">The name of the base prefab the custom item is cloned from.</param>
        /// <param name="pieceConfig">The <see cref="PieceConfig"/> for this custom piece.</param>
        public CustomPiece(string name, string baseName, PieceConfig pieceConfig) : base(Assembly.GetCallingAssembly())
        {
            PiecePrefab = PrefabManager.Instance.CreateClonedPrefab(name, baseName);

            if (!PiecePrefab)
            {
                fallbackPieceName = name;
                return;
            }

            Piece = PiecePrefab.GetComponent<Piece>();
            PieceTable = pieceConfig.PieceTable;
            FixConfig = true;
            Category = pieceConfig.Category;

            pieceConfig.Apply(PiecePrefab);
            CreateSettings();
        }

        private void CreateSettings()
        {
            SettingsEnabled = new BepInExSetting<bool>(SourceMod, PiecePrefab.name, "Enabled", false, $"Enable settings for {PiecePrefab.name}", 10);
            SettingsEnabled.OnChanged += () =>
            {
                BindSettings();
                ConfigManagerUtils.BuildSettingList();
            };

            CategorySetting = new BepInExDropdownSetting<string>(SourceMod, PiecePrefab.name, "Category", Category, PieceCategories.GetNames().Keys, $"Tool Category of {PiecePrefab.name}", 9);
            CategorySetting.OnChanged += () => Category = CategorySetting.Value;

            PieceTableSetting = new BepInExDropdownSetting<string>(SourceMod, PiecePrefab.name, "Tool", PieceTables.GetDisplayName(PieceTable), PieceTables.GetNames().Keys, $"Tool of of {PiecePrefab.name}", 8);
            PieceTableSetting.OnChanged += () => PieceTable = PieceTableSetting.Value;
        }

        /// <summary>
        ///     Checks if a custom piece is valid (i.e. has a prefab, a target PieceTable is set,
        ///     has a <see cref="global::Piece"/> component and that component has an icon).
        /// </summary>
        /// <returns>true if all criteria is met</returns>
        public bool IsValid()
        {
            bool valid = true;

            if (!PiecePrefab)
            {
                Logger.LogError(SourceMod, $"CustomPiece '{this}' has no prefab");
                valid = false;
            }

            if (PiecePrefab && !PiecePrefab.IsValid())
            {
                valid = false;
            }

            if (!Piece)
            {
                Logger.LogError(SourceMod, $"CustomPiece '{this}' has no Piece component");
                valid = false;
            }

            if (Piece && !Piece.m_icon)
            {
                Logger.LogError(SourceMod, $"CustomPiece '{this}' has no icon");
                valid = false;
            }

            if (string.IsNullOrEmpty(PieceTable))
            {
                Logger.LogError(SourceMod, $"CustomPiece '{this}' has no PieceTable");
                valid = false;
            }

            return valid;
        }

        public void BindSettings()
        {
            SettingsEnabled?.Bind();
            CategorySetting?.UpdateBinding(SettingsEnabled?.Value ?? false);
            PieceTableSetting?.UpdateBinding(SettingsEnabled?.Value ?? false);
        }

        /// <summary>
        ///     Helper method to determine if a prefab with a given name is a custom piece created with Jötunn.
        /// </summary>
        /// <param name="prefabName">Name of the prefab to test.</param>
        /// <returns>true if the prefab is added as a custom piece to the <see cref="PieceManager"/>.</returns>
        public static bool IsCustomPiece(string prefabName)
        {
            return PieceManager.Instance.Pieces.ContainsKey(prefabName);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj.GetHashCode() == GetHashCode();
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return PieceName.GetStableHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return PieceName;
        }
    }
}
