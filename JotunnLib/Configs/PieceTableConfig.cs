using System;
using System.Collections.Generic;
using Jotunn.Utils;
using UnityEngine;

namespace Jotunn.Configs
{
    /// <summary>
    ///     Configuration class for adding custom piece tables.
    /// </summary>
    public class PieceTableConfig
    {
        /// <summary>
        ///     Indicator if the <see cref="PieceTable"/> uses the vanilla categories. Defaults to <c>true</c>.
        /// </summary>
        public bool UseCategories { get; set; } = true;

        /// <summary>
        ///     Indicator if the <see cref="PieceTable"/> uses custom categories. Defaults to <c>false</c>.
        /// </summary>
        public bool UseCustomCategories { get; set; } = false;

        /// <summary>
        ///     Array of custom categories the <see cref="PieceTable"/> uses. 
        ///     Will be ignored when <see cref="UseCustomCategories"/> is false.
        /// </summary>
        public string[] CustomCategories { get; set; } = Array.Empty<string>();

        /// <summary>
        ///     Indicator if the <see cref="PieceTable"/> can also remove pieces. Defaults to <c>true</c>.
        /// </summary>
        public bool CanRemovePieces { get; set; } = true;

        /// <summary>
        ///     Creates the final categories array for this <see cref="PieceTable"/>. 
        ///     Adds vanilla categories when <see cref="UseCategories"/> is true.
        ///     Adds custom categories when <see cref="UseCustomCategories"/> is true.
        /// </summary>
        /// <returns>Array of category strings.</returns>
        public string[] GetCategories()
        {
            List<string> categories = new List<string>();

            if (UseCategories)
            {
                for (int i = 0; i < PieceUtils.VanillaMaxPieceCategory; i++)
                {
                    categories.Add(Enum.GetName(typeof(Piece.PieceCategory), i));
                }
            }

            if (UseCustomCategories)
            {
                foreach (var category in CustomCategories)
                {
                    categories.Add(category);
                }
            }

            return categories.ToArray();
        }

        /// <summary>
        ///     Apply this configs values to a piece table GameObject.
        /// </summary>
        /// <param name="prefab"></param>
        public void Apply(GameObject prefab)
        {
            var table = prefab.GetComponent<PieceTable>();
            if (table == null)
            {
                Logger.LogWarning($"GameObject has no PieceTable attached");
                return;
            }

            // Use categories at all?
            table.m_useCategories = UseCategories || UseCustomCategories;

            // Can remove pieces?
            table.m_canRemovePieces = CanRemovePieces;
        }
    }
}
