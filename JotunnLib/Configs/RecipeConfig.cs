using UnityEngine;
using JotunnLib.Managers;
using JotunnLib.Entities;

namespace JotunnLib.Configs
{
    public class RecipeConfig
    {
        public string Name { get; set; } = null;
        public string Item { get; set; }
        public int Amount { get; set; } = 1;
        public bool Enabled { get; set; } = true;
        public string CraftingStation { get; set; } = null;
        public string RepairStation { get; set; } = null;
        public int MinStationLevel { get; set; }
        public PieceRequirementConfig[] Requirements { get; set; } = new PieceRequirementConfig[0];

        public Piece.Requirement[] GetRequirements()
        {
            Piece.Requirement[] reqs = new Piece.Requirement[Requirements.Length];

            for (int i = 0; i < reqs.Length; i++)
            {
                reqs[i] = Requirements[i].GetPieceRequirement();
            }

            return reqs;
        }

        public Recipe GetRecipe()
        {
            var recipe = ScriptableObject.CreateInstance<Recipe>();

            var name = Name;
            if (string.IsNullOrEmpty(name))
            {
                name = "Recipe_" + Item;
            }

            recipe.name = name;
            recipe.m_item = Mock<ItemDrop>.Create(Name);
            recipe.m_amount = Amount;
            recipe.m_enabled = Enabled;

            if (CraftingStation != null)
            {
                recipe.m_craftingStation = Mock<CraftingStation>.Create(CraftingStation);
            }

            if (RepairStation != null)
            {
                recipe.m_craftingStation = Mock<CraftingStation>.Create(RepairStation);
            }

            recipe.m_minStationLevel = MinStationLevel;
            recipe.m_resources = GetRequirements();

            return recipe;
        }
    }
}
