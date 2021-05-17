// JotunnLib
// a Valheim mod
// 
// File:    SimpleJVL.cs
// Project: JotunnLib

using System;
using System.Reflection;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Jotunn.Utils
{
    public class SimpleJVL : IDisposable
    {
        // Simple JVL - For simpletons, a language simplifier
        public AssetBundle assetBundle;

        public SimpleJVL(string assetBundleName, Assembly assembly)
        {
            if (string.IsNullOrEmpty(assetBundleName))
            {
                throw new ArgumentNullException("Argument assetBundleName is null or empty");
            }

            if (assembly == null)
            {
                throw new ArgumentNullException("Argument assembly is null");
            }

            Logger.LogDebug($"Embedded resources: {string.Join(",", assembly.GetManifestResourceNames())}");
            assetBundle = AssetUtils.LoadAssetBundleFromResources(assetBundleName, assembly);
            Logger.LogDebug($"Loaded asset bundle {assetBundle}");
        }

        public void Dispose()
        {
            if (assetBundle)
            {
                string name = assetBundle.name;
                assetBundle.Unload(false);
                Logger.LogDebug($"Unloaded asset bundle {name}");
            }
        }

        public GameObject AddItem(string prefabName, string name, string description)
        {
            var prefabObject = assetBundle.LoadAsset<GameObject>(prefabName);
            var itemDrop = prefabObject.GetComponent<ItemDrop>().m_itemData;
            itemDrop.m_dropPrefab = prefabObject;
            itemDrop.m_shared.m_name = name;
            itemDrop.m_shared.m_description = description;
            return prefabObject;
        }

        public void AddClonedItem(string prefabNew, string prefabOld, string name, string description, int armor, int armorPerLevel, int maxDurability,
            int durabilityPerLevel, int movementModifier, StatusEffect setStatusEffect, StatusEffect equipStatusEffect, bool canBeRepaired, bool destroyBroken,
            string setName, string geartype, int setSize)
        {
            var Item = new CustomItem(prefabNew, prefabOld);
            var ItemDrop = Item.ItemDrop.m_itemData.m_shared;
            ItemDrop.m_name = name;
            ItemDrop.m_description = description;
            ItemDrop.m_armor = armor;
            ItemDrop.m_armorPerLevel = armorPerLevel;
            ItemDrop.m_maxDurability = maxDurability;
            ItemDrop.m_durabilityPerLevel = durabilityPerLevel;
            ItemDrop.m_movementModifier = movementModifier;
            ItemDrop.m_setStatusEffect = setStatusEffect;
            ItemDrop.m_equipStatusEffect = equipStatusEffect;
            ItemDrop.m_canBeReparied = canBeRepaired;
            ItemDrop.m_destroyBroken = destroyBroken;
            ItemDrop.m_setName = setName;
            ItemDrop.m_setSize = setSize;
            ItemManager.Instance.AddItem(Item);
        }

        public void AddPiece(string prefabName, string name, string description, GameObject prefab, string pieceTable, string craftingStation,
            params RequirementConfig[] inputs)
        {
            AddStationPiece(prefabName, name, description);
            AddPieceRecipe(prefab, pieceTable, craftingStation, inputs);
        }

        public void AddStationPiece(string prefabName, string name, string description)
        {
            var prefabObject = AddItem(prefabName, name, description);
            var reflect = prefabObject.GetComponent<CraftingStation>();
            if (reflect == null)
            {
                Logger.LogWarning($"Item {prefabName} has no CraftingStation component");
            }
            else
            {
                reflect.m_name = name;
            }
        }

        public void AddStation(string prefabName, string name, string description, GameObject prefab, string pieceTable, string craftingStation,
            bool allowedInDungeon, params RequirementConfig[] inputs)
        {
            AddStationPiece(prefabName, name, description);
            AddPieceRecipe(prefab, pieceTable, craftingStation, inputs);
        }

        public void AddConversion(string station, string fromitem, string toitem)
        {
            CustomItemConversion conversion;
            switch (station)
            {
                case "piece_cookingstation":
                    conversion = new CustomItemConversion(new CookingConversionConfig {Station = station, FromItem = fromitem, ToItem = toitem});
                    ItemManager.Instance.AddItemConversion(conversion);
                    break;
                case "fermenter":
                    conversion = new CustomItemConversion(new FermenterConversionConfig {Station = station, FromItem = fromitem, ToItem = toitem});
                    ItemManager.Instance.AddItemConversion(conversion);
                    break;
                case "smelter":
                case "charcoal_kiln":
                case "blastfurnace":
                case "windmill":
                case "piece_spinningwheel":
                    conversion = new CustomItemConversion(new SmelterConversionConfig {Station = station, FromItem = fromitem, ToItem = toitem});
                    ItemManager.Instance.AddItemConversion(conversion);
                    break;
                default:
                    Logger.LogWarning($"Unknown station {station} for item conversion from {fromitem} to {toitem}");
                    break;
            }
        }

        public void AddRecipe(GameObject prefabNew, string craftingStation, string repairStation, int minStationLevel, int amount,
            params RequirementConfig[] inputs)
        {
            var recipe = new CustomItem(prefabNew, false,
                new ItemConfig
                {
                    CraftingStation = craftingStation,
                    RepairStation = repairStation,
                    MinStationLevel = minStationLevel,
                    Amount = amount,
                    Requirements = inputs
                });
            ItemManager.Instance.AddItem(recipe);
        }

        public void AddCloneRecipe(GameObject item, int amount, string craftingStation, int minStationLevel, params RequirementConfig[] inputs)
        {
            var recipe = new CustomItem(item, false,
                new ItemConfig {Amount = amount, CraftingStation = craftingStation, MinStationLevel = minStationLevel, Requirements = inputs});
            ItemManager.Instance.AddItem(recipe);
        }

        public void AddPieceRecipe(GameObject pieceName, string pieceTable, string craftingStation, params RequirementConfig[] inputs)
        {
            var piece = new CustomPiece(pieceName,
                new PieceConfig {PieceTable = pieceTable, CraftingStation = craftingStation, AllowedInDungeons = false, Requirements = inputs});
            PieceManager.Instance.AddPiece(piece);
        }

        public void AddRecipe(GameObject prefabNew, bool fixRefs, string craftingStation, string repairStation, int minStationLevel, int amount,
            params RequirementConfig[] inputs)
        {
            var recipe = new CustomItem(prefabNew, fixRefs,
                new ItemConfig
                {
                    CraftingStation = craftingStation,
                    RepairStation = repairStation,
                    MinStationLevel = minStationLevel,
                    Amount = amount,
                    Requirements = inputs
                });
            ItemManager.Instance.AddItem(recipe);
        }
    }
}
