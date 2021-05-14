using System;
using System.Collections.Generic;
using UnityEngine;
using Jotunn.Utils;
using Jotunn.Entities;
using Jotunn.Configs;
using UnityEngine.SceneManagement;
using MonoMod.RuntimeDetour;

namespace Jotunn.Managers
{
    /// <summary>
    ///    Manager for handling all custom data added to the game related to items.
    /// </summary>
    public class ItemManager : IManager
    {
        private static ItemManager _instance;

        /// <summary>
        ///     The singleton instance of this manager.
        /// </summary>
        public static ItemManager Instance
        {
            get
            {
                if (_instance == null) _instance = new ItemManager();
                return _instance;
            }
        }

        /// <summary>
        ///     Event that gets fired after the vanilla items are in memory and available for cloning.
        ///     Your code will execute every time a new ObjectDB is copied (on every menu start).
        ///     If you want to execute just once you will need to unregister from the event after execution.
        /// </summary>
        public static event Action OnVanillaItemsAvailable;

        /// <summary>
        ///     Event that gets fired after all items were added to the ObjectDB on the FejdStartup screen.
        ///     Your code will execute every time a new ObjectDB is copied (on every menu start).
        ///     If you want to execute just once you will need to unregister from the event after execution.
        /// </summary>
        public static event Action OnItemsRegisteredFejd;

        /// <summary>
        ///     Event that gets fired after all items were added to the ObjectDB.
        ///     Your code will execute every time a new ObjectDB is created (on every game start).
        ///     If you want to execute just once you will need to unregister from the event after execution.
        /// </summary>
        public static event Action OnItemsRegistered;

        // Internal lists of all custom entities added
        internal readonly List<CustomItem> Items = new List<CustomItem>();
        internal readonly List<CustomRecipe> Recipes = new List<CustomRecipe>();
        internal readonly List<CustomStatusEffect> StatusEffects = new List<CustomStatusEffect>();
        internal readonly List<CustomItemConversion> ItemConversions = new List<CustomItemConversion>();

        /// <summary>
        ///     Registers all hooks.
        /// </summary>
        public void Init()
        {
            On.ObjectDB.CopyOtherDB += RegisterCustomDataFejd;
            On.ObjectDB.Awake += RegisterCustomData;
            On.Player.Load += ReloadKnownRecipes;

            // Fire events as a late action in the detour so all mods can load before
            // Leave space for mods to forcefully run after us. 1000 is an arbitrary "good amount" of space.
            using (new DetourContext(int.MaxValue - 1000))
            {
                On.ObjectDB.CopyOtherDB += InvokeOnItemsRegisteredFejd;
                On.ObjectDB.Awake += InvokeOnItemsRegistered;
            }
        }

        /// <summary>
        ///     Add a <see cref="CustomItem"/> to the game.<br />
        ///     Checks if the custom item is valid and unique and adds it to the list of custom items.<br />
        ///     Also adds the prefab of the custom item to the <see cref="PrefabManager"/>.<br />
        ///     Custom items are added to the current <see cref="ObjectDB"/> on every <see cref="ObjectDB.Awake"/>.
        /// </summary>
        /// <param name="customItem">The custom item to add.</param>
        /// <returns>true if the custom item was added to the manager.</returns>
        public bool AddItem(CustomItem customItem)
        {
            if (!customItem.IsValid())
            {
                Logger.LogWarning($"Custom item {customItem} is not valid");
                return false;
            }
            if (Items.Contains(customItem))
            {
                Logger.LogWarning($"Custom item {customItem} already added");
                return false;
            }

            // Add prefab to the right layer
            if (customItem.ItemPrefab.layer == 0)
            {
                customItem.ItemPrefab.layer = LayerMask.NameToLayer("item");
            }

            // Add prefab to PrefabManager
            PrefabManager.Instance.AddPrefab(customItem.ItemPrefab);

            // Add custom item to ItemManager
            Items.Add(customItem);

            // Add custom recipe if provided
            if (customItem.Recipe != null)
            {
                AddRecipe(customItem.Recipe);
            }

            return true;
        }

        /// <summary>
        ///     Add a <see cref="CustomRecipe"/> to the game.<br />
        ///     Checks if the custom recipe is unique and adds it to the list of custom recipes.<br />
        ///     Custom recipes are added to the current <see cref="ObjectDB"/> on every <see cref="ObjectDB.Awake"/>.
        /// </summary>
        /// <param name="customRecipe">The custom recipe to add.</param>
        /// <returns>true if the custom recipe was added to the manager.</returns>
        public bool AddRecipe(CustomRecipe customRecipe)
        {
            if (!customRecipe.IsValid())
            {
                Logger.LogWarning($"Custom recipe {customRecipe} is not valid");
                return false;
            }
            if (Recipes.Contains(customRecipe))
            {
                Logger.LogWarning($"Custom recipe {customRecipe} already added");
                return false;
            }

            Recipes.Add(customRecipe);
            return true;
        }

        /// <summary>
        ///     Adds recipes defined in a JSON file at given path, relative to BepInEx/plugins
        /// </summary>
        /// <param name="path">JSON file path, relative to BepInEx/plugins folder</param>
        public void AddRecipesFromJson(string path)
        {
            string json = AssetUtils.LoadText(path);

            if (string.IsNullOrEmpty(json))
            {
                Logger.LogError($"Failed to load recipes from invalid JSON: {path}");
                return;
            }

            List<RecipeConfig> recipes = RecipeConfig.ListFromJson(json);

            foreach (RecipeConfig recipe in recipes)
            {
                AddRecipe(new CustomRecipe(recipe));
            }
        }

        /// <summary>
        ///     Add a <see cref="CustomStatusEffect"/> to the game.<br />
        ///     Checks if the custom status effect is unique and adds it to the list of custom status effects.<br />
        ///     Custom status effects are added to the current <see cref="ObjectDB"/> on every <see cref="ObjectDB.Awake"/>.
        /// </summary>
        /// <param name="customStatusEffect">The custom status effect to add.</param>
        /// <returns>true if the custom status effect was added to the manager.</returns>
        public bool AddStatusEffect(CustomStatusEffect customStatusEffect)
        {
            if (!customStatusEffect.IsValid())
            {
                Logger.LogWarning($"Custom status effect {customStatusEffect} is not valid");
                return false;
            }
            if (StatusEffects.Contains(customStatusEffect))
            {
                Logger.LogWarning($"Custom status effect {customStatusEffect} already added");
                return false;
            }

            StatusEffects.Add(customStatusEffect);
            return true;
        }

        /// <summary>
        ///     Adds a new item conversion for any prefab that has a CookingStation component (such as a "piece_cookingstation").
        /// </summary>
        /// <param name="itemConversion">Item conversion details</param>
        /// <returns>Whether the addition was successful or not</returns>
        public bool AddItemConversion(CustomItemConversion itemConversion)
        {
            if (!itemConversion.IsValid())
            {
                Logger.LogWarning($"Custom item conversion {itemConversion} is not valid");
                return false;
            }
            if (ItemConversions.Contains(itemConversion))
            {
                Logger.LogWarning($"Custom item conversion {itemConversion} already added");
                return false;
            }

            ItemConversions.Add(itemConversion);
            return true;
        }

        /// <summary>
        ///     Adds item conversions defined in a JSON file at given path, relative to BepInEx/plugins
        /// </summary>
        /// <param name="path">JSON file path, relative to BepInEx/plugins folder</param>
       /* public void AddItemConversionsFromJson(string path)
        {
            string json = AssetUtils.LoadText(path);

            if (string.IsNullOrEmpty(json))
            {
                Logger.LogError($"Failed to load item conversions from invalid JSON: {path}");
                return;
            }

            List<CustomItemConversion> configs = CustomItemConversion.ListFromJson(json);

            foreach (var config in configs)
            {
                AddItemConversion(config);
            }
        }*/

        private void RegisterCustomItems(ObjectDB objectDB)
        {
            Logger.LogInfo($"---- Adding custom items to {objectDB} ----");

            foreach (var customItem in Items)
            {
                try
                {
                    var itemDrop = customItem.ItemDrop;
                    if (customItem.FixReference)
                    {
                        customItem.ItemPrefab.FixReferences();
                        itemDrop.m_itemData.m_shared.FixReferences();
                        customItem.FixReference = false;
                    }
                    if (!itemDrop.m_itemData.m_dropPrefab)
                    {
                        itemDrop.m_itemData.m_dropPrefab = customItem.ItemPrefab;
                    }
                    objectDB.m_items.Add(customItem.ItemPrefab);

                    Logger.LogInfo($"Added item {customItem} | Token: {customItem.ItemDrop.TokenName()}");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error while adding item {customItem}: {ex.Message}");
                }
            }

            Logger.LogInfo("Updating item hashes");

            objectDB.UpdateItemHashes();
        }

        private void RegisterCustomRecipes(ObjectDB objectDB)
        {
            Logger.LogInfo($"---- Adding custom recipes to {objectDB} ----");

            foreach (var customRecipe in Recipes)
            {
                try
                {
                    var recipe = customRecipe.Recipe;

                    if (customRecipe.FixReference)
                    {
                        recipe.FixReferences();
                        customRecipe.FixReference = false;
                    }

                    if (customRecipe.FixRequirementReferences)
                    {
                        foreach (var requirement in recipe.m_resources)
                        {
                            requirement.FixReferences();
                        }
                        customRecipe.FixRequirementReferences = false;
                    }
                    objectDB.m_recipes.Add(recipe);

                    Logger.LogInfo($"Added recipe for {recipe.m_item.TokenName()}");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error while adding recipe {customRecipe}: {ex.Message}");
                }
            }
        }

        private void RegisterCustomStatusEffects(ObjectDB objectDB)
        {
            Logger.LogInfo($"---- Adding custom status effects to {objectDB} ----");

            foreach (var customStatusEffect in StatusEffects)
            {
                try
                {
                    var statusEffect = customStatusEffect.StatusEffect;
                    if (customStatusEffect.FixReference)
                    {
                        statusEffect.FixReferences();
                        customStatusEffect.FixReference = false;
                    }

                    objectDB.m_StatusEffects.Add(statusEffect);

                    Logger.LogInfo($"Added status effect {customStatusEffect}");

                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error while adding status effect {customStatusEffect}: {ex.Message}");
                }
            }
        }

        private void RegisterCustomItemConversions()
        {
            Logger.LogInfo($"---- Adding custom item conversions ----");

            foreach (var conversion in ItemConversions)
            {
                try
                {
                    // Try to get the station prefab
                    GameObject stationPrefab = PrefabManager.Instance.GetPrefab(conversion.Config.Station);
                    if (!stationPrefab)
                    {
                        throw new Exception($"Invalid station prefab {conversion.Config.Station}");
                    }

                    // Fix references if needed
                    if (conversion.fixReference)
                    {
                        conversion.ItemConversion.FixReferences();
                        conversion.fixReference = false;
                    }

                    // Sure, make three almost identical classes but dont have a common base class, Iron Gate
                    switch (conversion.Type)
                    {
                        case CustomItemConversion.ConversionType.CookingStation:
                            var cookStation = stationPrefab.GetComponent<CookingStation>();
                            var cookConversion = (CookingStation.ItemConversion)conversion.ItemConversion;
                            
                            if (cookStation.m_conversion.Exists(c => c.m_from == cookConversion.m_from))
                            {
                                Logger.LogInfo($"Already added conversion ${conversion}");
                                continue;
                            }

                            cookStation.m_conversion.Add(cookConversion);

                            break;
                        case CustomItemConversion.ConversionType.Fermenter:
                            var fermenterStation = stationPrefab.GetComponent<Fermenter>();
                            var fermenterConversion = (Fermenter.ItemConversion)conversion.ItemConversion;

                            if (fermenterStation.m_conversion.Exists(c => c.m_from == fermenterConversion.m_from))
                            {
                                Logger.LogInfo($"Already added conversion ${conversion}");
                                continue;
                            }

                            fermenterStation.m_conversion.Add(fermenterConversion);

                            break;
                        case CustomItemConversion.ConversionType.Smelter:
                            var smelterStation = stationPrefab.GetComponent<Smelter>();
                            var smelterConversion = (Smelter.ItemConversion)conversion.ItemConversion;

                            if (smelterStation.m_conversion.Exists(c => c.m_from == smelterConversion.m_from))
                            {
                                Logger.LogInfo($"Already added conversion ${conversion}");
                                continue;
                            }

                            smelterStation.m_conversion.Add(smelterConversion);

                            break;
                        default:
                            throw new Exception($"Unknown conversion type");
                    }

                    Logger.LogInfo($"Added item conversion {conversion}");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error while adding item conversion {conversion}: {ex.Message}");
                }
            }
        }

        /// <summary>
        ///     Hook on <see cref="ObjectDB.CopyOtherDB"/> to add custom items to FejdStartup screen (aka main menu)
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        /// <param name="other"></param>
        private void RegisterCustomDataFejd(On.ObjectDB.orig_CopyOtherDB orig, ObjectDB self, ObjectDB other)
        {
            OnVanillaItemsAvailable?.SafeInvoke();

            orig(self, other);

            if (SceneManager.GetActiveScene().name == "start")
            {
                var isValid = self.IsValid();
                ItemDropMockFix.Switch(!isValid);

                if (isValid)
                {
                    RegisterCustomItems(self);

                    self.UpdateItemHashes();
                }
            }
        }

        private void InvokeOnItemsRegisteredFejd(On.ObjectDB.orig_CopyOtherDB orig, ObjectDB self, ObjectDB other)
        {
            orig(self, other);

            if (SceneManager.GetActiveScene().name == "start" && self.IsValid())
            {
                OnItemsRegisteredFejd?.SafeInvoke();
            }
        }

        /// <summary>
        ///     Hook on <see cref="ObjectDB.Awake"/> to register all custom entities from this manager to the <see cref="ObjectDB"/>.
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private void RegisterCustomData(On.ObjectDB.orig_Awake orig, ObjectDB self)
        {
            orig(self);

            if (SceneManager.GetActiveScene().name == "main")
            {
                var isValid = self.IsValid();
                ItemDropMockFix.Switch(!isValid);

                if (isValid)
                {
                    RegisterCustomItems(self);
                    RegisterCustomRecipes(self);
                    RegisterCustomStatusEffects(self);
                    RegisterCustomItemConversions();

                    self.UpdateItemHashes();
                }
            }
        }
        private void InvokeOnItemsRegistered(On.ObjectDB.orig_Awake orig, ObjectDB self)
        {
            orig(self);

            if (SceneManager.GetActiveScene().name == "main" && self.IsValid())
            {
                OnItemsRegistered?.SafeInvoke();
            }
        }

        /// <summary>
        ///     Hook on <see cref="Player.Load"/> to refresh recipes for the custom items.
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        /// <param name="pkg"></param>
        private void ReloadKnownRecipes(On.Player.orig_Load orig, Player self, ZPackage pkg)
        {
            orig(self, pkg);

            if (Game.instance == null)
            {
                return;
            }

            self.UpdateKnownRecipesList();
        }
        // Simple JVL - For simpletons, a language simplifier.

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
            },
            new RequirementConfig
            {
                Item = needs2,
                Amount = needsAmount2,
                AmountPerLevel = needsAmountPerLevel2,
                Recover = recoverMats2
            },
            new RequirementConfig
            {
                Item = needs3,
                Amount = needsAmount3,
                AmountPerLevel = needsAmountPerLevel3,
                Recover = recoverMats3
            },
            new RequirementConfig
            {
                Item = needs4,
                Amount = needsAmount4,
                AmountPerLevel = needsAmountPerLevel4,
                Recover = recoverMats4
            });
        }
        public void AddCustomConversions(string station, string fromitem, string toitem)
        {
            // Create a conversion for the blastfurnace, the custom item is the new outcome
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
        private void AddPiece(string prefabName, string name, string description, GameObject prefab, string pieceTable, string craftingStation, bool allowedInDungeon, string needs1, int needsAmount1, int needsAmountPerLevel1, bool recoverMats1)
        {
            AddStationPiece(prefabName, name, description);
            AddPieceRecipeWithOneInputs(prefab, pieceTable, craftingStation, needs1, needsAmount1, needsAmountPerLevel1, recoverMats1);
        }
        private void AddStationPiece(string prefabName, string name, string description)
        {
            AddItem(prefabName, name, description);
            reflect = prefab.GetComponent<CraftingStation>();
            reflect.m_name = name;
        }

    }
}
