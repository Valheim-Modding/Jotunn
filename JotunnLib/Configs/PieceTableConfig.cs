using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Jotunn.Configs
{
    /// <summary>
    ///     Configuration class for adding custom piece tables.
    /// </summary>
    public class PieceTableConfig
    {
        public bool UseCategories { get; set; } = true;
        
        public bool UseCustomCategories { get; set; } = false;

        public string[] CustomCategories { get; set; } = new string[0];

        public bool CanRemovePieces { get; set; } = true;

        public string[] GetCategories()
        {
            List<string> categories = new List<string>();

            if (UseCategories)
            {
                for (int i = 0; i < (int)Piece.PieceCategory.Max; i++)
                {
                    categories.Add(Enum.GetName(typeof(Piece.PieceCategory), i));
                }
            }

            if (UseCustomCategories)
            {
                for (int i = 0; i < CustomCategories.Length; i++)
                {
                    categories.Add(CustomCategories[i]);
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
