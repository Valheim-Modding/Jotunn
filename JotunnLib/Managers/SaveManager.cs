using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JotunnLib.Entities;
using JotunnLib.Utils;

namespace JotunnLib.Managers
{
    internal static class SaveCustomData
    {
        internal const string PlayerPrefix = "player_";
        internal const string EntrySeparator = ".";

        internal static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

        internal static void Init()
        {
            Directory.CreateDirectory(Paths.CustomItemDataFolder);

            On.Container.Awake += AddToCache;
            On.Container.OnDestroyed += RemoveFromCache;

            On.Inventory.Save += SaveModdedItems;
            On.Inventory.Load += AddBackCustomItems;
        }

        private static void AddToCache(On.Container.orig_Awake orig, Container self)
        {
            orig(self);

            if (self && self.m_inventory != null)
            {
                Prefab.Cache.InventoryToContainer.Add(self.m_inventory, self);
            }
        }

        private static void RemoveFromCache(On.Container.orig_OnDestroyed orig, Container self)
        {
            orig(self);

            if (self && self.m_inventory != null)
            {
                Prefab.Cache.InventoryToContainer.Remove(self.m_inventory);
            }
        }

        private static string GetContainerUID(this Container container) => container.m_nview.GetZDO().m_uid.ToString();

        internal static string GetInventoryUID(this Inventory self)
        {
            var localPlayer = Player.m_localPlayer;
            if (localPlayer && localPlayer.m_inventory == self)
            {
                return PlayerPrefix + Game.instance.m_playerProfile.m_filename;
            }
            else
            {
                if (Prefab.Cache.InventoryToContainer.TryGetValue(self, out var container))
                {
                    return new string(container.GetContainerUID().Where(c => !InvalidFileNameChars.Contains(c)).ToArray());
                }
            }

            return null;
        }

        private static void SaveModdedItems(On.Inventory.orig_Save orig, Inventory self, ZPackage pkg)
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
                        foreach (var customItem in ObjectManager.Instance.Items)
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

        private static void AddBackCustomItems(On.Inventory.orig_Load orig, Inventory self, ZPackage pkg)
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
