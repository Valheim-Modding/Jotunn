using System.Collections.Generic;
using UnityEngine;
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
            if (Item == null)
            {
                Logger.LogError($"No item set in recipe config");
                return null;
            }

            var recipe = ScriptableObject.CreateInstance<Recipe>();

            var name = Name;
            if (string.IsNullOrEmpty(name))
            {
                name = "Recipe_" + Item;
            }

            recipe.name = name;

            recipe.m_item = Mock<ItemDrop>.Create(Item);
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

        // <summary>
        ///     Loads a single RecipeConfig from a JSON string
        /// </summary>
        /// <param name="json">JSON text</param>
        /// <returns>Loaded RecipeConfig</returns>
        public static RecipeConfig FromJson(string json)
        {
            return SimpleJson.SimpleJson.DeserializeObject<RecipeConfig>(json);
        }

        /// <summary>
        ///     Loads a list of RecipeConfigs from a JSON string
        /// </summary>
        /// <param name="json">JSON text</param>
        /// <returns>Loaded list of RecipeConfigs</returns>
        public static List<RecipeConfig> ListFromJson(string json)
        {
            return SimpleJson.SimpleJson.DeserializeObject<List<RecipeConfig>>(json);
        }
    }
}
