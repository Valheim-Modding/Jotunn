using System.IO;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Jotunn.Utils;
using MonoMod.Cil;

namespace Jotunn.Managers
{
    internal class SaveManager : IManager
    {
        internal const string PlayerPrefix = "player_";
        internal const string EntrySeparator = ".";

        private static SaveManager _instance;
        internal static SaveManager Instance => _instance ??= new SaveManager();

        /// <summary>
        ///     Hide .ctor
        /// </summary>
        private SaveManager() {}

        internal readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

        internal ConditionalWeakTable<Inventory, Container> InventoryToContainer = new ConditionalWeakTable<Inventory, Container>();

        public void Init()
        {
            Directory.CreateDirectory(Paths.CustomItemDataFolder);

            Main.Harmony.PatchAll(typeof(Patches));

            // hook is disabled as the whole manager is disabled anyway. Has to be reworked with a HarmonyTranspiler in the future
            // IL.Inventory.MoveAll += FixMoveAllPerformance;
        }

        private static class Patches
        {
            [HarmonyPatch(typeof(Container), nameof(Container.Awake)), HarmonyPostfix]
            private static void AddToCache(Container __instance) => Instance.AddToCache(__instance);

            [HarmonyPatch(typeof(Container), nameof(Container.OnDestroyed)), HarmonyPostfix]
            private static void RemoveFromCache(Container __instance) => Instance.RemoveFromCache(__instance);

            [HarmonyPatch(typeof(Inventory), nameof(Inventory.Save)), HarmonyPostfix]
            private static void SaveModdedItems(Inventory __instance, ZPackage pkg) => Instance.SaveModdedItems(__instance, pkg);

            [HarmonyPatch(typeof(Inventory), nameof(Inventory.Load)), HarmonyPostfix]
            private static void AddBackCustomItems(Inventory __instance, ZPackage pkg) => Instance.AddBackCustomItems(__instance, pkg);
        }

        private static bool OptimizedRemoveItem(Inventory fromInventory, ItemDrop.ItemData item)
        {
            return fromInventory.m_inventory.Remove(item);
        }

        private static void FixMoveAllPerformance(ILContext il)
        {
            var cursor = new ILCursor(il);

            var optimizedRemoveItemMethodReference = il.Import(typeof(SaveManager).GetMethod(nameof(SaveManager.OptimizedRemoveItem), ReflectionHelper.AllBindingFlags));

            if (cursor.TryGotoNext(i => i.MatchCallOrCallvirt<Inventory>(nameof(Inventory.RemoveItem))))
            {
                cursor.Next.Operand = optimizedRemoveItemMethodReference;

                if (cursor.TryGotoNext(i => i.MatchCallOrCallvirt<Inventory>(nameof(Inventory.RemoveItem))))
                {
                    cursor.Next.Operand = optimizedRemoveItemMethodReference;
                }
                else
                {
                    Logger.LogError($"Failed ILHook {nameof(Inventory.MoveAll)} 2.");
                }
            }
            else
            {
                Logger.LogError($"Failed ILHook {nameof(Inventory.MoveAll)} 1.");
            }
        }

        private void AddToCache(Container self)
        {
            if (self && self.m_inventory != null)
            {
                InventoryToContainer.Add(self.m_inventory, self);
            }
        }

        private void RemoveFromCache(Container self)
        {
            if (self && self.m_inventory != null)
            {
                InventoryToContainer.Remove(self.m_inventory);
            }
        }

        private void SaveModdedItems(Inventory self, ZPackage pkg)
        {
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

        private void AddBackCustomItems(Inventory self, ZPackage pkg)
        {
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
