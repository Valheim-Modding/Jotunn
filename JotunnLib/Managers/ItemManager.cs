using System;
using System.Collections.Generic;
using UnityEngine;
using Jotunn.Utils;
using Jotunn.Entities;
using Jotunn.Configs;
using MonoMod.RuntimeDetour;
using System.Reflection;
using static Jotunn.Entities.CustomItemConversion;

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

        internal readonly List<CustomItem> Items = new List<CustomItem>();
        internal readonly List<CustomRecipe> Recipes = new List<CustomRecipe>();
        internal readonly List<CustomStatusEffect> StatusEffects = new List<CustomStatusEffect>();
        internal readonly List<CustomItemConversion> ItemConversions = new List<CustomItemConversion>();


        public void Init()
        {
            On.ObjectDB.CopyOtherDB += RegisterCustomDataFejd;
            On.ObjectDB.Awake += RegisterCustomData;
            On.Player.Load += ReloadKnownRecipes;

            //Leave space for mods to forcefully run after us. 1000 is an arbitrary "good amount" of space.
            using (new DetourContext() { Priority = int.MaxValue - 1000 })
            {
                On.ObjectDB.Awake += LateObjectDBAwake;
                On.ObjectDB.CopyOtherDB += LateFejdCopy;
            }
        }

        /// <summary>
        /// Event is invoked after most other mods have hooked CopyOtherDB.
        /// </summary>
        private void LateFejdCopy(On.ObjectDB.orig_CopyOtherDB orig, ObjectDB self, ObjectDB other)
        {
            orig(self, other);
            Jotunn.Logger.LogInfo($"Invoking late OnItemsRegisteredFejd");
            OnItemsRegisteredFejd?.SafeInvoke();
        }
        /// <summary>
        /// Event is invoked after most other mods have hooked ObjectDB.Awake.
        /// </summary>
        private void LateObjectDBAwake(On.ObjectDB.orig_Awake orig, ObjectDB self)
        {
            orig(self);
            Jotunn.Logger.LogInfo($"Invoking late OnItemsRegistered");
            OnItemsRegistered?.SafeInvoke();
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

                    if (!itemDrop.m_itemData.m_dropPrefab)
                    {
                        itemDrop.m_itemData.m_dropPrefab = customItem.ItemPrefab;
                    }
                    if (customItem.FixReference)
                    {
                        customItem.ItemPrefab.FixReferences();
                        itemDrop.m_itemData.m_shared.FixReferences();
                        customItem.FixReference = false;
                    }
                    objectDB.m_items.Add(customItem.ItemPrefab);

                    Logger.LogInfo($"Added item {customItem} | Token: {customItem.ItemDrop.TokenName()}");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error while adding item {customItem}: {ex.Message}");

                    // Remove prefab, item and recipe from the managers again
                    PrefabManager.Instance.RemovePrefab(customItem.ItemPrefab.name);
                    Items.Remove(customItem);
                    if (customItem.Recipe != null)
                    {
                        Recipes.Remove(customItem.Recipe);
                    }
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

                    // Remove recipe from the manager again
                    Recipes.Remove(customRecipe);
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

                    // Remove status effect
                    StatusEffects.Remove(customStatusEffect);
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
                    GameObject stationPrefab = PrefabManager.Instance.GetPrefab(conversion.Config.Station);

                    if (!stationPrefab)
                    {
                        throw new Exception($"Invalid station prefab: {conversion.Config.Station}");
                    }

                    // Sure, make three almost identical classes but dont have a common base class, Iron Gate
                    switch (conversion.Type)
                    {
                        case ConversionType.CookingStation:
                            var cookStation = stationPrefab.GetComponent<CookingStation>();
                            var cookConversion = ((CookingConversionConfig)conversion.Config).GetItemConversion();
                            cookConversion.FixReferences();

                            if (cookStation.m_conversion.Exists(c => c.m_from == cookConversion.m_from))
                            {
                                throw new Exception($"Conversion already added: ${cookConversion.m_from.name}");
                            }

                            cookStation.m_conversion.Add(cookConversion);

                            break;
                        case ConversionType.Fermenter:
                            var fermenterStation = stationPrefab.GetComponent<Fermenter>();
                            var fermenterConversion = ((FermenterConversionConfig)conversion.Config).GetItemConversion();
                            fermenterConversion.FixReferences();

                            if (fermenterStation.m_conversion.Exists(c => c.m_from == fermenterConversion.m_from))
                            {
                                throw new Exception($"Conversion already added: ${fermenterConversion.m_from.name}");
                            }

                            fermenterStation.m_conversion.Add(fermenterConversion);

                            break;
                        case ConversionType.Smelter:
                            var smelterStation = stationPrefab.GetComponent<Smelter>();
                            var smelterConversion = ((SmelterConversionConfig)conversion.Config).GetItemConversion();
                            smelterConversion.FixReferences();

                            if (smelterStation.m_conversion.Exists(c => c.m_from == smelterConversion.m_from))
                            {
                                throw new Exception($"Conversion already added: ${smelterConversion.m_from.name}");
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

                    // Remove item conversion again
                    ItemConversions.Remove(conversion);
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
            orig(self, other);

            var isValid = self.IsValid();
            ItemDropMockFix.Switch(!isValid);

            if (isValid)
            {
                RegisterCustomItems(self);

                self.UpdateItemHashes();
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
    }
}
