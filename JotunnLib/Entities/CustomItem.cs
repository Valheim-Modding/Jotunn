using JotunnLib.Managers;
using UnityEngine;

namespace JotunnLib.Entities
{
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
        public ItemDrop ItemDrop { get; set; } = null;

        /// <summary>
        ///     Indicator if references from <see cref="Mock"/>s will be replaced at runtime.
        /// </summary>
        public bool FixReference { get; set; } = false;

        /// <summary>
        ///     Custom item from a prefab.<br />
        ///     Can fix references for <see cref="Mock"/>s.
        /// </summary>
        /// <param name="itemPrefab">The prefab for this custom item.</param>
        /// <param name="fixReference">If true references for <see cref="Mock"/> objects get resolved at runtime by Jötunn.</param>
        public CustomItem(GameObject itemPrefab, bool fixReference)
        {
            ItemPrefab = itemPrefab;
            ItemDrop = itemPrefab.GetComponent<ItemDrop>();
            FixReference = fixReference;
        }

        /// <summary>
        ///     Custom item from a prefab loaded from an asset bundle.<br />
        ///     Can fix references for <see cref="Mock"/>s.
        /// </summary>
        /// <param name="assetBundle">A preloaded <see cref="AssetBundle"/></param>
        /// <param name="assetName">Name of the prefab in the bundle.</param>
        /// <param name="fixReference">If true references for <see cref="Mock"/> objects get resolved at runtime by Jötunn.</param>
        public CustomItem(AssetBundle assetBundle, string assetName, bool fixReference)
        {
            ItemPrefab = (GameObject)assetBundle.LoadAsset(assetName);
            if (ItemPrefab)
            {
                ItemDrop = ItemPrefab.GetComponent<ItemDrop>();
            }
            FixReference = fixReference;
        }

        /// <summary>
        ///     Custom item created as an "empty" primitive.<br />
        ///     At least the name and the icon of the ItemDrop must be edited after creation.
        /// </summary>
        /// <param name="name">Name of the new prefab. Must be unique.</param>
        /// <param name="addZNetView">If true a ZNetView component will be added to the prefab for network sync.</param>
        public CustomItem(string name, bool addZNetView = true)
        {
            ItemPrefab = PrefabManager.Instance.CreateEmptyPrefab(name, addZNetView);
            if (ItemPrefab)
            {
                ItemDrop = ItemPrefab.AddComponent<ItemDrop>();
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
        ///     Checks if a custom item is valid (i.e. has a prefab, has an <see cref="ItemDrop"/> 
        ///     component and that component has at least one icon).
        /// </summary>
        /// <returns>true if all criteria is met</returns>
        public bool IsValid()
        {
            return ItemPrefab && ItemDrop && ItemDrop.IsValid();
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

        public override bool Equals(object obj)
        {
            return obj.GetHashCode() == GetHashCode();
        }

        public override int GetHashCode()
        {
            return ItemPrefab.name.GetStableHashCode();
        }

        public override string ToString()
        {
            return ItemPrefab.name;
        }
    }
}
