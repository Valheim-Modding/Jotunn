using JotunnLib.Configs;
using JotunnLib.Managers;

namespace JotunnLib.Entities
{
    /// <summary>
    ///     Main interface for adding custom recipes to the game.<br />
    ///     All custom recipes have to be wrapped inside this class to add it to Jötunns <see cref="ItemManager"/>.
    /// </summary>
    public class CustomRecipe
    {
        /// <summary>
        ///     The <see cref="global::Recipe"/> for this custom recipe.
        /// </summary>
        public Recipe Recipe { get; set; }

        /// <summary>
        ///     Indicator if references from <see cref="Mock"/>s will be replaced at runtime.
        /// </summary>
        public bool FixReference { get; set; } = false;

        /// <summary>
        ///     Indicator if references from <see cref="MockRequirement"/>s will be replaced at runtime.
        /// </summary>
        public bool FixRequirementReferences { get; set; } = false;

        /// <summary>
        ///     Custom recipe from a <see cref="global::Recipe"/>.<br />
        ///     Can fix references for <see cref="Mock"/>s and <see cref="MockRequirement"/>s or not.
        /// </summary>
        /// <param name="recipe">The <see cref="global::Recipe"/> for a custom item.</param>
        /// <param name="fixReference">If true references for <see cref="Mock"/> objects get resolved at runtime by Jötunn.</param>
        /// <param name="fixRequirementReferences">If true references for <see cref="MockRequirement"/>s get resolved at runtime by Jötunn.</param>
        public CustomRecipe(Recipe recipe, bool fixReference, bool fixRequirementReferences)
        {
            Recipe = recipe;
            FixReference = fixReference;
            FixRequirementReferences = fixRequirementReferences;
        }
        /// <summary>
        ///     Custom recipe from a <see cref="RecipeConfig"/>.<br />
        ///     The <see cref="global::Recipe"/> is created automatically by Jötunn at runtime.
        /// </summary>
        /// <param name="recipeConfig">The <see cref="RecipeConfig"/> for a custom recipe.</param>
        public CustomRecipe(RecipeConfig recipeConfig)
        {
            Recipe = recipeConfig.GetRecipe();
            FixReference = true;
            FixRequirementReferences = true;
        }

        public override bool Equals(object obj)
        {
            return obj.GetHashCode() == GetHashCode();
        }

        public override int GetHashCode()
        {
            return Recipe.name.GetStableHashCode();
        }

        public override string ToString()
        {
            return Recipe.name;
        }
    }
}