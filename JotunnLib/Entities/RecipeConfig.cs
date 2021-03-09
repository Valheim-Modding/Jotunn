using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JotunnLib.Managers;

namespace JotunnLib.Entities
{
    public class RecipeConfig
    {
        public string Item { get; set; }
        public int Amount { get; set; }
        public bool Enabled { get; set; } = true;
        public string CraftingStation { get; set; }
        public int MinStationLevel { get; set; }
        public PieceRequirementConfig[] Requirements { get; set; } = new PieceRequirementConfig[0];

        public Recipe GetRecipe()
        {
            Piece.Requirement[] resources = new Piece.Requirement[Requirements.Length];

            for (int i = 0; i < resources.Length; i++)
            {
                resources[i] = Requirements[i].GetPieceRequirement();
            }

            return new Recipe()
            {
                m_item = PrefabManager.Instance.GetPrefab(Item).GetComponent<ItemDrop>(),
                m_amount = Amount,
                m_enabled = Enabled,
                m_craftingStation = PrefabManager.Instance.GetPrefab(CraftingStation).GetComponent<CraftingStation>(),
                m_minStationLevel = MinStationLevel,
                m_resources = resources
            };
        }
    }
}
