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
            Recipe = recipeConfig.GetRecipe();
            FixReference = true;
            FixRequirementReferences = true;
        }
    }
}