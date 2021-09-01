using System;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace Jotunn.Entities
{
    /// <summary>
    ///     Main interface for adding custom piece tables to the game.<br />
    ///     All custom piece tables have to be wrapped inside this class 
    ///     to add it to Jötunns <see cref="PieceManager"/>.<br />
    ///     Add strings to <see cref="Categories"/> to use custom categories on your
    ///     piece table. All categories will be replaced so list vanilla categories, too.
    /// </summary>
    public class CustomPieceTable : CustomEntity
    {
        /// <summary>
        ///     The prefab for this custom piece table.
        /// </summary>
        public GameObject PieceTablePrefab { get; }

        /// <summary>
        ///     The <see cref="global::PieceTable"/> component for this custom piece table as a shortcut.
        /// </summary>
        public PieceTable PieceTable { get; }

        /// <summary>
        ///     String array of categories used on the <see cref="global::PieceTable"/>. 
        ///     Will be ignored when m_useCategories is false.<br />
        ///     All categories provided here will be used and displayed on the <see cref="Hud"/>.
        /// </summary>
        public string[] Categories { get; } = Array.Empty<string>();

        /// <summary>
        ///     Custom piece table from a prefab.
        /// </summary>
        /// <param name="pieceTablePrefab">The prefab for this custom piece table.</param>
        public CustomPieceTable(GameObject pieceTablePrefab)
        {
            PieceTablePrefab = pieceTablePrefab;
            PieceTable = pieceTablePrefab.GetComponent<PieceTable>();
        }

        /// <summary>
        ///     Custom piece table from a prefab with a <see cref="PieceTableConfig"/> attached.
        /// </summary>
        /// <param name="pieceTablePrefab">The prefab for this custom piece table.</param>
        /// <param name="config">The <see cref="PieceTableConfig"/> for this custom piece table.</param>
        public CustomPieceTable(GameObject pieceTablePrefab, PieceTableConfig config)
        {
            PieceTablePrefab = pieceTablePrefab;
            config.Apply(pieceTablePrefab);
            PieceTable = pieceTablePrefab.GetComponent<PieceTable>();
            Categories = config.GetCategories();
        }

        /// <summary>
        ///     "Empty" custom piece table with a <see cref="PieceTableConfig"/> attached.
        /// </summary>
        /// <param name="name">The name of the custom piece table.</param>
        /// <param name="config">The <see cref="PieceTableConfig"/> for this custom piece table.</param>
        public CustomPieceTable(string name, PieceTableConfig config)
        {
            PieceTablePrefab = new GameObject(name);
            PieceTable = PieceTablePrefab.AddComponent<PieceTable>();
            config.Apply(PieceTablePrefab);
            Categories = config.GetCategories();
        }

        /// <summary>
        ///     Checks if a custom piece table is valid (i.e. has a prefab and a PieceTable component).
        /// </summary>
        /// <returns>true if all criteria is met</returns>
        public bool IsValid()
        {
            bool valid = true;

            if (!PieceTablePrefab)
            {
                Logger.LogError($"CustomPieceTable {this} has no prefab");
                valid = false;
            }
            if (!PieceTablePrefab.IsValid())
            {
                valid = false;
            }
            if (PieceTable == null)
            {
                Logger.LogError($"CustomPieceTable {this} has no PieceTable component");
                valid = false;
            }

            return valid;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj.GetHashCode() == GetHashCode();
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return PieceTablePrefab.name.GetStableHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return PieceTablePrefab.name;
        }
    }
}
