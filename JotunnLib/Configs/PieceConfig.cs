using System;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Jotunn.Configs
{
    /// <summary>
    ///     Configuration class for adding custom pieces.<br />
    ///     Use this in a constructor of <see cref="CustomPiece"/> and 
    ///     Jötunn resolves the references to the game objects at runtime.
    /// </summary>
    public class PieceConfig
    {
        /// <summary>
        ///     The name for your piece. May be tokenized.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        ///     The description of your piece. May be tokenized.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        ///     Whether this piece is buildable or not. Defaults to <c>true</c>.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        ///     Can this piece be built in dungeons? Defaults to <c>false</c>.
        /// </summary>
        public bool AllowedInDungeons { get; set; } = false;

        /// <summary>
        ///     The name of the piece table where this piece will be added.
        /// </summary>
        public string PieceTable { get; set; } = string.Empty;

        /// <summary>
        ///     The name of the category this piece will appear on. If categories are disabled on the 
        ///     target <see cref="global::PieceTable"/>, this setting will be ignored.<br />
        ///     If categories are enabled but the given category can't be found, a new 
        ///     <see cref="global::Piece.PieceCategory"/> will be added to the table.
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        ///     The name of the crafting station prefab which needs to be in close proximity to build this piece.
        /// </summary>
        public string CraftingStation { get; set; } = string.Empty;

        /// <summary>
        ///     The name of the crafting station prefab to which this piece will be an upgrade to.
        /// </summary>
        public string ExtendStation { get; set; } = string.Empty;
        
        /// <summary>
        ///     Icon which is displayed in the crafting GUI.
        /// </summary>
        public Sprite Icon { get; set; } = null;

        /// <summary>
        ///     Array of <see cref="RequirementConfig"/>s for all crafting materials it takes to craft the recipe.
        /// </summary>
        public RequirementConfig[] Requirements { get; set; } = new RequirementConfig[0];

        /// <summary>
        ///     Apply this configs values to a piece GameObject.
        /// </summary>
        /// <param name="prefab"></param>
        public void Apply(GameObject prefab)
        {
            var piece = prefab.GetComponent<Piece>();
            if (piece == null)
            {
                Logger.LogWarning($"GameObject has no Piece attached");
                return;
            }

            piece.enabled = Enabled;
            piece.m_allowedInDungeons = AllowedInDungeons;
            
            // Set name if given
            if (!string.IsNullOrEmpty(Name))
            {
                piece.m_name = Name;
            }
            
            // Set description if given
            if (!string.IsNullOrEmpty(Description))
            {
                piece.m_description = Description;
            }
            
            // Set icon if overriden
            if (Icon != null)
            {
                piece.m_icon = Icon;
            }

            // Assign all needed resources for this piece
            piece.m_resources = GetRequirements();

            // Assign the CraftingStation for this piece
            if (!string.IsNullOrEmpty(CraftingStation))
            {
                piece.m_craftingStation = Mock<CraftingStation>.Create(CraftingStation);
            }

            // Assign an extension station for this piece
            if (!string.IsNullOrEmpty(ExtendStation))
            {
                var stationExt = prefab.GetComponent<StationExtension>();
                if (stationExt == null)
                {
                    stationExt = prefab.AddComponent<StationExtension>();
                }
                
                stationExt.m_craftingStation = Mock<CraftingStation>.Create(ExtendStation);
            }

            if (!string.IsNullOrEmpty(Category))
            {
                piece.m_category = PieceManager.Instance.AddPieceCategory(PieceTable, Category);
            }
        }

        /// <summary>
        ///     Converts the <see cref="RequirementConfig"/>s to Valheim style <see cref="Piece.Requirement"/> array.
        /// </summary>
        /// <returns>The Valheim <see cref="global::Piece.Requirement"/> array</returns>
        public Piece.Requirement[] GetRequirements()
        {
            Piece.Requirement[] reqs = new Piece.Requirement[Requirements.Length];

            for (int i = 0; i < reqs.Length; i++)
            {
                reqs[i] = Requirements[i].GetRequirement();
            }

            return reqs;
        }
    }
}
