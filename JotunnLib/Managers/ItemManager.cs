using System;
using System.Collections.Generic;
using UnityEngine;
using JotunnLib.Utils;
using JotunnLib.Entities;

namespace JotunnLib.Managers
{
    public class ItemManager : Manager
    {
        public static ItemManager Instance { get; private set; }

        public event EventHandler OnItemsRegistered;
        internal readonly List<CustomItem> Items = new List<CustomItem>();
        internal readonly List<CustomRecipe> Recipes = new List<CustomRecipe>();
        internal readonly List<CustomStatusEffect> StatusEffects = new List<CustomStatusEffect>();

        /// <summary>
        ///     Event that get fired after the ObjectDB get init and before its filled with custom items.
        ///     Your code will execute once unless you resub, the event get cleared after each fire.
        /// </summary>
        public static Action OnBeforeCustomItemsAdded;

        /// <summary>
        ///     Event that get fired after the ObjectDB get init and filled with custom items.
        ///     Your code will execute once unless you resub, the event get cleared after each fire.
        /// </summary>
        public static Action OnAfterInit;

        private void Awake()
        {
            if (Instance != null)
            {
                Logger.LogError($"Cannot have multiple instances of singleton: {GetType()}");

                return;
            }

            Instance = this;
        }

        internal override void Init()
        {
            On.ObjectDB.CopyOtherDB += registerCustomDataFejd;
            On.ObjectDB.Awake += registerCustomData;
            On.Player.Load += reloadKnownRecipes;
        }

        //TODO: dont know if still needed, please check
        /*internal override void Register()
        {
            Logger.LogInfo("---- Registering custom objects ----");

            // Clear existing items and recipes
            Items.Clear();
            Recipes.Clear();

            ItemDropMockFix.Switch(true);

            // Register new items and recipes
            ObjectRegister?.Invoke(null, EventArgs.Empty);
        }*/

        public bool AddItem(CustomItem customItem)
        {
            if (customItem.IsValid())
            {
                if (Items.Contains(customItem))
                {
                    Logger.LogWarning($"Custom item {customItem} already added");
                }
                else
                {
                    // Add to the right layer
                    if (customItem.ItemPrefab.layer == 0)
                    {
                        customItem.ItemPrefab.layer = LayerMask.NameToLayer("item");
                    }

                    PrefabManager.Instance.AddPrefab(customItem.ItemPrefab);
                    Items.Add(customItem);

                    //PrefabManager.instance.NetworkRegister(customItem.ItemPrefab);
                    //customItem.ItemPrefab.NetworkRegister();

                    return true;
                }
            }

            return false;
        }

        public bool AddRecipe(CustomRecipe customRecipe)
        {
            if (!Recipes.Contains(customRecipe))
            {
                Recipes.Add(customRecipe);

                return true;
            }

            return false;
        }

        public bool AddStatusEffect(CustomStatusEffect customStatusEffect)
        {
            if (!StatusEffects.Contains(customStatusEffect))
            {
                StatusEffects.Add(customStatusEffect);

                return true;
            }

            return false;
        }

        private void registerCustomItems(ObjectDB objectDB)
        {
            Logger.LogInfo($"---- Adding custom items to {objectDB} ----");

            foreach (var customItem in Items)
            {
                try
                {
                    var itemDrop = customItem.ItemDrop;
                    
                    if (customItem.FixReference || itemDrop.m_itemData.m_dropPrefab == null)
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

                    Logger.LogInfo($"Added custom item : {customItem.ItemPrefab.name} | Token : {customItem.ItemDrop.TokenName()}");
                } 
                catch (Exception ex)
                {
                    Logger.LogError($"Error while adding custom item {customItem.ItemPrefab.name}: {ex.Message}");
                }
            }

            Logger.LogInfo("Updating item hashes");

            objectDB.UpdateItemHashes();
        }

        private void registerCustomRecipes(ObjectDB objectDB)
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

                    Logger.LogInfo($"Added recipe for : {recipe.m_item.TokenName()}");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error while adding custom recipe {customRecipe.Recipe.name}: {ex.Message}");
                }

            }
        }

        private void registerCustomStatusEffects(ObjectDB objectDB)
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

                    Logger.LogInfo($"Added status effect : {statusEffect.m_name}");

                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error while adding custom status effect {customStatusEffect.StatusEffect.name}: {ex.Message}");
                }
            }
        }

        private void registerCustomDataFejd(On.ObjectDB.orig_CopyOtherDB orig, ObjectDB self, ObjectDB other)
        {
            orig(self, other);

            var isValid = self.IsValid();
            ItemDropMockFix.Switch(!isValid);

            if (isValid)
            {
                OnBeforeCustomItemsAdded.SafeInvoke();
                OnBeforeCustomItemsAdded = null;

                registerCustomItems(self);

                self.UpdateItemHashes();

                OnAfterInit.SafeInvoke();
                OnAfterInit = null;
            }
        }

        private void registerCustomData(On.ObjectDB.orig_Awake orig, ObjectDB self)
        {
            orig(self);

            var isValid = self.IsValid();
            ItemDropMockFix.Switch(!isValid);

            if (isValid)
            {
                OnBeforeCustomItemsAdded.SafeInvoke();
                OnBeforeCustomItemsAdded = null;

                registerCustomItems(self);
                registerCustomRecipes(self);
                registerCustomStatusEffects(self);

                self.UpdateItemHashes();

                OnAfterInit.SafeInvoke();
                OnAfterInit = null;
            }

            // Fire event that everything is added and registered
            OnItemsRegistered?.Invoke(null, EventArgs.Empty);
        }

        private void reloadKnownRecipes(On.Player.orig_Load orig, Player self, ZPackage pkg)
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
