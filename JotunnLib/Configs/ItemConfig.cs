using System.Collections.Generic;
using UnityEngine;
using Jotunn.Entities;

namespace Jotunn.Configs
{
    /// <summary>
    ///     Configuration class for adding custom items. Automatically creates a recipe for this item.<br />
    ///     Use this in a constructor of <see cref="CustomItem"/> and 
    ///     Jötunn resolves the references to the game objects at runtime.
    /// </summary>
    public class ItemConfig
    {
        /// <summary>
        ///     The unique name for your item.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        ///     The name of the item prefab that this recipe should create. Is automatically set in <see cref="CustomItem"/>.
        /// </summary>
        internal string Item { get; set; } = string.Empty;

        /// <summary>
        ///     The amount of <see cref="Item"/> that will be created from the recipe. Defaults to <c>1</c>.
        /// </summary>
        public int Amount { get; set; } = 1;

        /// <summary>
        ///     Whether this item is craftable or not.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        ///     The name of the crafting station prefab where this recipe can be crafted.<br/>
        ///     Can be set to <c>null</c> to have the recipe be craftable without a crafting station.
        /// </summary>
        public string CraftingStation { get; set; } = string.Empty;

        /// <summary>
        ///     The name of the crafting station prefab where this item can be repaired.<br/>
        ///     Can be set to <c>null</c> to have the recipe be repairable without a crafting station.
        /// </summary>
        public string RepairStation { get; set; } = string.Empty;

        /// <summary>
        ///     The minimum required level for the crafting station. Defaults to <c>0</c>.
        /// </summary>
        public int MinStationLevel { get; set; } = 0;

        /// <summary>
        ///     Array of <see cref="RequirementConfig"/>s for all crafting materials it takes to craft the recipe.
        /// </summary>
        public RequirementConfig[] Requirements { get; set; } = new RequirementConfig[0];

        /// <summary>
        ///     Converts the RequirementConfigs to Valheim style Piece.Requirements
        /// </summary>
        /// <returns>The Valheim Piece.Requirement array</returns>
        public Piece.Requirement[] GetRequirements()
        {
            Piece.Requirement[] reqs = new Piece.Requirement[Requirements.Length];

            for (int i = 0; i < reqs.Length; i++)
            {
                reqs[i] = Requirements[i].GetRequirement();
            }

            return reqs;
        }

        /// <summary>
        ///     Converts the ItemConfig to a Valheim style Recipe.
        /// </summary>
        /// <returns>The Valheim recipe</returns>
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

            if (!string.IsNullOrEmpty(CraftingStation))
            {
                recipe.m_craftingStation = Mock<CraftingStation>.Create(CraftingStation);
            }

            if (!string.IsNullOrEmpty(RepairStation))
            {
                recipe.m_craftingStation = Mock<CraftingStation>.Create(RepairStation);
            }

            recipe.m_minStationLevel = MinStationLevel;
            recipe.m_resources = GetRequirements();

            return recipe;
        }

        /// <summary>
        ///     Loads a single ItemConfig from a JSON string
        /// </summary>
        /// <param name="json">JSON text</param>
        /// <returns>Loaded ItemConfig</returns>
        public static ItemConfig FromJson(string json)
        {
            return SimpleJson.SimpleJson.DeserializeObject<ItemConfig>(json);
        }

        /// <summary>
        ///     Loads a list of ItemConfigs from a JSON string
        /// </summary>
        /// <param name="json">JSON text</param>
        /// <returns>Loaded list of ItemConfigs</returns>
        public static List<ItemConfig> ListFromJson(string json)
        {
            return SimpleJson.SimpleJson.DeserializeObject<List<ItemConfig>>(json);
        }
    }
}
