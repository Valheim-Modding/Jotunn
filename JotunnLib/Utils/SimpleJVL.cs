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
    /// <summary>
    ///     Util functions related to wrapping or extending commonly used Utils.
    /// </summary>
    public class SimpleJVL : IDisposable
    {
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

        /// <summary>
        ///     Loads a <see cref="GameObject"/> from the currently cached assetbundle and adds it to ObjectDB.
        /// </summary>
        /// <param name="prefabName">Case sensitive path of .prefab, relative to "plugins" BepInEx folder.</param>
        /// <param name="name">Token name of the new item.</param>
        /// <param name="description">Description of the item, shows up in tooltip.</param>
        public GameObject AddItem(string prefabName, string name, string description)
        {
            GameObject prefabObject = assetBundle.LoadAsset<GameObject>(prefabName);
            ItemDrop itemDrop = prefabObject.GetComponent<ItemDrop>().m_itemData;
            itemDrop.m_dropPrefab = prefabObject;
            itemDrop.m_shared.m_name = name;
            itemDrop.m_shared.m_description = description;
            return prefabObject;
        }

        /// <summary>
        ///     Clones a <see cref="GameObject"/> from ObjectDB with inherited properties by default.
        /// </summary>
        /// <param name="prefabNew">Case sensitive name of the new .prefab.</param>
        /// <param name="prefabOld">Case sensitive name of the .prefab you are cloning.</param>
        /// <param name="name">Token name of the new item.</param>
        /// <param name="description">Description of the item, shows up in tooltip.</param>
        public void AddClonedItem(string prefabNew, string prefabOld, string name, string description)
        {
            CustomItem Item = new CustomItem(prefabNew, prefabOld);
            ItemDrop ItemDrop = Item.ItemDrop.m_itemData.m_shared;
            ItemDrop.m_name = name;
            ItemDrop.m_description = description;
            ItemManager.Instance.AddItem(Item);
        }

        /// <summary>
        ///     Adds a <see cref="Piece"/> to ObjectDB with inherited properties by default, and builds a new recipe for it.
        /// </summary>
        /// <param name="prefabName">Case sensitive name of the new <see cref="Piece"/>.</param>
        /// <param name="name">Token name of the new item.</param>
        /// <param name="description">Description of the item, shows up in tooltip.</param>
        /// <param name="pieceTable">Case sensitive name of <see cref="PieceTable"/> assigned to the recipe.</param>
        /// <param name="craftingStation">Case sensitive name of the <see cref="CraftingStation"/> required </param>
        /// <param name="inputs">Recipe config as a <see cref="RequirementConfig"/></param>
        public void AddPiece(string prefabName, string name, string description, GameObject prefab,
            string pieceTable = "piece_HammerPieceTable", string craftingStation = "",
            bool isAllowedInDungeons = false, params RequirementConfig[] inputs)
        {
            AddStationPiece(prefabName, name, description);
            AddPieceRequirements(prefab, pieceTable, craftingStation, isAllowedInDungeons, inputs);
        }

        /// <summary>
        ///     Adds a <see cref="CraftingStation"/> to ObjectDB with inherited properties by default.
        /// </summary>
        /// <param name="prefabName">Case sensitive name of the new <see cref="Piece"/>.</param>
        /// <param name="name">Token name of the new item.</param>
        /// <param name="description">Description of the item, shows up in tooltip.</param>
        public void AddStationPiece(string prefabName, string name, string description)
        {
            GameObject prefabObject = AddItem(prefabName, name, description);
            CraftingStation reflect = prefabObject.GetComponent<CraftingStation>();
            if (reflect == null)
            {
                GameObject er = GameObject.Find("piece_workbench");
                CraftingStation craft = er.GetComponent<CraftingStation>();
                prefabObject.AddComponent<CraftingStation>();
                CraftingStation Craft = prefabObject.GetComponent<CraftingStation>();
                Craft.m_areaMarker = craft.m_areaMarker;
                Craft.m_attachedExtensions = craft.m_attachedExtensions;
                Craft.m_connectionPoint = craft.m_connectionPoint;
                Craft.m_craftItemDoneEffects = craft.m_craftItemDoneEffects;
                Craft.m_craftItemEffects = craft.m_craftItemEffects;
                Craft.m_craftRequireFire = craft.m_craftRequireFire;
                Craft.m_craftRequireRoof = craft.m_craftRequireRoof;
                Craft.m_discoverRange = craft.m_discoverRange;
                Craft.m_haveFire = craft.m_haveFire;
                Craft.m_haveFireObject = craft.m_haveFireObject;
                Craft.m_icon = craft.m_icon;
                Craft.m_inUseObject = craft.m_inUseObject;
                Craft.m_inUseObject = craft.m_inUseObject;
                Craft.m_name = craft.m_name;
                Craft.m_nview = craft.m_nview;
                Craft.m_rangeBuild = craft.m_rangeBuild;
                Craft.m_repairItemDoneEffects = craft.m_repairItemDoneEffects;
                Craft.m_roofCheckPoint = craft.m_roofCheckPoint;
                Craft.m_showBasicRecipies = craft.m_showBasicRecipies;
                Craft.m_updateExtensionTimer = craft.m_updateExtensionTimer;
                Craft.m_useAnimation = craft.m_useAnimation;
                Craft.m_useDistance = craft.m_useDistance;
                Craft.m_useTimer = craft.m_useTimer;
            }
            else
            {
                reflect.m_name = name;
            }
        }

        /// <summary>
        ///     Adds a <see cref="CraftingStation"/> to ObjectDB with inherited properties by default, and builds a new recipe for it.
        /// </summary>
        /// <param name="prefabName">Case sensitive name of the new Piece.</param>
        /// <param name="name">Token name of the new item.</param>
        /// <param name="description">Description of the item, shows up in tooltip.</param>
        /// <param name="pieceTable">Case sensitive name of PieceTable assigned to the recipe.</param>
        /// <param name="craftingStation">Case sensitive name of the CraftingStation required </param>
        /// <param name="inputs">Recipe config as a <see cref="RequirementConfig"/></param>
        public void AddStation(string prefabName, string name, string description, GameObject prefab, string pieceTable,
            string craftingStation, bool isAllowedInDungeons, params RequirementConfig[] inputs)
        {
            AddStationPiece(prefabName, name, description);
            AddPieceRequirements(prefab, pieceTable, craftingStation, isAllowedInDungeons, inputs);
        }

        /// <summary>
        ///     Adds the appropriate <see cref="ConversionConfig"/> based on which <see cref="CraftingStation"/> is used.
        /// </summary>
        /// <param name="station">Case sensitive name of the <see cref="CraftingStation"/> the recipe is made at.</param>
        /// <param name="fromitem">Case sensitive name of item to be converted.</param>
        /// <param name="toitem">Case sensitive name of item to be produced.</param>
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

        /// <summary>
        ///     Adds a <see cref="RecipeConfig"/>.
        /// </summary>
        /// <param name="prefabNew">Case sensitive name of the new .prefab.</param>
        /// <param name="fixRefs">Sets whether JVL should fix the references.</param>
        /// <param name="craftingStation">Case sensitive name of the <see cref="CraftingStation"/> required for crafting this item.</param>
        /// <param name="repairStation">Case sensitive name of the <see cref="CraftingStation"/> required for repairing this item.</param>
        /// <param name="minStationLevel">Level of the <see cref="CraftingStation"/> required for crafting this item.</param>
        /// <param name="amount">Amount of this item that <see cref="RequirementConfig"/> will return.</param>
        /// <param name="inputs">Recipe config as a <see cref="RequirementConfig"/></param>
        public void AddRecipe(string prefabNew, bool fixRefs = false, string craftingStation = "",
            string repairStation = "", int minStationLevel = 1, int amount = 1,
            params RequirementConfig[] inputs)
        {
            CustomItem recipe = new CustomItem(prefabNew, fixRefs,
                new ItemConfig { CraftingStation = craftingStation, RepairStation = repairStation, MinStationLevel = minStationLevel, Amount = amount, Requirements = inputs });
            ItemManager.Instance.AddItem(recipe);
        }

        /// <summary>
        ///     Adds a <see cref="RecipeConfig"/> for a <see cref="Piece"/>.
        /// </summary>
        /// <param name="pieceName">Case sensitive name of the new .prefab.</param>
        /// <param name="pieceTable">Case sensitive name of the new .prefab.</param>
        /// <param name="craftingStation">Case sensitive name of the <see cref="CraftingStation"/> required for crafting this item.</param>
        /// <param name="isAllowedInDungeons">Amount of this item that <see cref="RequirementConfig"/> will return.</param>
        /// <param name="inputs">Recipe config as a <see cref="RequirementConfig"/></param>
        public void AddPieceRequirements(string pieceName, string pieceTable, string craftingStation,
            bool isAllowedInDungeons = false, params RequirementConfig[] inputs)
        {
            CustomPiece piece = new CustomPiece(pieceName,
                new PieceConfig { PieceTable = pieceTable, CraftingStation = craftingStation, AllowedInDungeons = isAllowedInDungeons, Requirements = inputs });
            PieceManager.Instance.AddPiece(piece);
        }

        /// <summary>
        ///     Changes the <see cref="RecipeConfig"/> for an <see cref="ItemDrop"/>.
        /// </summary>
        /// <param name="prefabOld">Case sensitive name of the old .prefab.</param>
        /// <param name="fixRefs">Sets whether <see cref="ItemManager"/> should fix the references.</param>
        /// <param name="craftingStation">Case sensitive name of the <see cref="CraftingStation"/> required for crafting this item.</param>
        /// <param name="repairStation">Case sensitive name of the <see cref="CraftingStation"/> required for repairing this item.</param>
        /// <param name="minStationLevel">Level of the <see cref="CraftingStation"/> required for crafting this item.</param>
        /// <param name="amount">Amount of this item that <see cref="RequirementConfig"/> will return.</param>
        /// <param name="inputs">Recipe config as a <see cref="RequirementConfig"/></param>
        public void ChangeRecipe(string prefabOld, bool fixRefs = false, string craftingStation = "",
            string repairStation = "", int minStationLevel = 1, int amount = 1,
            params RequirementConfig[] inputs)
        {
            GameObject ger = GameObject.Find(prefabOld);
            ItemDrop change = ger.GetComponent<ItemDrop>();
            ItemData d = change.GetComponent<Recipe>();
            CustomItem recipe = new CustomItem(ger, fixRefs,
                new ItemConfig
                {
                    CraftingStation = craftingStation,
                    RepairStation = repairStation,
                    MinStationLevel = minStationLevel,
                    Amount = amount,
                    Requirements = inputs
                });
            ObjectDB.instance.m_recipes.Remove(d);
            ItemManager.Instance.AddItem(recipe);
        }

        /// <summary>
        ///     Registers a <see cref="GameObject"/> as a prefab, but does not add it to any spawn tables. Only use with prebuilt assets.
        /// </summary>
        /// <param name="prefabName">Case sensitive name of the new mob.</param>
        /// <param name="mobName">Token name of the new mob.</param>
        public void LoadMob(string prefabName, string mobName)
        {
            GameObject mob = assetBundle.LoadAsset<GameObject>(prefabName);
            ItemData Entity = mob.GetComponent<Humanoid>();
            Entity.m_name = mobName;
            PrefabManager.Instance.AddPrefab(mob);
        }
    }
}
