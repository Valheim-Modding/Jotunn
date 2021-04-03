using JotunnLib.Configs;
using JotunnLib.Managers;
using UnityEngine;

namespace JotunnLib.Entities
{
    public class CustomRecipe
    {
        public Recipe Recipe;

        public bool FixReference { get; set; } = false;
        public bool FixRequirementReferences { get; set; } = false;

        public CustomRecipe(Recipe recipe, bool fixReference, bool fixRequirementReferences)
        {
            Recipe = recipe;
            FixReference = fixReference;
            FixRequirementReferences = fixRequirementReferences;
        }

        public CustomRecipe(RecipeConfig recipeConfig)
        {
            Recipe = ScriptableObject.CreateInstance<Recipe>();

            var name = recipeConfig.Name;
            if (string.IsNullOrEmpty(name))
            {
                name = "Recipe_" + recipeConfig.Item;
            }

            Recipe.name = name;
            Recipe.m_item = Mock<ItemDrop>.Create(recipeConfig.Name);
            Recipe.m_amount = recipeConfig.Amount;
            Recipe.m_enabled = recipeConfig.Enabled;

            if (recipeConfig.CraftingStation != null)
            {
                Recipe.m_craftingStation = Mock<CraftingStation>.Create(recipeConfig.CraftingStation);
            }

            if (recipeConfig.RepairStation != null)
            {
                Recipe.m_craftingStation = Mock<CraftingStation>.Create(recipeConfig.RepairStation);
            }

            Recipe.m_minStationLevel = recipeConfig.MinStationLevel;
            Recipe.m_resources = recipeConfig.GetRequirements();

            FixReference = true;
            FixRequirementReferences = true;
        }
    }
}