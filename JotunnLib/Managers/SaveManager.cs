using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using JotunnLib.Entities;
using JotunnLib.Utils;

namespace JotunnLib.Managers
{
    internal class SaveManager : Manager
    {
        internal static SaveManager Instance { get; private set; }

        internal const string PlayerPrefix = "player_";
        internal const string EntrySeparator = ".";

        internal readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

        internal ConditionalWeakTable<Inventory, Container> InventoryToContainer = new ConditionalWeakTable<Inventory, Container>();

        private void Awake()
        {
            if (Instance != null)
            {
                Logger.LogError($"Two instances of singleton {GetType()}");

                return;
            }

            Instance = this;
        }

        internal override void Init()
        {
            Directory.CreateDirectory(Paths.CustomItemDataFolder);

            On.Container.Awake += AddToCache;
            On.Container.OnDestroyed += RemoveFromCache;

            On.Inventory.Save += SaveModdedItems;
            On.Inventory.Load += AddBackCustomItems;
        }

        private void AddToCache(On.Container.orig_Awake orig, Container self)
        {
            orig(self);

            if (self && self.m_inventory != null)
            {
                InventoryToContainer.Add(self.m_inventory, self);
            }
        }

        private void RemoveFromCache(On.Container.orig_OnDestroyed orig, Container self)
        {
            orig(self);

            if (self && self.m_inventory != null)
            {
                InventoryToContainer.Remove(self.m_inventory);
            }
        }

        private void SaveModdedItems(On.Inventory.orig_Save orig, Inventory self, ZPackage pkg)
        {
            orig(self, pkg);

            string inventoryId = self.GetInventoryUID();

            if (inventoryId != null)
            {
                var inventoryFilePath = Path.Combine(Paths.CustomItemDataFolder, inventoryId);
                if (File.Exists(inventoryFilePath) && self.HasEveryItemFromSavedFile())
                {
                    File.Delete(inventoryFilePath);
                }

                foreach (var itemData in self.m_inventory)
                {
                    if (itemData.m_dropPrefab)
                    {
                        foreach (var customItem in ItemManager.Instance.Items)
                        {
                            customItem.ItemDrop.m_itemData.RemoveItemFromFile(inventoryId);
                            if (customItem.ItemDrop.TokenName() == itemData.TokenName())
                            {
                                itemData.SaveToFile(inventoryId);

                                break;
                            }
                        }
                    }
                }
            }
        }

        private void AddBackCustomItems(On.Inventory.orig_Load orig, Inventory self, ZPackage pkg)
        {
            orig(self, pkg);

            string inventoryId = self.GetInventoryUID();

            if (inventoryId != null)
            {
                var inventoryFilePath = Path.Combine(Paths.CustomItemDataFolder, inventoryId);
                var hasDataForThisInventory = File.Exists(inventoryFilePath);
                if (hasDataForThisInventory)
                {
                    if (self.HasEveryItemFromSavedFile())
                    {
                        File.Delete(inventoryFilePath);
                        return;
                    }

                    self.AddCustomItemsFromFile(inventoryId);
                }
            }
        }
    }
}
