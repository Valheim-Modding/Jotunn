using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Jotunn.Managers;
using Jotunn.Entities;
using Jotunn.Configs;

namespace Jotunn.Utils
{
    static class SimpleJVL
    {   // Simple JVL - For simpletons, a language simplifier
        static GameObject prefabObject;
        static ItemDrop.ItemData itemDrop;
        static CraftingStation reflect;
        private static AssetBundle assetBundle;
        private static string needs1;
        private static int needsAmount1;
        private static int needsAmountPerLevel1;
        private static bool recoverMats1;
        private static int amount1;
        private static int amountPerLevel1;

        static void AddItem(string prefabName, string name, string description)
        {
            prefabObject = assetBundle.LoadAsset<GameObject>(prefabName);
            itemDrop = prefabObject.GetComponent<ItemDrop>().m_itemData;
            itemDrop.m_dropPrefab = prefabObject;
            itemDrop.m_shared.m_name = name;
            itemDrop.m_shared.m_description = description;
        }
        static void AddClonedItem(string prefabNew, string prefabOld, string name, string description, int armor, int armorPerLevel, int maxDurability, int durabilityPerLevel, int movementModifier, StatusEffect setStatusEffect, StatusEffect equipStatusEffect, bool canBeRepaired, bool destroyBroken, string setName, string geartype, int setSize)
        {
            CustomItem Item = new CustomItem(prefabNew, prefabOld);
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
        static void AddStation(string prefabName, string name, string description, GameObject prefab, string pieceTable, string craftingStation, bool allowedInDungeon, params RequirementConfig[] inputs)
        {
            AddStationPiece(prefabName, name, description);
            AddPieceRecipe(prefab, pieceTable, craftingStation, new RequirementConfig
            {
                Item = needs1,
                Amount = needsAmount1,
                AmountPerLevel = needsAmountPerLevel1,
                Recover = recoverMats1
            });
        }
        static void AddCustomConversions(string station, string fromitem, string toitem)
        {
            var conversion = new CustomItemConversion(new SmelterConversionConfig
            {
                Station = station,
                FromItem = fromitem,
                ToItem = toitem
            });
            ItemManager.Instance.AddItemConversion(conversion);
        }
        static void AddRecipe(GameObject prefabNew, string craftingStation, string repairStation, int minStationLevel, int amount, params RequirementConfig[] inputs)
        {
            var recipe = new CustomItem(prefabNew, fixReference: false,
                new ItemConfig
                {
                    CraftingStation = craftingStation,
                    RepairStation = repairStation,
                    MinStationLevel = minStationLevel,
                    Amount = amount,
                    Requirements = new[]
                    {
                        new RequirementConfig {Item = needs1, Amount = needsAmount1, AmountPerLevel = needsAmountPerLevel1},
                    }
                });
            ItemManager.Instance.AddItem(recipe);
        }
        static void AddCloneRecipe(GameObject item, int amount, string craftingStation, int minStationLevel, params RequirementConfig[] inputs)
        {
            var recipe = new CustomItem(item, fixReference: false,
                new ItemConfig
                {
                    Amount = amount,
                    CraftingStation = craftingStation,
                    MinStationLevel = minStationLevel,
                    Requirements = inputs
                });
            ItemManager.Instance.AddItem(recipe);
        }
        static void AddPieceRecipe(GameObject pieceName, string pieceTable, string craftingStation, params RequirementConfig[] inputs)
        {
            var piece = new CustomPiece(pieceName,
                new PieceConfig
                {
                    PieceTable = pieceTable,
                    CraftingStation = craftingStation,
                    AllowedInDungeons = false,
                    Requirements = new[]
                    {
                        new RequirementConfig
                        {
                            Item = needs1,
                            Amount = amount1,
                            AmountPerLevel = amountPerLevel1,
                            Recover = recoverMats1
                        }
                    }
                });
            PieceManager.Instance.AddPiece(piece);
        }
        static void AddPiece(string prefabName, string name, string description, GameObject prefab, string pieceTable, string craftingStation, params RequirementConfig[] inputs)
        {
            AddStationPiece(prefabName, name, description);
            AddPieceRecipe(prefab, pieceTable, craftingStation, new RequirementConfig
            {
                Item = needs1,
                Amount = needsAmount1,
                AmountPerLevel = needsAmountPerLevel1,
                Recover = recoverMats1
            });
        }
        static void AddStationPiece(string prefabName, string name, string description)
        {
            AddItem(prefabName, name, description);
            reflect = prefabObject.GetComponent<CraftingStation>();
            reflect.m_name = name;
        }

    }
}
