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
        ///     The unique name for your item. May be tokenized.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        ///     The description of your item. May be tokenized.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        ///     The name of the item prefab. Is automatically set in <see cref="CustomItem"/>.
        /// </summary>
        internal string Item { get; set; }

        /// <summary>
        ///     The amount of <see cref="Item"/> that will be created when crafting this item. Defaults to <c>1</c>.
        /// </summary>
        public int Amount { get; set; } = 1;

        /// <summary>
        ///     Whether this item is craftable or not. Defaults to <c>true</c>.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        ///     The name of the crafting station prefab where this recipe can be crafted.<br/>
        ///     Can be set to <c>null</c> to have the recipe be craftable without a crafting station.
        /// </summary>
        public string CraftingStation { get; set; } = null;

        /// <summary>
        ///     The name of the crafting station prefab where this item can be repaired.<br/>
        ///     Can be set to <c>null</c> to have the item be repairable without a crafting station.
        /// </summary>
        public string RepairStation { get; set; } = null;

        /// <summary>
        ///     The minimum required level for the crafting station. Defaults to <c>0</c>.
        /// </summary>
        public int MinStationLevel { get; set; } = 0;

        /// <summary>
        ///     Icons for this item. If more than one icon is added, this item automatically has variants.
        /// </summary>
        public Sprite[] Icons { get; set; } = null;

        /// <summary>
        ///     Array of <see cref="RequirementConfig"/>s for all crafting materials it takes to craft the recipe.
        /// </summary>
        public RequirementConfig[] Requirements { get; set; } = new RequirementConfig[0];

        public void Apply(GameObject prefab)
        {
            var itemDrop = prefab.GetComponent<ItemDrop>();
            if (itemDrop == null)
            {
                Logger.LogError($"GameObject has no ItemDrop attached");
                return;
            }

            // Set the items name from the prefab
            Item = prefab.name;

            var shared = itemDrop.m_itemData.m_shared;
            if (shared == null)
            {
                Logger.LogError($"ItemDrop has no SharedData component");
                return;
            }

            // Set the name and description if provided
            if (!string.IsNullOrEmpty(Name))
            {
                shared.m_name = Name;
            }
            if (!string.IsNullOrEmpty(Description))
            {
                shared.m_description = Description;
            }

            // If there is still no m_name, add a token from the prefabs name
            if (string.IsNullOrEmpty(shared.m_name))
            {
                shared.m_name = $"${prefab.name}";
            }

            // Set icons if provided
            if (Icons != null && Icons.Length > 0)
            {
                itemDrop.m_itemData.m_shared.m_variants = Icons.Length;
                itemDrop.m_itemData.m_shared.m_icons = Icons;
            }
        }

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
