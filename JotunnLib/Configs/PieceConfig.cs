using System;
using System.Collections.Generic;
using HarmonyLib;
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
        public string PieceTable
        {
            get => pieceTable;
            set => pieceTable = PieceTables.GetInternalName(value);
        }

        /// <summary>
        ///     The name of the category this piece will appear on. If categories are disabled on the 
        ///     target <see cref="global::PieceTable"/>, this setting will be ignored.<br />
        ///     If categories are enabled but the given category can't be found, a new 
        ///     <see cref="Piece.PieceCategory"/> will be added to the table.
        /// </summary>
        public string Category
        {
            get => category;
            set => category = PieceCategories.GetInternalName(value);
        }

        /// <summary>
        ///     The name of the crafting station prefab which needs to be in close proximity to build this piece.
        /// </summary>
        public string CraftingStation
        {
            get => craftingStation;
            set => craftingStation = CraftingStations.GetInternalName(value);
        }

        /// <summary>
        ///     The name of the crafting station prefab to which this piece will be an upgrade to.
        /// </summary>
        public string ExtendStation
        {
            get => extendStation;
            set => extendStation = CraftingStations.GetInternalName(value);
        }

        /// <summary>
        ///     Icon which is displayed in the crafting GUI.
        /// </summary>
        public Sprite Icon { get; set; } = null;

        /// <summary>
        ///     Array of <see cref="RequirementConfig"/>s for all crafting materials it takes to craft the recipe.
        /// </summary>
        public RequirementConfig[] Requirements { get; set; } = Array.Empty<RequirementConfig>();

        private string pieceTable = string.Empty;
        private string category = string.Empty;
        private string craftingStation = string.Empty;
        private string extendStation = string.Empty;

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

            piece.m_enabled = Enabled;
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

            // Assign all needed resources for this piece if provided
            if (Requirements.Length > 0)
            {
                piece.m_resources = GetRequirements();
            }

            // Assign the CraftingStation for this piece
            if (!string.IsNullOrEmpty(CraftingStation))
            {
                piece.m_craftingStation = Mock<CraftingStation>.Create(CraftingStation);
            }

            // Assign an extension station for this piece
            if (!string.IsNullOrEmpty(ExtendStation))
            {
                var stationExt = prefab.GetOrAddComponent<StationExtension>();
                stationExt.m_craftingStation = Mock<CraftingStation>.Create(ExtendStation);
            }

            if (!string.IsNullOrEmpty(Category))
            {
                piece.m_category = PieceManager.Instance.AddPieceCategory(PieceTable, Category);
            }
        }

        /// <summary>
        ///     Converts the <see cref="RequirementConfig">RequirementConfigs</see> to Valheim style <see cref="Piece.Requirement"/> array.
        /// </summary>
        /// <returns>The Valheim <see cref="Piece.Requirement"/> array</returns>
        public Piece.Requirement[] GetRequirements()
        {
            List<Piece.Requirement> reqs = new List<Piece.Requirement>();

            foreach (RequirementConfig requirement in Requirements)
            {
                if (requirement != null && requirement.IsValid())
                {
                    reqs.Add(requirement.GetRequirement());
                }
            }

            return reqs.ToArray();
        }

        /// <summary>
        ///     Loads a single PieceConfig from a JSON string
        /// </summary>
        /// <param name="json">JSON text</param>
        /// <returns>Loaded PieceConfig</returns>
        public static PieceConfig FromJson(string json)
        {
            return SimpleJson.SimpleJson.DeserializeObject<PieceConfig>(json);
        }

        /// <summary>
        ///     Loads a list of PieceConfigs from a JSON string
        /// </summary>
        /// <param name="json">JSON text</param>
        /// <returns>Loaded list of PieceConfigs</returns>
        public static List<PieceConfig> ListFromJson(string json)
        {
            return SimpleJson.SimpleJson.DeserializeObject<List<PieceConfig>>(json);
        }

        /// <summary>
        ///     Appends a new <see cref="RequirementConfig"/> to the array of existing ones.<br />
        ///     If the requirement is null or is not valid (has not item name or amount set) nothing will be added.
        /// </summary>
        /// <param name="requirementConfig"></param>
        public void AddRequirement(RequirementConfig requirementConfig)
        {
            if (requirementConfig != null && requirementConfig.IsValid())
            {
                Requirements = Requirements.AddToArray(requirementConfig);
            }
        }
    }
}
