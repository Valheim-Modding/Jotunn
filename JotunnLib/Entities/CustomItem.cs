using JotunnLib.Managers;
using UnityEngine;

namespace JotunnLib.Entities
{
    public class CustomItem
    {
        public GameObject ItemPrefab { get; private set; }
        public ItemDrop ItemDrop { get; set; } = null;
        public bool FixReference { get; set; } = false;

        public CustomItem(GameObject itemPrefab, bool fixReference)
        {
            ItemPrefab = itemPrefab;
            ItemDrop = itemPrefab.GetComponent<ItemDrop>();
            FixReference = fixReference;
        }

        public CustomItem(string name)
        {
            ItemPrefab = PrefabManager.Instance.CreateEmptyPrefab(name);
        }

        public CustomItem(string name, string baseName)
        {
            ItemPrefab = PrefabManager.Instance.CreateClonedPrefab(name, baseName);
            ItemDrop = ItemPrefab.GetComponent<ItemDrop>();
        }

        public CustomItem(AssetBundle assetBundle, string assetName, bool fixReference)
        {
            ItemPrefab = (GameObject)assetBundle.LoadAsset(assetName);
            ItemDrop = ItemPrefab.GetComponent<ItemDrop>();
            FixReference = fixReference;
        }

        public bool IsValid()
        {
            return ItemPrefab && ItemDrop && ItemDrop.IsValid();
        }

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
    }
}
