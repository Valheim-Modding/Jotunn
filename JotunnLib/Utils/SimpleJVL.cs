using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jotunn.Utils
{
    public static class SimpleJVL
    {   // Simple JVL - For simpletons, a language simplifier.
        public GameObject prefab;
        public ItemDrop.ItemData itemDrop;
        public StatusEffect setStatusEffect;
        public StatusEffect equipStatusEffect;
        public ItemDrop.ItemData.SharedData shareddata;
        public HitData.DamageTypes itemDamagePerLevel;
        public HitData.DamageTypes itemDamages;
        public CraftingStation reflect;
        public string prefabName;
        public string pieceName;
        public string pieceTable;
        public string description;
        public string craftingStation;
        public int minStationLevel;
        public int amount;
        public bool allowedInDungeons;
        public string needs1;
        public int needsAmount1;
        public int needsAmountPerLevel1 = 0;
        public bool recoverMats1;
        public string needs2;
        public int needsAmount2;
        public int needsAmountPerLevel2 = 0;
        public bool recoverMats2;
        public string needs3;
        public int needsAmount3;
        public int needsAmountPerLevel3 = 0;
        public bool recoverMats3;
        public string needs4;
        public int needsAmount4;
        public int needsAmountPerLevel4 = 0;
        public bool recoverMats4;
        public string material;
        public string itemsig = "";
        public string downgradeMaterial;
        public string upgradeMaterial;
        public string otherMaterial;
        public string otherMaterial2;
        public string armortype;
        public string oldItem;
        public int armor;
        public int armorPerLevel;
        public int maxDurability;
        public int durabilityPerLevel;
        public int movementModifier;
        public bool canBeRepaired;
        public bool destroyBroken;
        public string geartype;
        public int setSize;
        public string craftngStation;
        public string repairStation;
        public int craftAmount;
        public string repairtiptopline;
        public string repairtipstation;
        public string repairtipappend;
        public string weaponType;
        public int deflectionForce;
        public bool teleportable;
        public int variants = 0;
        public int blunt;
        public int pierce;
        public int slash;
        public int chop;
        public int pickaxe;
        public int fire;
        public int frost;
        public int lightning;
        public int poison;
        public int spirit;

        public void LoadAssets(string assetBundleName)
        {
            Jotunn.Logger.LogInfo($"Embedded resources: {string.Join(",", typeof(Setup).Assembly.GetManifestResourceNames())}");
            assetBundle = AssetUtils.LoadAssetBundleFromResources(assetBundleName, typeof(Setup).Assembly);
            Jotunn.Logger.LogInfo(assetBundle);
        }
        public void AddItem(string prefabName, string name, string description)
        {
            prefab = assetBundle.LoadAsset<GameObject>(prefabName);
            itemDrop = prefab.GetComponent<ItemDrop>().m_itemData;
            itemDrop.m_dropPrefab = prefab;
            itemDrop.m_shared.m_name = name;
            itemDrop.m_shared.m_description = description;
        }
        public void AddClonedItem(string prefabNew, string prefabOld, string name, string description, int armor, int armorPerLevel, int maxDurability, int durabilityPerLevel, int movementModifier, StatusEffect setStatusEffect, StatusEffect equipStatusEffect, bool canBeRepaired, bool destroyBroken, string setName, string geartype, int setSize)
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
        public void AddStation(string prefabName, string name, string description, GameObject prefab, string pieceTable, string craftingStation, bool allowedInDungeon, params RequirementConfig[] inputs)
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
        public void AddCustomConversions(string station, string fromitem, string toitem)
        {
            var conversion = new CustomItemConversion(new SmelterConversionConfig
            {
                Station = station,
                FromItem = fromitem,
                ToItem = toitem
            });
            ItemManager.Instance.AddItemConversion(conversion);
        }
        public void AddRecipe(GameObject prefabNew, string craftingStation, string repairStation, int minStationLevel, int amount, params RequirementConfig[] inputs)
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
        public void AddCloneRecipe(GameObject item, int amount, string craftingStation, int minStationLevel, params RequirementConfig[] inputs)
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
        public void AddPieceRecipe(GameObject pieceName, string pieceTable, string craftingStation, params RequirementConfig[] inputs)
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
        public void AddPiece(string prefabName, string name, string description, GameObject prefab, string pieceTable, string craftingStation, params RequirementConfig[] inputs)
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
        private void AddStationPiece(string prefabName, string name, string description)
        {
            AddItem(prefabName, name, description);
            reflect = prefab.GetComponent<CraftingStation>();
            reflect.m_name = name;
        }

    }
}
