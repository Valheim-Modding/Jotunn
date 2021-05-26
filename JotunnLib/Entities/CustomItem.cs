using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace Jotunn.Entities
{
    /// <summary>
    ///     Main interface for adding custom items to the game.<br />
    ///     All custom items have to be wrapped inside this class to add it to Jötunns <see cref="ItemManager"/>.
    /// </summary>
    public class CustomItem
    {
        /// <summary>
        ///     The prefab for this custom item.
        /// </summary>
        public GameObject ItemPrefab { get; set; }

        /// <summary>
        ///     The <see cref="global::ItemDrop"/> component for this custom item as a shortcut. 
        ///     Will not be added again to the prefab when replaced.
        /// </summary>
        public ItemDrop ItemDrop { get; set; }

        /// <summary>
        ///     The <see cref="CustomRecipe"/> associated with this custom item. Is needed to craft
        ///     this item on a workbench or from the players crafting menu.
        /// </summary>
        public CustomRecipe Recipe { get; set; }

        /// <summary>
        ///     Indicator if references from <see cref="Entities.Mock{T}"/>s will be replaced at runtime.
        /// </summary>
        public bool FixReference { get; set; } = false;

        /// <summary>
        ///     Custom item from a prefab.<br />
        ///     Can fix references for <see cref="Entities.Mock{T}"/>s and the <see cref="global::Recipe"/>.
        /// </summary>
        /// <param name="itemPrefab">The prefab for this custom item.</param>
        /// <param name="fixReference">If true references for <see cref="Entities.Mock{T}"/> objects get resolved at runtime by Jötunn.</param>
        public CustomItem(GameObject itemPrefab, bool fixReference)
        {
            ItemPrefab = itemPrefab;
            ItemDrop = itemPrefab.GetComponent<ItemDrop>();
            FixReference = fixReference;
        }

        /// <summary>
        ///     Custom item from a prefab with a <see cref="global::Recipe"/> made from a <see cref="ItemConfig"/>.<br />
        ///     Can fix references for <see cref="Entities.Mock{T}"/>s.
        /// </summary>
        /// <param name="itemPrefab">The prefab for this custom item.</param>
        /// <param name="fixReference">If true references for <see cref="Entities.Mock{T}"/> objects get resolved at runtime by Jötunn.</param>
        /// <param name="itemConfig">The recipe config for this custom item.</param>
        public CustomItem(GameObject itemPrefab, bool fixReference, ItemConfig itemConfig)
        {
            ItemPrefab = itemPrefab;
            ItemDrop = itemPrefab.GetComponent<ItemDrop>();
            FixReference = fixReference;

            itemConfig.Apply(ItemPrefab);
            Recipe = new CustomRecipe(itemConfig.GetRecipe(), true, true);
        }

        /// <summary>
        ///     Custom item created as an "empty" primitive.<br />
        ///     At least the name and the Icon of the <see cref="global::ItemDrop"/> must be edited after creation.
        /// </summary>
        /// <param name="name">Name of the new prefab. Must be unique.</param>
        /// <param name="addZNetView">If true a ZNetView component will be added to the prefab for network sync.</param>
        public CustomItem(string name, bool addZNetView)
        {
            ItemPrefab = PrefabManager.Instance.CreateEmptyPrefab(name, addZNetView);
            if (ItemPrefab)
            {
                ItemDrop = ItemPrefab.AddComponent<ItemDrop>();
                ItemDrop.m_itemData.m_shared = new ItemDrop.ItemData.SharedData();
                ItemDrop.m_itemData.m_shared.m_name = name;
            }
        }

        /// <summary>
        ///     Custom item created as an "empty" primitive with a <see cref="global::Recipe"/> made from a <see cref="ItemConfig"/>.
        /// </summary>
        /// <param name="name">Name of the new prefab. Must be unique.</param>
        /// <param name="addZNetView">If true a ZNetView component will be added to the prefab for network sync.</param>
        /// <param name="itemConfig">The item config for this custom item.</param>
        public CustomItem(string name, bool addZNetView, ItemConfig itemConfig)
        {
            ItemPrefab = PrefabManager.Instance.CreateEmptyPrefab(name, addZNetView);
            if (ItemPrefab)
            {
                ItemDrop = ItemPrefab.AddComponent<ItemDrop>();
                ItemDrop.m_itemData.m_shared = new ItemDrop.ItemData.SharedData();
                itemConfig.Apply(ItemPrefab);
                Recipe = new CustomRecipe(itemConfig.GetRecipe(), true, true);
            }
        }

        /// <summary>
        ///     Custom item created as a copy of a vanilla Valheim prefab.
        /// </summary>
        /// <param name="name">The new name of the prefab after cloning.</param>
        /// <param name="basePrefabName">The name of the base prefab the custom item is cloned from.</param>
        public CustomItem(string name, string basePrefabName)
        {
            ItemPrefab = PrefabManager.Instance.CreateClonedPrefab(name, basePrefabName);
            if (ItemPrefab)
            {
                ItemDrop = ItemPrefab.GetComponent<ItemDrop>();
            }
        }

        /// <summary>
        ///     Custom item created as a copy of a vanilla Valheim prefab with a <see cref="global::Recipe"/> made from a <see cref="ItemConfig"/>.
        /// </summary>
        /// <param name="name">The new name of the prefab after cloning.</param>
        /// <param name="basePrefabName">The name of the base prefab the custom item is cloned from.</param>
        /// <param name="itemConfig">The recipe config for this custom item.</param>
        public CustomItem(string name, string basePrefabName, ItemConfig itemConfig)
        {
            ItemPrefab = PrefabManager.Instance.CreateClonedPrefab(name, basePrefabName);
            if (ItemPrefab)
            {
                ItemDrop = ItemPrefab.GetComponent<ItemDrop>();
                itemConfig.Apply(ItemPrefab);
                Recipe = new CustomRecipe(itemConfig.GetRecipe(), true, true);
            }
        }

        /// <summary>
        ///     Checks if a custom item is valid (i.e. has a prefab, has an <see cref="ItemDrop"/> 
        ///     component with at least one icon).
        /// </summary>
        /// <returns>true if all criteria is met</returns>
        public bool IsValid()
        {
            return ItemPrefab && ItemPrefab.IsValid() && ItemDrop && ItemDrop.IsValid();
        }

        /// <summary>
        ///     Helper method to determine if a prefab with a given name is a custom item created with Jötunn.
        /// </summary>
        /// <param name="prefabName">Name of the prefab to test.</param>
        /// <returns>true if the prefab is added as a custom item to the <see cref="ItemManager"/>.</returns>
        public static bool IsCustomItem(string prefabName)
        {
            foreach (var customItem in ItemManager.Instance.Items)
            {
                if (customItem.ItemPrefab.name == prefabName)
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj.GetHashCode() == GetHashCode();
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return ItemPrefab.name.GetStableHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return ItemPrefab.name;
        }
    }
}
