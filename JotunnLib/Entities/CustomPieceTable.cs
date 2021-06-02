using System;
using System.Collections.Generic;
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
    public class CustomPieceTable
    {
        /// <summary>
        ///     The prefab for this custom piece table.
        /// </summary>
        public GameObject PieceTablePrefab { get; set; }

        /// <summary>
        ///     The <see cref="global::PieceTable"/> component for this custom piece table as a shortcut. 
        ///     Will not be added again to the prefab when replaced.
        /// </summary>
        public PieceTable PieceTable { get; set; } = null;
        
        public string[] Categories { get; set; } = new string[0];

        public CustomPieceTable(GameObject pieceTablePrefab)
        {
            PieceTablePrefab = pieceTablePrefab;
            PieceTable = pieceTablePrefab.GetComponent<PieceTable>();
        }

        public CustomPieceTable(GameObject pieceTablePrefab, PieceTableConfig config)
        {
            PieceTablePrefab = pieceTablePrefab;
            config.Apply(pieceTablePrefab);
            PieceTable = pieceTablePrefab.GetComponent<PieceTable>();
            Categories = config.GetCategories();
        }

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
            return PieceTablePrefab && PieceTable != null;
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
