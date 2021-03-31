﻿using JotunnLib.Managers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using JotunnLib.Utils;

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
            foreach (var customItem in ObjectManager.CustomItems)
            {
                if (customItem.ItemPrefab.name == prefabName)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public static class ItemDropExtension
    {
        public static string TokenName(this ItemDrop self) => self.m_itemData.m_shared.m_name;

        public static bool IsValid(this ItemDrop self)
        {
            try
            {
                var tokenName = self.TokenName();
                if (tokenName[0] != Language.TokenFirstChar)
                {
                    throw new Exception($"Item name first char should be $ for token lookup ! (current item name : {tokenName})");
                }

                var hasIcon = self.m_itemData.m_shared.m_icons.Length > 0;
                if (!hasIcon)
                {
                    throw new Exception($"ItemDrop should have atleast one icon !");
                }

                return true;
            }
            catch (Exception e)
            {
                Log.LogError(e);

                return false;
            }
        }
    }

    public static class ItemDataExtension
    {
        public static string TokenName(this ItemDrop.ItemData self) => self.m_shared.m_name;

        internal const int LinesToNextEntry = 9;

        internal static string GetUID(this ItemDrop.ItemData self)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine(self.m_dropPrefab.name);
            stringBuilder.AppendLine(self.m_stack.ToString(CultureInfo.InvariantCulture));
            stringBuilder.AppendLine(self.m_durability.ToString(CultureInfo.InvariantCulture));
            stringBuilder.AppendLine(self.m_equiped.ToString(CultureInfo.InvariantCulture));
            stringBuilder.AppendLine(self.m_quality.ToString(CultureInfo.InvariantCulture));
            stringBuilder.AppendLine(self.m_variant.ToString(CultureInfo.InvariantCulture));
            stringBuilder.AppendLine(self.m_crafterID.ToString(CultureInfo.InvariantCulture));
            stringBuilder.AppendLine(self.m_crafterName);

            return Hashes.ComputeSha256Hash(stringBuilder.ToString());
        }

        internal static bool RemoveItemFromFile(this ItemDrop.ItemData self, string inventoryId)
        {
            var inventoryFilePath = Path.Combine(Paths.CustomItemDataFolder, inventoryId);

            if (File.Exists(inventoryFilePath))
            {
                var fileLines = File.ReadAllLines(inventoryFilePath).ToList();

                var itemName = self.m_dropPrefab.name;

                var linesToRemove = new List<int>();

                for (var i = 0; i < fileLines.Count; i += LinesToNextEntry)
                {
                    if (fileLines[i] == itemName)
                    {
                        linesToRemove.Add(i);

                        break;
                    }
                }

                if (linesToRemove.Count > 0)
                {
                    foreach (var lineToRemove in linesToRemove)
                    {
                        fileLines.RemoveRange(lineToRemove, LinesToNextEntry);
                    }

                    File.WriteAllLines(inventoryFilePath, fileLines);

                    return true;
                }

                return false;
            }

            return true;
        }

        internal static void SaveToFile(this ItemDrop.ItemData self, string inventoryId)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine(self.m_dropPrefab.name);
            stringBuilder.AppendLine(self.m_stack.ToString(CultureInfo.InvariantCulture));
            stringBuilder.AppendLine(self.m_durability.ToString(CultureInfo.InvariantCulture));
            stringBuilder.AppendLine(self.m_equiped.ToString(CultureInfo.InvariantCulture));
            stringBuilder.AppendLine(self.m_quality.ToString(CultureInfo.InvariantCulture));
            stringBuilder.AppendLine(self.m_variant.ToString(CultureInfo.InvariantCulture));
            stringBuilder.AppendLine(self.m_crafterID.ToString(CultureInfo.InvariantCulture));
            stringBuilder.AppendLine(self.m_crafterName);
            stringBuilder.AppendLine(self.GetUID());

            var inventoryFilePath = Path.Combine(Paths.CustomItemDataFolder, inventoryId);
            File.AppendAllText(inventoryFilePath, stringBuilder.ToString());
        }
    }

    public static class InventoryExtension
    {
        public static bool HasAnyCustomItem(this Inventory self)
        {
            foreach (var inventoryItem in self.m_inventory)
            {
                foreach (var customItem in ObjectManager.CustomItems)
                {
                    if (inventoryItem.TokenName() == customItem.ItemDrop.TokenName())
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal static bool HasEveryItemFromSavedFile(this Inventory self)
        {
            var customItemNames = self.GetAllCustomItemNamesFromFile();
            var savedFileCount = customItemNames.Count;

            var inventoryCount = 0;
            foreach (var inventoryItem in self.m_inventory)
            {
                foreach (var customItemName in customItemNames)
                {
                    if (inventoryItem.m_dropPrefab.name == customItemName)
                    {
                        inventoryCount++;
                    }
                }
            }

            return inventoryCount == savedFileCount;
        }

        internal static List<string> GetAllCustomItemNamesFromFile(this Inventory self)
        {
            string inventoryId = self.GetInventoryUID();

            var inventoryFilePath = Path.Combine(Paths.CustomItemDataFolder, inventoryId);

            if (!File.Exists(inventoryFilePath))
            {
                return new List<string>();
            }

            var customItemNames = new List<string>();

            var data = File.ReadAllLines(inventoryFilePath);

            for (var i = 0; i < data.Length; i += ItemDataExtension.LinesToNextEntry)
            {
                customItemNames.Add(data[i]);
            }

            return customItemNames;
        }

        internal static void AddCustomItemsFromFile(this Inventory self, string inventoryId)
        {
            var data = File.ReadAllLines(Path.Combine(Paths.CustomItemDataFolder, inventoryId));

            for (var i = 0; i < data.Length; i += ItemDataExtension.LinesToNextEntry)
            {
                var itemPrefab = ObjectDB.instance.GetItemPrefab(data[i]);
                if (!itemPrefab)
                {
                    continue;
                }

                ZNetView.m_forceDisableInit = true;
                GameObject gameObject = UnityEngine.Object.Instantiate(itemPrefab);
                ZNetView.m_forceDisableInit = false;

                var itemDrop = gameObject.GetComponent<ItemDrop>();
                if (!itemDrop)
                {
                    continue;
                }

                var hash = data[i + 8];
                var foundInInventory = false;
                foreach (var itemData in self.m_inventory)
                {
                    if (itemData.GetUID() == hash)
                    {
                        foundInInventory = true;
                        break;
                    }
                }

                if (!foundInInventory)
                {
                    itemDrop.m_itemData.m_stack = Mathf.Min(int.Parse(data[i + 1], CultureInfo.InvariantCulture), itemDrop.m_itemData.m_shared.m_maxStackSize);
                    itemDrop.m_itemData.m_durability = float.Parse(data[i + 2], CultureInfo.InvariantCulture);
                    itemDrop.m_itemData.m_equiped = bool.Parse(data[i + 3]);
                    itemDrop.m_itemData.m_quality = int.Parse(data[i + 4], CultureInfo.InvariantCulture);
                    itemDrop.m_itemData.m_variant = int.Parse(data[i + 5], CultureInfo.InvariantCulture);
                    itemDrop.m_itemData.m_crafterID = long.Parse(data[i + 6], CultureInfo.InvariantCulture);
                    itemDrop.m_itemData.m_crafterName = data[i + 7];

                    self.AddItem(itemDrop.m_itemData);
                }

                UnityEngine.Object.Destroy(gameObject);
            }
        }
    }
}
