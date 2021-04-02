using JotunnLib.Managers;
using UnityEngine;

namespace JotunnLib.Entities
{
    public class CustomItem
    {
        public GameObject ItemPrefab;
        public ItemDrop ItemDrop;
        public bool FixReference;

        public CustomItem(GameObject itemPrefab, bool fixReference)
        {
            ItemPrefab = itemPrefab;
            ItemDrop = itemPrefab.GetComponent<ItemDrop>();

            FixReference = fixReference;
        }

        public bool IsValid()
        {
            return ItemPrefab && ItemDrop.IsValid();
        }

        public static bool IsCustomItem(string prefabName)
        {
            foreach (var customItem in ObjectManager.Instance.Items)
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
