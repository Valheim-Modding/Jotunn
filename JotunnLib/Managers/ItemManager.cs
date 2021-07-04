using System;
using System.Collections.Generic;
using UnityEngine;
using Jotunn.Utils;
using Jotunn.Entities;
using Jotunn.Configs;
using UnityEngine.SceneManagement;
using MonoMod.RuntimeDetour;
using System.Linq;

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
        ///     Internal event that gets fired after <see cref="OnVanillaItemsAvailable"/> did run.
        ///     On this point all mods should have their items and pieces registered with the managers.
        /// </summary>
        internal static event Action OnKitbashItemsAvailable;

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
        ///     Get a custom item by its name.
        /// </summary>
        /// <param name="itemName">Name of the item to search.</param>
        /// <returns></returns>
        public CustomItem GetItem(string itemName)
        {
            return Items.FirstOrDefault(x => x.ItemPrefab.name.Equals(itemName));
        }

        /// <summary>
        ///     Remove a custom item by its name. Removes the custom recipe, too.
        /// </summary>
        /// <param name="itemName">Name of the item to remove.</param>
        public void RemoveItem(string itemName)
        {
            var item = GetItem(itemName);
            if (item == null)
            {
                Logger.LogWarning($"Could not remove item {itemName}: Not found");
                return;
            }

            RemoveItem(item);
        }

        /// <summary>
        ///     Remove a custom item by its ref. Removes the custom recipe, too.
        /// </summary>
        /// <param name="item"><see cref="CustomItem"/> to remove.</param>
        public void RemoveItem(CustomItem item)
        {
            Items.Remove(item);

            if (item.ItemPrefab)
            {
                PrefabManager.Instance.RemovePrefab(item.ItemPrefab.name);
            }
            
            if (item.Recipe != null)
            {
                RemoveRecipe(item.Recipe);
            }
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
        ///     Get a custom recipe by its name.
        /// </summary>
        /// <param name="recipeName">Name of the recipe to search.</param>
        /// <returns></returns>
        public CustomRecipe GetRecipe(string recipeName)
        {
            return Recipes.FirstOrDefault(x => x.Recipe.name.Equals(recipeName));
        }

        /// <summary>
        ///     Remove a custom recipe by its name. Removes it from the manager and the <see cref="ObjectDB"/>, if instantiated.
        /// </summary>
        /// <param name="recipeName">Name of the recipe to remove.</param>
        public void RemoveRecipe(string recipeName)
        {
            var recipe = GetRecipe(recipeName);
            if (recipe == null)
            {
                Logger.LogWarning($"Could not remove recipe {recipeName}: Not found");
                return;
            }

            RemoveRecipe(recipe);
        }

        /// <summary>
        ///     Remove a custom recipe by its ref. Removes it from the manager and the <see cref="ObjectDB"/>, if instantiated.
        /// </summary>
        /// <param name="recipe"><see cref="CustomRecipe"/> to remove.</param>
        public void RemoveRecipe(CustomRecipe recipe)
        {
            Recipes.Remove(recipe);
            
            if (recipe.Recipe != null && ObjectDB.instance != null &&
                ObjectDB.instance.m_recipes.Contains(recipe.Recipe))
            {
                ObjectDB.instance.m_recipes.Remove(recipe.Recipe);
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

        /*
        /// <summary>
        ///     Adds item conversions defined in a JSON file at given path, relative to BepInEx/plugins
        /// </summary>
        /// <param name="path">JSON file path, relative to BepInEx/plugins folder</param>
        public void AddItemConversionsFromJson(string path)
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
            if (Items.Count > 0)
            {
                Logger.LogInfo($"---- Adding custom items to {objectDB} ----");

                List<CustomItem> toDelete = new List<CustomItem>();

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

                        RegisterItemInObjectDB(customItem.ItemPrefab);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Error while adding item {customItem}: {ex}");
                        toDelete.Add(customItem);
                    }
                }

                // Delete custom items with errors
                foreach (var item in toDelete)
                {
                    RemoveItem(item);
                }

                Logger.LogInfo("Updating item hashes");

                objectDB.UpdateItemHashes();
            }
        }

        /// <summary>
        ///     Register a single item in the current ObjectDB.
        ///     Also adds the prefab to the <see cref="PrefabManager"/> and <see cref="ZNetScene"/> if necessary.<br />
        ///     No mock references are fixed.
        /// </summary>
        /// <param name="prefab"><see cref="GameObject"/> with an <see cref="ItemDrop"/> component to add to the <see cref="ObjectDB"/></param>
        public void RegisterItemInObjectDB(GameObject prefab)
        {
            var itemDrop = prefab.GetComponent<ItemDrop>();
            if (itemDrop == null)
            {
                throw new Exception($"Prefab {prefab.name} has no ItemDrop component attached");
            }

            var objectDB = ObjectDB.instance;
            if (objectDB == null)
            {
                throw new Exception($"ObjectDB is not instantiated");
            }

            var hash = prefab.name.GetStableHashCode();
            if (objectDB.m_itemByHash.ContainsKey(hash))
            {
                Logger.LogInfo($"Already added item {prefab.name}");
            }
            else
            {
                if (!PrefabManager.Instance.Prefabs.ContainsKey(prefab.name))
                {
                    PrefabManager.Instance.AddPrefab(prefab);
                }
                if (ZNetScene.instance != null && !ZNetScene.instance.m_namedPrefabs.ContainsKey(hash))
                {
                    PrefabManager.Instance.RegisterToZNetScene(prefab);
                }

                objectDB.m_items.Add(prefab);
                objectDB.m_itemByHash.Add(hash, prefab);
            }

            Logger.LogInfo($"Added item {prefab.name} | Token: {itemDrop.TokenName()}");
        }

        private void RegisterCustomRecipes(ObjectDB objectDB)
        {
            if (Recipes.Count > 0)
            {
                Logger.LogInfo($"---- Adding custom recipes to {objectDB} ----");

                List<CustomRecipe> toDelete = new List<CustomRecipe>();

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
                        Logger.LogError($"Error while adding recipe {customRecipe}: {ex}");
                        toDelete.Add(customRecipe);
                    }
                }

                // Delete custom recipes with errors
                foreach(var recipe in toDelete)
                {
                    Recipes.Remove(recipe);
                }
            }
        }

        private void RegisterCustomStatusEffects(ObjectDB objectDB)
        {
            if (StatusEffects.Count > 0)
            {
                Logger.LogInfo($"---- Adding custom status effects to {objectDB} ----");

                List<CustomStatusEffect> toDelete = new List<CustomStatusEffect>();

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
                        Logger.LogError($"Error while adding status effect {customStatusEffect}: {ex}");
                        toDelete.Add(customStatusEffect);
                    }
                }

                // Delete status effects with errors
                foreach (var statusEffect in toDelete)
                {
                    StatusEffects.Remove(statusEffect);
                }
            }
        }

        private void RegisterCustomItemConversions()
        {
            if (ItemConversions.Count > 0)
            {
                Logger.LogInfo($"---- Adding custom item conversions ----");

                List<CustomItemConversion> toDelete = new List<CustomItemConversion>();

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
                        Logger.LogError($"Error while adding item conversion {conversion}: {ex}");
                        toDelete.Add(conversion);
                    }
                }

                // Delete item conversions with errors
                foreach (var itemConversion in toDelete)
                {
                    ItemConversions.Remove(itemConversion);
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
            InvokeOnVanillaItemsAvailable();
            
            InvokeOnKitbashItemsAvailable();

            orig(self, other);

            if (SceneManager.GetActiveScene().name == "start")
            {
                RegisterCustomItems(self);

                self.UpdateItemHashes();
            }
        }

        private void InvokeOnVanillaItemsAvailable()
        {
            OnVanillaItemsAvailable?.SafeInvoke();
        }

        private void InvokeOnKitbashItemsAvailable()
        {
            OnKitbashItemsAvailable?.SafeInvoke();
        }

        private void InvokeOnItemsRegisteredFejd(On.ObjectDB.orig_CopyOtherDB orig, ObjectDB self, ObjectDB other)
        {
            orig(self, other);

            if (SceneManager.GetActiveScene().name == "start")
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
                RegisterCustomItems(self);
                RegisterCustomRecipes(self);
                RegisterCustomStatusEffects(self);
                RegisterCustomItemConversions();

                self.UpdateItemHashes();
            }
        }
        private void InvokeOnItemsRegistered(On.ObjectDB.orig_Awake orig, ObjectDB self)
        {
            orig(self);

            if (SceneManager.GetActiveScene().name == "main")
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

            if (Items.Count > 0 || Recipes.Count() > 0)
            {
                self.UpdateKnownRecipesList();
            }
        }
    }
}
