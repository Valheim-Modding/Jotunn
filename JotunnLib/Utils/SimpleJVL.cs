// JotunnLib
// a Valheim mod
// 
// File:    SimpleJVL.cs
// Project: JotunnLib

using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;

namespace Jotunn.Utils
{
    /// <summary>
    ///     Util functions related to wrapping or extending commonly used Utils.
    /// </summary>
    public class SimpleJVL : IDisposable
    {
        public AssetBundle assetBundle;

        /// <summary>
        ///     Checks if an <see cref="AssetBundle"/> is loaded, and if so, unloads it.
        /// </summary>
        /// <param name="assetBundleName">Case sensitive path of .prefab, relative to "plugins" BepInEx folder.</param>
        /// <param name="assembly">Case sensitive name of your main class, containing the Awake() method.</param>
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

        /// <summary>
        ///     Checks if an <see cref="AssetBundle"/> is loaded, and if so, unloads it.
        /// </summary>
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
        /// <param name="craftingStation">Case sensitive name of the <see cref="CraftingStation"/> required required to craft this item.</param>
        /// <param name="repairStation">Case sensitive name of the <see cref="CraftingStation"/> required to repair this item.</param>
        /// <param name="minStationLevel">Level of the <see cref="CraftingStation"/> required for crafting this item.</param>
        /// <param name="amount">Amount of this item that <see cref="RequirementConfig"/> will return.</param>
        /// <param name="inputs">Recipe config as a <see cref="RequirementConfig"/></param>
        public void AddItem(string prefabName, string name, string description, bool fixRefs, string craftingStation, string repairStation, int minStationLevel, int amount, params RequirementConfig[] inputs)
        {
            var prefabObject = assetBundle.LoadAsset<GameObject>(prefabName);
            var itemDrop = prefabObject.GetComponent<ItemDrop>().m_itemData;
            itemDrop.m_dropPrefab = prefabObject;
            itemDrop.m_shared.m_name = name;
            itemDrop.m_shared.m_description = description;
            var recipe = new CustomItem(prefabObject, fixRefs,
                new ItemConfig
                {
                    Amount = amount,
                    CraftingStation = craftingStation,
                    RepairStation = repairStation,
                    MinStationLevel = minStationLevel,
                    Requirements = inputs
                });
            ItemManager.Instance.AddItem(recipe);
        }

        /// <summary>
        ///     Loads a <see cref="GameObject"/> from the currently cached assetbundle and adds it to ObjectDB.
        /// </summary>
        /// <param name="prefabName">Case sensitive path of .prefab, relative to "plugins" BepInEx folder.</param>
        /// <param name="name">Token name of the new item.</param>
        /// <param name="description">Description of the item, shows up in tooltip.</param>
        /// <param name="canBeRepaired">Sets whether or not this item can be repaired.</param>
        /// <param name="destroyBroken">Sets whether or not this item will break at 0 durability.</param>
        /// <param name="attackForce">Amount of attack force this item has on successful hits.</param>
        /// <param name="backstabBonus">Damage multiplier from successfully landing a hit on an unaware enemy as <see cref="int"/>, multiplied by 100%..</param>
        /// <param name="blockPower">Amount of block power this item has on successful blocking of hits.</param>
        /// <param name="blockPowerPerLevel">Amount of block power this item has on successful blocking of hits, added on, per level.</param>
        /// <param name="blunt">Damage of type blunt dealt on successful hit.</param>
        /// <param name="pierce">Damage of type pierce dealt on successful hit.</param>
        /// <param name="slash">Damage of type slash dealt on successful hit.</param>
        /// <param name="fire">Damage of type fire dealt on successful hit.</param>
        /// <param name="frost">Damage of type frost dealt on successful hit.</param>
        /// <param name="lightning">Damage of type lightning dealt on successful hit.</param>
        /// <param name="poison">Damage of type poison dealt on successful hit.</param>
        /// <param name="spirit">Damage of type spirit dealt on successful hit.</param>
        /// <param name="chop">Damage of type chop dealt on successful hit.</param>
        /// <param name="pickaxe">Damage of type pickaxe dealt on successful hit.</param>
        /// <param name="bluntPL">Damage of type blunt, added on, per level.</param>
        /// <param name="piercePL">Damage of type pierce, added on, per level.</param>
        /// <param name="slashPL">Damage of type slash, added on, per level.</param>
        /// <param name="firePL">Damage of type fire, added on, per level.</param>
        /// <param name="frostPL">Damage of type frost, added on, per level.</param>
        /// <param name="lightningPL">Damage of type lightning, added on, per level.</param>
        /// <param name="poisonPL">Damage of type poison, added on, per level.</param>
        /// <param name="spiritPL">Damage of type spirit, added on, per level.</param>
        /// <param name="chopPL">Damage of type chop, added on, per level.</param>
        /// <param name="pickaxePL">Damage of type pickaxe, added on, per level.</param>
        /// <param name="deflectionForce">Total deflectionForce this item has.</param>
        /// <param name="deflectionForcePerLevel">Total deflectionForce this item has added to it's durability, per level.</param>
        /// <param name="durabilityPerLevel">Total durability this item has added to it's durability, per level.</param>
        /// <param name="maxDurability">Total durability this item has on creation.</param>
        /// <param name="maxQuality">Maximum Quality this item can achieve.</param>
        /// <param name="movementModifier">Movement speed multiplier as <see cref="int"/>, multiplied by 100%.</param>
        /// <param name="weight">Weight of this item.</param>
        /// <param name="fixRefs">Description of the item, shows up in tooltip.</param>
        /// <param name="craftingStation">Case sensitive name of the <see cref="CraftingStation"/> required required to craft this item.</param>
        /// <param name="repairStation">Case sensitive name of the <see cref="CraftingStation"/> required to repair this item.</param>
        /// <param name="minStationLevel">Level of the <see cref="CraftingStation"/> required for crafting this item.</param>
        /// <param name="amount">Amount of this item that <see cref="RequirementConfig"/> will return.</param>
        /// <param name="inputs">Recipe config as a <see cref="RequirementConfig"/></param>
        public void AddWeapon(string prefabName, string name, string description, bool canBeRepaired, bool destroyBroken, int attackForce, int backstabBonus, int blockPower, int blockPowerPerLevel, int blunt, int pierce, int slash, int fire, int frost, int lightning, int poison, int spirit, int chop, int pickaxe, int bluntPL, int piercePL, int slashPL, int firePL, int frostPL, int lightningPL, int poisonPL, int spiritPL, int chopPL, int pickaxePL, int deflectionForce, int deflectionForcePerLevel, int durabilityPerLevel, int maxDurability, int maxQuality, int movementModifier, int weight, bool fixRefs, string craftingStation, string repairStation, int minStationLevel, int amount, params RequirementConfig[] inputs)
        {
            var prefabObject = assetBundle.LoadAsset<GameObject>(prefabName);
            var itemDrop = prefabObject.GetComponent<ItemDrop>().m_itemData;
            itemDrop.m_dropPrefab = prefabObject;
            itemDrop.m_shared.m_name = name;
            itemDrop.m_shared.m_description = description;
            itemDrop.m_shared.m_canBeReparied = canBeRepaired;
            itemDrop.m_shared.m_destroyBroken = destroyBroken;
            itemDrop.m_shared.m_attackForce = attackForce;
            itemDrop.m_shared.m_backstabBonus = backstabBonus;
            itemDrop.m_shared.m_blockPower = blockPower;
            itemDrop.m_shared.m_blockPowerPerLevel = blockPowerPerLevel;
            var itemDamages = itemDrop.m_shared.m_damages;
            itemDamages.m_blunt = blunt;
            itemDamages.m_pierce = pierce;
            itemDamages.m_slash = slash;
            itemDamages.m_fire = fire;
            itemDamages.m_frost = frost;
            itemDamages.m_lightning = lightning;
            itemDamages.m_poison = poison;
            itemDamages.m_spirit = spirit;
            itemDamages.m_chop = chop;
            itemDamages.m_pickaxe = pickaxe;
            var itemDamagesPerLevel = itemDrop.m_shared.m_damagesPerLevel;
            itemDamagesPerLevel.m_blunt = bluntPL;
            itemDamagesPerLevel.m_pierce = piercePL;
            itemDamagesPerLevel.m_slash = slashPL;
            itemDamagesPerLevel.m_fire = firePL;
            itemDamagesPerLevel.m_frost = frostPL;
            itemDamagesPerLevel.m_lightning = lightningPL;
            itemDamagesPerLevel.m_poison = poisonPL;
            itemDamagesPerLevel.m_spirit = spiritPL;
            itemDamagesPerLevel.m_chop = chopPL;
            itemDamagesPerLevel.m_pickaxe = pickaxePL;
            itemDrop.m_shared.m_deflectionForce = deflectionForce;
            itemDrop.m_shared.m_deflectionForcePerLevel = deflectionForcePerLevel;
            itemDrop.m_shared.m_durabilityPerLevel = durabilityPerLevel;
            itemDrop.m_shared.m_maxDurability = maxDurability;
            itemDrop.m_shared.m_maxQuality = maxQuality;
            itemDrop.m_shared.m_movementModifier = movementModifier;
            itemDrop.m_shared.m_weight = weight;
            var recipe = new CustomItem(prefabObject, fixRefs,
                new ItemConfig
                {
                    Amount = amount,
                    CraftingStation = craftingStation,
                    RepairStation = repairStation,
                    MinStationLevel = minStationLevel,
                    Requirements = inputs
                });
            ItemManager.Instance.AddItem(recipe);
        }

        /// <summary>
        ///     Clones a <see cref="GameObject"/> from ObjectDB with inherited properties by default.
        /// </summary>
        /// <param name="prefabNew">Case sensitive name of the new .prefab.</param>
        /// <param name="prefabOld">Case sensitive name of the .prefab you are cloning.</param>
        /// <param name="name">Token name of the new item.</param>
        /// <param name="description">Description of the item, shows up in tooltip.</param>
        /// <param name="amount">Description of the item, shows up in tooltip.</param>
        /// <param name="craftingStation">Description of the item, shows up in tooltip.</param>
        /// <param name="minStationLevel">Description of the item, shows up in tooltip.</param>
        /// <param name="inputs">Recipe config as a <see cref="RequirementConfig"/></param>
        public void AddClonedItem(string prefabNew, string prefabOld, string name, string description, int amount, string craftingStation, int minStationLevel, params RequirementConfig[] inputs)
        {
            var prefabObject = new CustomItem(prefabNew, prefabOld,
                new ItemConfig
                {
                    Amount = amount,
                    CraftingStation = craftingStation,
                    RepairStation = craftingStation,
                    MinStationLevel = minStationLevel,
                    Requirements = inputs
                });
            var itemDrop = prefabObject.ItemDrop.m_itemData;
            itemDrop.m_shared.m_name = name;
            itemDrop.m_shared.m_description = description;

            ItemManager.Instance.AddItem(prefabObject);
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
        public void AddPiece(string prefabName, string name, string description, string pieceTable, string craftingStation, bool isAllowedInDungeons, params RequirementConfig[] inputs)
        {
            var prefabObject = assetBundle.LoadAsset<GameObject>(prefabName);
            var reflect = prefabObject.GetComponent<Piece>();
            reflect.m_name = name;
            reflect.m_description = description;
            var pieceObject = new CustomPiece(prefabObject,
                new PieceConfig
                {
                    PieceTable = pieceTable,
                    CraftingStation = craftingStation,
                    AllowedInDungeons = isAllowedInDungeons,
                    Requirements = inputs
                });
            PieceManager.Instance.AddPiece(pieceObject);
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
        public void AddStation(string prefabName, string name, string description, string pieceTable, string craftingStation, bool isAllowedInDungeons, params RequirementConfig[] inputs)
        {
            var prefabObject = assetBundle.LoadAsset<GameObject>(prefabName);
            var reflectPiece = prefabObject.GetComponent<Piece>();
            if (reflectPiece == null)
            {
                var er = GameObject.Find("piece_workbench");
                var thatPiece = er.GetComponent<Piece>();
                prefabObject.AddComponent<Piece>();
                var thisPiece = prefabObject.GetComponent<Piece>();
                thisPiece = thatPiece;
            } // Replace this with CloneComponent<CraftingStation> ?
            else
            {
                reflectPiece.m_name = name;
            }
            reflectPiece.m_name = name;
            reflectPiece.m_description = description;
            var reflect = prefabObject.GetComponent<CraftingStation>();
            if (reflect == null)
            {
                var er = GameObject.Find("piece_workbench");
                var craft = er.GetComponent<CraftingStation>();
                prefabObject.AddComponent<CraftingStation>();
                var Craft = prefabObject.GetComponent<CraftingStation>();
                Craft.m_areaMarker = craft.m_areaMarker;
                //        Craft.m_attachedExtensions = craft.m_attachedExtensions;
                Craft.m_connectionPoint = craft.m_connectionPoint;
                Craft.m_craftItemDoneEffects = craft.m_craftItemDoneEffects;
                Craft.m_craftItemEffects = craft.m_craftItemEffects;
                Craft.m_craftRequireFire = craft.m_craftRequireFire;
                Craft.m_craftRequireRoof = craft.m_craftRequireRoof;
                Craft.m_discoverRange = craft.m_discoverRange;
                //        Craft.m_haveFire = craft.m_haveFire;
                Craft.m_haveFireObject = craft.m_haveFireObject;
                Craft.m_icon = craft.m_icon;
                Craft.m_inUseObject = craft.m_inUseObject;
                Craft.m_inUseObject = craft.m_inUseObject;
                Craft.m_name = craft.m_name;
                //        Craft.m_nview = craft.m_nview;
                Craft.m_rangeBuild = craft.m_rangeBuild;
                Craft.m_repairItemDoneEffects = craft.m_repairItemDoneEffects;
                Craft.m_roofCheckPoint = craft.m_roofCheckPoint;
                Craft.m_showBasicRecipies = craft.m_showBasicRecipies;
                //        Craft.m_updateExtensionTimer = craft.m_updateExtensionTimer;
                Craft.m_useAnimation = craft.m_useAnimation;
                Craft.m_useDistance = craft.m_useDistance;
                //        Craft.m_useTimer = craft.m_useTimer;
            } // Replace this with CloneComponent<CraftingStation> ?
            else
            {
                reflect.m_name = name;
            }
            var pieceObject = new CustomPiece(prefabObject,
                new PieceConfig
                {
                    PieceTable = pieceTable,
                    CraftingStation = craftingStation,
                    AllowedInDungeons = isAllowedInDungeons,
                    Requirements = inputs
                });
            PieceManager.Instance.AddPiece(pieceObject);
        }

        /// <summary>
        ///     Adds the appropriate <see cref="ConversionConfig"/> based on which <see cref="CraftingStation"/> is used.
        /// </summary>
        /// <param name="station">Case sensitive name of the <see cref="CraftingStation"/> the recipe is made at.</param>
        /// <param name="fromitem">Case sensitive name of item to be converted.</param>
        /// <param name="toitem">Case sensitive name of item to be produced.</param>
        public void AddCustomConversion(string station, string fromitem, string toitem)
        {
            CustomItemConversion conversion;
            switch (station)
            {
                case "piece_cookingstation":
                    conversion = new CustomItemConversion(new CookingConversionConfig { Station = station, FromItem = fromitem, ToItem = toitem });
                    ItemManager.Instance.AddItemConversion(conversion);
                    break;
                case "fermenter":
                    conversion = new CustomItemConversion(new FermenterConversionConfig { Station = station, FromItem = fromitem, ToItem = toitem });
                    ItemManager.Instance.AddItemConversion(conversion);
                    break;
                case "smelter":
                case "charcoal_kiln":
                case "blastfurnace":
                case "windmill":
                case "piece_spinningwheel":
                    conversion = new CustomItemConversion(new SmelterConversionConfig { Station = station, FromItem = fromitem, ToItem = toitem });
                    ItemManager.Instance.AddItemConversion(conversion);
                    break;
                default:
                    //            Logger.LogWarning($"Unknown station {station} for item conversion from {fromitem} to {toitem}");
                    break;
            }
        }

        /// <summary>
        ///     Adds a <see cref="RecipeConfig"/> for a pre-existing <see cref="CustomItem"/>.
        /// </summary>
        /// <param name="prefabNew">Case sensitive name of the new .prefab.</param>
        /// <param name="fixRefs">Sets whether JVL should fix the references.</param>
        /// <param name="craftingStation">Case sensitive name of the <see cref="CraftingStation"/> required for crafting this item.</param>
        /// <param name="repairStation">Case sensitive name of the <see cref="CraftingStation"/> required for repairing this item.</param>
        /// <param name="minStationLevel">Level of the <see cref="CraftingStation"/> required for crafting this item.</param>
        /// <param name="amount">Amount of this item that <see cref="RequirementConfig"/> will return.</param>
        /// <param name="inputs">Recipe config as a <see cref="RequirementConfig"/></param>
        public void AddRecipe(string prefabNew, bool fixRefs = false, string craftingStation, string repairStation, int minStationLevel, int amount, params RequirementConfig[] inputs)
        {
            CustomItem recipe = new CustomItem(prefabNew, fixRefs,
                new ItemConfig { 
                    CraftingStation = craftingStation,
                    RepairStation = repairStation,
                    MinStationLevel = minStationLevel,
                    Amount = amount,
                    Requirements = inputs });
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
