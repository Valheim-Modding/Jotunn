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
            var prefabObject = assetBundle.LoadAsset<GameObject>(prefabName);
            var itemDrop = prefabObject.GetComponent<ItemDrop>().m_itemData;
            itemDrop.m_dropPrefab = prefabObject;
            itemDrop.m_shared.m_name = name;
            itemDrop.m_shared.m_description = description;
            return prefabObject;
        }


        /// <summary>
        ///     Loads a <see cref="GameObject"/> from the currently cached assetbundle and adds it to ObjectDB. Optionl paarameters for fine-tuning weapon properties.
        /// </summary>
        /// <param name="prefabName">Case sensitive path of .prefab, relative to "plugins" BepInEx folder.</param>
        /// <param name="name">Token name of the new item.</param>
        /// <param name="description">Description of the item, shows up in tooltip.</param>
        /// <param name="ammoType">Type of ammo that is used by this weapon. Defult is "arrow".</param>
        /// <param name="armor">Armor value.</param>
        /// <param name="armorMaterial">An image reference in .png</param>
        /// <param name="armorPerLevel">Value of additional <paramref name="armor"/> per upgrade of this item.</param>
        /// <param name="attackStatusEffect">Triggered <see cref="StatusEffect"/> on successful hit with this item.</param>
        /// <param name="backstabBonus">Backstab multiplier, applied on-hit, after damage calculations.</param>
        /// <param name="buildPieces">List of pieces the item can build, as a <see cref="PieceTable"/></param>
        /// <param name="canBeRepaired">Sets whether or not this object can be repaired.</param>
        /// <param name="destroyBroken">Sets whether or not this object will be destroyed at 0 durability.</param>
        /// <param name="centerCamera">Sets whether or not the camera follows the direction the player is facing.</param>
        /// <param name="blunt">Blunt damage</param>
        /// <param name="chop">Chop damage</param>
        /// <param name="fire">Fire damage</param>
        /// <param name="frost">Frost damage</param>
        /// <param name="lightning">Lightning damage</param>
        /// <param name="pickaxe">Pickaxe damage</param>
        /// <param name="pierce">Pierce damage</param>
        /// <param name="poison">Poison damage</param>
        /// <param name="slash">Slash damage</param>
        /// <param name="spirit">Spirit damage</param>
        /// <param name="bluntPerLevel"><paramref name="blunt"/> per upgrade of this item.</param>
        /// <param name="chopPerLevel"><paramref name="chop"/> per upgrade of this item.</param>
        /// <param name="firePerLevel"><paramref name="fire"/> per upgrade of this item.</param>
        /// <param name="frostPerLevel"><paramref name="frost"/>Frost damage per upgrade of this item.</param>
        /// <param name="lightningPerLevel"><paramref name="lightning"/>Lightning damage per upgrade of this item.</param>
        /// <param name="pickaxePerLevel"><paramref name="pickaxe"/>Pickaxe damage per upgrade of this item.</param>
        /// <param name="piercePerLevel"><paramref name="pierce"/>Pierce damage per upgrade of this item.</param>
        /// <param name="poisonPerLevel"><paramref name="poison"/>Poison damage per upgrade of this item.</param>
        /// <param name="slashPerLevel"><paramref name="slash"/>Slash damage per upgrade of this item.</param>
        /// <param name="spiritPerLevel"><paramref name="spirit"/>Spirit damage per upgrade of this item.</param>
        /// <param name="secondaryAtkDmg">Damage multiplier of base damage.</param>
        /// <param name="deflectionForce">This value is checked when parrying to discover whether the attack can be parried or not.</param>
        /// <param name="deflectionForcePerLevel">Additional deflection force per upgrade of this item.</param>
        /// <param name="dodgeable">Whether or not this attack will ignore immunity frames in dodging.</param>
        /// <param name="holdStaminaDrain">The amount of stamina drain per second.</param>
        /// <param name="maxDurability">Maximum durability before this item becomes unusable, or breaks if destroyBroken = <see cref="true"/>.</param>
        /// <param name="maxQuality">The maximum amount of upgrades this item can have.</param>
        /// <param name="maxStackSize">Maximum stack size this item can have.</param>
        /// <param name="movementModifier">The movement speed increase, as a multiplier.</param>
        /// <param name="questItem">Sets whether or not this item treated as a quest item.</param>
        /// <param name="spawnOnHit"><see cref="GameObject"/> spawned on successful hit.</param>
        /// <param name="spawnOnHitTerrain"><see cref="GameObject"/> spawned on successful hit on terrain.</param>
        /// <param name="teleportable">Sets whether or not this item can be brought through portals.</param>
        /// <param name="value">Value of this item to Haldor, the Trader.</param>
        /// <param name="variants">Number of variants of this item during crafting.</param>
        /// <param name="weight">Weight of the item, default scaling is ~5:1kg. Default carry weight of the player is 300, or ~60kg.</param>
        /// <param name="blockable">Whether or not this item's attacks are blockable, or whether they ignore "blocking via right click".</param>
        /// <param name="blockPower">Value of "blocking via right click", for this item.</param>
        /// <param name="blockPowerPerLevel">Additional value of <paramref name="blockPower"/>, for this item; per upgrade of this item.</param>
        /// <param name="equipDuration">Length of time that equipping this item takes, in seconds.</param>
        /// <param name="equipStatusEffect">Triggered <see cref="StatusEffect"/> on equip of this item.</param>
        /// <param name="consumeStatusEffect">Triggered <see cref="StatusEffect"/> on consuming this item.</param>
        /// <param name="food">Value added to Hungerbar upon consumption.</param>
        /// <param name="foodBurnTime">Length of time this item gives it's bonuses for.</param>
        /// <param name="foodRegen">How much Health this item grants per second.</param>
        /// <param name="foodStamina">How much Stamina this item grants per second.</param>
        /// <param name="setName">Name of the set that this item belongs to.</param>
        /// <param name="setSize">Size of the set that this item belongs to.</param>
        /// <param name="setStatusEffect">Triggered <see cref="StatusEffect"/> when player has equipped <paramref name="setSize"/> of <paramref name="setName"/></param>
        /// <param name="durabilityDrain">Durability reduced for each use.</param>
        /// <param name="durabilityPerLevel">Additional <paramref name="durabilityDrain"/> per upgrade of this item.</param>
        /// <param name="useDurability">Durability reduced for each use.</param>
        /// <param name="useDurabilityDrain">Whether or not this item uses <paramref name="durabilityDrain"/> when actions are performed.</param>
        public GameObject AddItem(string prefabName, string name, string description,
            string ammoType = "arrow", int armor = 0, Material armorMaterial = null,
            int armorPerLevel = 0, StatusEffect attackStatusEffect = null, int backstabBonus = 2,
            PieceTable buildPieces = null, bool canBeRepaired = true, bool destroyBroken = false,
            bool centerCamera = false,
            int blunt = 0, int chop = 0, int fire = 0, int frost = 0, int lightning = 0,
            int pickaxe = 0, int pierce = 0, int poison = 0, int slash = 0, int spirit = 0,
            int bluntPerLevel = 0, int chopPerLevel = 0, int firePerLevel = 0, int frostPerLevel = 0, int lightningPerLevel = 0,
            int pickaxePerLevel = 0, int piercePerLevel = 0, int poisonPerLevel = 0, int slashPerLevel = 0, int spiritPerLevel = 0,
            float secondaryAtkDmg = 1,
            int deflectionForce = 0, int deflectionForcePerLevel = 0,
            bool dodgeable = true, int holdStaminaDrain = 0, int maxDurability = 1, int maxQuality = 4,
            int maxStackSize = 1, int movementModifier = 0, bool questItem = false,
            GameObject spawnOnHit = null, GameObject spawnOnHitTerrain = null, bool teleportable = true,
            int value = 0, int variants = 0, int weight = 0, bool blockable = true, int blockPower = 0,
            int blockPowerPerLevel = 0, int equipDuration = 2, StatusEffect equipStatusEffect = null,
            StatusEffect consumeStatusEffect = null, float food = 0, int foodBurnTime = 0,
            int foodRegen = 0, int foodStamina = 0, string setName = "", int setSize = 0,
            StatusEffect setStatusEffect = null, int durabilityDrain = 1, int durabilityPerLevel = 50,
            bool useDurability = true, float useDurabilityDrain = 0)
        {

            // if (prefabOld == null) { var Item = new CustomItem(prefabName, prefabOld); }
            // else, break;
            var prefabObject = assetBundle.LoadAsset<GameObject>(prefabName);
            var itemDrop = prefabObject.GetComponent<ItemDrop>().m_itemData;

            // GENERAL
            itemDrop.m_dropPrefab = prefabObject;
            itemDrop.m_shared.m_name = name;
            itemDrop.m_shared.m_description = description;
            itemDrop.m_shared.m_ammoType = ammoType;
            itemDrop.m_shared.m_armor = armor;
            itemDrop.m_shared.m_armorMaterial = armorMaterial;
            itemDrop.m_shared.m_armorPerLevel = armorPerLevel;
            itemDrop.m_shared.m_attackStatusEffect = attackStatusEffect;
            itemDrop.m_shared.m_backstabBonus = backstabBonus;
            itemDrop.m_shared.m_buildPieces = buildPieces;
            itemDrop.m_shared.m_canBeReparied = canBeRepaired;
            itemDrop.m_shared.m_destroyBroken = destroyBroken;
            itemDrop.m_shared.m_centerCamera = centerCamera;
            itemDrop.m_shared.m_damages.m_blunt = blunt;
            itemDrop.m_shared.m_damages.m_chop = chop;
            itemDrop.m_shared.m_damages.m_fire = fire;
            itemDrop.m_shared.m_damages.m_frost = frost;
            itemDrop.m_shared.m_damages.m_lightning = lightning;
            itemDrop.m_shared.m_damages.m_pickaxe = pickaxe;
            itemDrop.m_shared.m_damages.m_pierce = pierce;
            itemDrop.m_shared.m_damages.m_poison = poison;
            itemDrop.m_shared.m_damages.m_slash = slash;
            itemDrop.m_shared.m_damages.m_spirit = spirit;
            itemDrop.m_shared.m_damagesPerLevel.m_blunt = bluntPerLevel;
            itemDrop.m_shared.m_damagesPerLevel.m_chop = chopPerLevel;
            itemDrop.m_shared.m_damagesPerLevel.m_fire = firePerLevel;
            itemDrop.m_shared.m_damagesPerLevel.m_frost = frostPerLevel;
            itemDrop.m_shared.m_damagesPerLevel.m_lightning = lightningPerLevel;
            itemDrop.m_shared.m_damagesPerLevel.m_pickaxe = pickaxePerLevel;
            itemDrop.m_shared.m_damagesPerLevel.m_pierce = piercePerLevel;
            itemDrop.m_shared.m_damagesPerLevel.m_poison = poisonPerLevel;
            itemDrop.m_shared.m_damagesPerLevel.m_slash = slashPerLevel;
            itemDrop.m_shared.m_damagesPerLevel.m_spirit = spiritPerLevel;
            itemDrop.m_shared.m_secondaryAttack.m_damageMultiplier = secondaryAtkDmg;
            itemDrop.m_shared.m_dodgeable = dodgeable;
            itemDrop.m_shared.m_holdStaminaDrain = holdStaminaDrain;
            itemDrop.m_shared.m_maxDurability = maxDurability;
            itemDrop.m_shared.m_maxQuality = maxQuality;
            itemDrop.m_shared.m_maxStackSize = maxStackSize;
            itemDrop.m_shared.m_movementModifier = movementModifier;
            itemDrop.m_shared.m_questItem = questItem;
            itemDrop.m_shared.m_spawnOnHit = spawnOnHit;
            itemDrop.m_shared.m_spawnOnHitTerrain = spawnOnHitTerrain;
            itemDrop.m_shared.m_teleportable = teleportable;
            itemDrop.m_shared.m_value = value;
            itemDrop.m_shared.m_variants = variants;
            itemDrop.m_shared.m_weight = weight;
            // BLOCKING
            itemDrop.m_shared.m_blockable = blockable;
            itemDrop.m_shared.m_blockPower = blockPower;
            itemDrop.m_shared.m_blockPowerPerLevel = blockPowerPerLevel;
            itemDrop.m_shared.m_deflectionForce = deflectionForce;
            itemDrop.m_shared.m_deflectionForcePerLevel = deflectionForcePerLevel;
            // ON EQUIP
            itemDrop.m_shared.m_equipDuration = equipDuration;
            itemDrop.m_shared.m_equipStatusEffect = equipStatusEffect;
            // FOOD
            itemDrop.m_shared.m_consumeStatusEffect = consumeStatusEffect;
            itemDrop.m_shared.m_food = food;
            itemDrop.m_shared.m_foodBurnTime = foodBurnTime;
            itemDrop.m_shared.m_foodRegen = foodRegen;
            itemDrop.m_shared.m_foodStamina = foodStamina;
            // EQUIPPED SETS
            itemDrop.m_shared.m_setName = setName;
            itemDrop.m_shared.m_setSize = setSize;
            itemDrop.m_shared.m_setStatusEffect = setStatusEffect;
            // DURABILITY
            itemDrop.m_shared.m_durabilityDrain = durabilityDrain;
            itemDrop.m_shared.m_durabilityPerLevel = durabilityPerLevel;
            itemDrop.m_shared.m_useDurability = useDurability;
            itemDrop.m_shared.m_useDurabilityDrain = useDurabilityDrain;
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
            var Item = new CustomItem(prefabNew, prefabOld);
            var ItemDrop = Item.ItemDrop.m_itemData.m_shared;
            ItemDrop.m_name = name;
            ItemDrop.m_description = description;
            ItemManager.Instance.AddItem(Item);
        }

        /// <summary>
        ///     Clones a <see cref="GameObject"/> from ObjectDB with fine-tuned properties.
        /// </summary>
        /// <param name="prefabNew">Case sensitive name of the new .prefab.</param>
        /// <param name="prefabOld">Case sensitive name of the .prefab you are cloning.</param>
        /// <param name="name">Token name of the new item.</param>
        /// <param name="description">Description of the item, shows up in tooltip.</param>
        /// <param name="ammoType">Type of ammo that is used by this weapon. Defult is "arrow".</param>
        /// <param name="armor">Armor value.</param>
        /// <param name="armorMaterial">An image reference in .png</param>
        /// <param name="armorPerLevel">Value of additional <paramref name="armor"/> per upgrade of this item.</param>
        /// <param name="attackStatusEffect">Triggered <see cref="StatusEffect"/> on successful hit with this item.</param>
        /// <param name="backstabBonus">Backstab multiplier, applied on-hit, after damage calculations.</param>
        /// <param name="buildPieces">List of pieces the item can build, as a <see cref="PieceTable"/></param>
        /// <param name="canBeRepaired">Sets whether or not this object can be repaired.</param>
        /// <param name="destroyBroken">Sets whether or not this object will be destroyed at 0 durability.</param>
        /// <param name="centerCamera">Sets whether or not the camera follows the direction the player is facing.</param>
        /// <param name="blunt">Blunt damage</param>
        /// <param name="chop">Chop damage</param>
        /// <param name="fire">Fire damage</param>
        /// <param name="frost">Frost damage</param>
        /// <param name="lightning">Lightning damage</param>
        /// <param name="pickaxe">Pickaxe damage</param>
        /// <param name="pierce">Pierce damage</param>
        /// <param name="poison">Poison damage</param>
        /// <param name="slash">Slash damage</param>
        /// <param name="spirit">Spirit damage</param>
        /// <param name="bluntPerLevel"><paramref name="blunt"/> per upgrade of this item.</param>
        /// <param name="chopPerLevel"><paramref name="chop"/> per upgrade of this item.</param>
        /// <param name="firePerLevel"><paramref name="fire"/> per upgrade of this item.</param>
        /// <param name="frostPerLevel"><paramref name="frost"/>Frost damage per upgrade of this item.</param>
        /// <param name="lightningPerLevel"><paramref name="lightning"/>Lightning damage per upgrade of this item.</param>
        /// <param name="pickaxePerLevel"><paramref name="pickaxe"/>Pickaxe damage per upgrade of this item.</param>
        /// <param name="piercePerLevel"><paramref name="pierce"/>Pierce damage per upgrade of this item.</param>
        /// <param name="poisonPerLevel"><paramref name="poison"/>Poison damage per upgrade of this item.</param>
        /// <param name="slashPerLevel"><paramref name="slash"/>Slash damage per upgrade of this item.</param>
        /// <param name="spiritPerLevel"><paramref name="spirit"/>Spirit damage per upgrade of this item.</param>
        /// <param name="secondaryAtkDmg">Damage multiplier of base damage.</param>
        /// <param name="deflectionForce">This value is checked when parrying to discover whether the attack can be parried or not.</param>
        /// <param name="deflectionForcePerLevel">Additional deflection force per upgrade of this item.</param>
        /// <param name="dodgeable">Whether or not this attack will ignore immunity frames in dodging.</param>
        /// <param name="holdStaminaDrain">The amount of stamina drain per second.</param>
        /// <param name="maxDurability">Maximum durability before this item becomes unusable, or breaks if destroyBroken = <see cref="true"/>.</param>
        /// <param name="maxQuality">The maximum amount of upgrades this item can have.</param>
        /// <param name="maxStackSize">Maximum stack size this item can have.</param>
        /// <param name="movementModifier">The movement speed increase, as a multiplier.</param>
        /// <param name="questItem">Sets whether or not this item treated as a quest item.</param>
        /// <param name="spawnOnHit"><see cref="GameObject"/> spawned on successful hit.</param>
        /// <param name="spawnOnHitTerrain"><see cref="GameObject"/> spawned on successful hit on terrain.</param>
        /// <param name="teleportable">Sets whether or not this item can be brought through portals.</param>
        /// <param name="value">Value of this item to Haldor, the Trader.</param>
        /// <param name="variants">Number of variants of this item during crafting.</param>
        /// <param name="weight">Weight of the item, default scaling is ~5:1kg. Default carry weight of the player is 300, or ~60kg.</param>
        /// <param name="blockable">Whether or not this item's attacks are blockable, or whether they ignore "blocking via right click".</param>
        /// <param name="blockPower">Value of "blocking via right click", for this item.</param>
        /// <param name="blockPowerPerLevel">Additional value of <paramref name="blockPower"/>, for this item; per upgrade of this item.</param>
        /// <param name="equipDuration">Length of time that equipping this item takes, in seconds.</param>
        /// <param name="equipStatusEffect">Triggered <see cref="StatusEffect"/> on equip of this item.</param>
        /// <param name="consumeStatusEffect">Triggered <see cref="StatusEffect"/> on consuming this item.</param>
        /// <param name="food">Value added to Hungerbar upon consumption.</param>
        /// <param name="foodBurnTime">Length of time this item gives it's bonuses for.</param>
        /// <param name="foodRegen">How much Health this item grants per second.</param>
        /// <param name="foodStamina">How much Stamina this item grants per second.</param>
        /// <param name="setName">Name of the set that this item belongs to.</param>
        /// <param name="setSize">Size of the set that this item belongs to.</param>
        /// <param name="setStatusEffect">Triggered <see cref="StatusEffect"/> when player has equipped <paramref name="setSize"/> of <paramref name="setName"/></param>
        /// <param name="durabilityDrain">Durability reduced for each use.</param>
        /// <param name="durabilityPerLevel">Additional <paramref name="durabilityDrain"/> per upgrade of this item.</param>
        /// <param name="useDurability">Durability reduced for each use.</param>
        /// <param name="useDurabilityDrain">Whether or not this item uses <paramref name="durabilityDrain"/> when actions are performed.</param>
        public void AddClonedItem(string prefabNew, string prefabOld, string name, string description,
            string ammoType = "arrow", int armor = 0, Material armorMaterial = null,
            int armorPerLevel = 0, StatusEffect attackStatusEffect = null, int backstabBonus = 2,
            PieceTable buildPieces = null, bool canBeRepaired = true, bool destroyBroken = false,
            bool centerCamera = false, int blunt = 0, int chop = 0, int fire = 0, int frost = 0,
            int lightning = 0, int pickaxe = 0, int pierce = 0, int poison = 0, int slash = 0,
            int spirit = 0, int bluntPerLevel = 0, int chopPerLevel = 0, int firePerLevel = 0,
            int frostPerLevel = 0, int lightningPerLevel = 0, int pickaxePerLevel = 0,
            int piercePerLevel = 0, int poisonPerLevel = 0, int slashPerLevel = 0, int spiritPerLevel = 0,
            float secondaryAtkDmg = 1, int deflectionForce = 0, int deflectionForcePerLevel = 0,
            bool dodgeable = true, int holdStaminaDrain = 0, int maxDurability = 1, int maxQuality = 4,
            int maxStackSize = 1, int movementModifier = 0, bool questItem = false,
            GameObject spawnOnHit = null, GameObject spawnOnHitTerrain = null, bool teleportable = true,
            int value = 0, int variants = 0, int weight = 0, bool blockable = true, int blockPower = 0,
            int blockPowerPerLevel = 0, int equipDuration = 2, StatusEffect equipStatusEffect = null,
            StatusEffect consumeStatusEffect = null, float food = 0, int foodBurnTime = 0,
            int foodRegen = 0, int foodStamina = 0, string setName = "", int setSize = 0,
            StatusEffect setStatusEffect = null, int durabilityDrain = 1, int durabilityPerLevel = 50,
            bool useDurability = true, float useDurabilityDrain = 0)
        {
            var Item = new CustomItem(prefabNew, prefabOld);
            var itemDrop = Item.ItemDrop.m_itemData.m_shared;
            itemDrop.m_name = name;
            itemDrop.m_description = description;
            itemDrop.m_ammoType = ammoType;
            itemDrop.m_armor = armor;
            itemDrop.m_armorMaterial = armorMaterial;
            itemDrop.m_armorPerLevel = armorPerLevel;
            itemDrop.m_attackStatusEffect = attackStatusEffect;
            itemDrop.m_backstabBonus = backstabBonus;
            itemDrop.m_buildPieces = buildPieces;
            itemDrop.m_canBeReparied = canBeRepaired;
            itemDrop.m_destroyBroken = destroyBroken;
            itemDrop.m_centerCamera = centerCamera;
            itemDrop.m_damages.m_blunt = blunt;
            itemDrop.m_damages.m_chop = chop;
            itemDrop.m_damages.m_fire = fire;
            itemDrop.m_damages.m_frost = frost;
            itemDrop.m_damages.m_lightning = lightning;
            itemDrop.m_damages.m_pickaxe = pickaxe;
            itemDrop.m_damages.m_pierce = pierce;
            itemDrop.m_damages.m_poison = poison;
            itemDrop.m_damages.m_slash = slash;
            itemDrop.m_damages.m_spirit = spirit;
            itemDrop.m_damagesPerLevel.m_blunt = bluntPerLevel;
            itemDrop.m_damagesPerLevel.m_chop = chopPerLevel;
            itemDrop.m_damagesPerLevel.m_fire = firePerLevel;
            itemDrop.m_damagesPerLevel.m_frost = frostPerLevel;
            itemDrop.m_damagesPerLevel.m_lightning = lightningPerLevel;
            itemDrop.m_damagesPerLevel.m_pickaxe = pickaxePerLevel;
            itemDrop.m_damagesPerLevel.m_pierce = piercePerLevel;
            itemDrop.m_damagesPerLevel.m_poison = poisonPerLevel;
            itemDrop.m_damagesPerLevel.m_slash = slashPerLevel;
            itemDrop.m_damagesPerLevel.m_spirit = spiritPerLevel;
            itemDrop.m_secondaryAttack.m_damageMultiplier = secondaryAtkDmg;
            itemDrop.m_dodgeable = dodgeable;
            itemDrop.m_holdStaminaDrain = holdStaminaDrain;
            itemDrop.m_maxDurability = maxDurability;
            itemDrop.m_maxQuality = maxQuality;
            itemDrop.m_maxStackSize = maxStackSize;
            itemDrop.m_movementModifier = movementModifier;
            itemDrop.m_questItem = questItem;
            itemDrop.m_spawnOnHit = spawnOnHit;
            itemDrop.m_spawnOnHitTerrain = spawnOnHitTerrain;
            itemDrop.m_teleportable = teleportable;
            itemDrop.m_value = value;
            itemDrop.m_variants = variants;
            itemDrop.m_weight = weight;
            // BLOCKING
            itemDrop.m_blockable = blockable;
            itemDrop.m_blockPower = blockPower;
            itemDrop.m_blockPowerPerLevel = blockPowerPerLevel;
            itemDrop.m_deflectionForce = deflectionForce;
            itemDrop.m_deflectionForcePerLevel = deflectionForcePerLevel;
            // ON EQUIP
            itemDrop.m_equipDuration = equipDuration;
            itemDrop.m_equipStatusEffect = equipStatusEffect;
            // FOOD
            itemDrop.m_consumeStatusEffect = consumeStatusEffect;
            itemDrop.m_food = food;
            itemDrop.m_foodBurnTime = foodBurnTime;
            itemDrop.m_foodRegen = foodRegen;
            itemDrop.m_foodStamina = foodStamina;
            // EQUIPPED SETS
            itemDrop.m_setName = setName;
            itemDrop.m_setSize = setSize;
            itemDrop.m_setStatusEffect = setStatusEffect;
            // DURABILITY
            itemDrop.m_durabilityDrain = durabilityDrain;
            itemDrop.m_durabilityPerLevel = durabilityPerLevel;
            itemDrop.m_useDurability = useDurability;
            itemDrop.m_useDurabilityDrain = useDurabilityDrain;
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
            var prefabObject = AddItem(prefabName, name, description);
            var reflect = prefabObject.GetComponent<CraftingStation>();
            if (reflect == null)
            {
                var er = GameObject.Find("piece_workbench");
                var craft = er.GetComponent<CraftingStation>();
                prefabObject.AddComponent<CraftingStation>();
                var Craft = prefabObject.GetComponent<CraftingStation>();
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
        /// <param name="prefabName">Case sensitive name of the new <see cref="Piece"/>.</param>
        /// <param name="name">Token name of the new item.</param>
        /// <param name="description">Description of the item, shows up in tooltip.</param>
        /// <param name="pieceTable">Case sensitive name of <see cref="PieceTable"/> assigned to the recipe.</param>
        /// <param name="craftingStation">Case sensitive name of the <see cref="CraftingStation"/> required </param>
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
        public void AddRecipe(GameObject prefabNew, bool fixRefs = false, string craftingStation = "",
            string repairStation = "", int minStationLevel = 1, int amount = 1,
            params RequirementConfig[] inputs)
        {
            var recipe = new CustomItem(prefabNew, fixRefs,
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
        public void AddPieceRequirements(GameObject pieceName, string pieceTable, string craftingStation,
            bool isAllowedInDungeons = false, params RequirementConfig[] inputs)
        {
            var piece = new CustomPiece(pieceName,
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
            var d = change.GetComponent<Recipe>();
            var recipe = new CustomItem(ger, fixRefs,
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
    }
}
