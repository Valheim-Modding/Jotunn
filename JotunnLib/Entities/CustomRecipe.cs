using JotunnLib.Managers;
using UnityEngine;

namespace JotunnLib.Entities
{
    public class CustomRecipe
    {
        private RecipeConfig _config;
        private Recipe _recipe;

        public Recipe Recipe { 
            get => GetRecipe();
            set => _recipe = value;
        }
        public bool FixReference { get; set; } = false;
        public bool FixRequirementReferences { get; set; } = false;

        public CustomRecipe(Recipe recipe, bool fixReference, bool fixRequirementReferences)
        {
            _recipe = recipe;
            FixReference = fixReference;
            FixRequirementReferences = fixRequirementReferences;
        }

        public CustomRecipe(RecipeConfig recipeConfig)
        {
            _config = recipeConfig;
            FixReference = false;
            FixRequirementReferences = false;
        }

        private Recipe GetRecipe()
        {
            if (_recipe != null)
            {
                return _recipe;
            }

            if (_config != null)
            {
                return _config.GetRecipe();
            }

            return null;
        }
    }
}